using Internship.Application.DTOs.RefundRequest;
using Internship.Application.Interfaces;
using Internship.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Internship.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class RefundRequestController : ControllerBase
    {
        private readonly IRefundRequestService _refundRequestService;
        private readonly ILogger<RefundRequestController> _logger;

        public RefundRequestController(
            IRefundRequestService refundRequestService,
            ILogger<RefundRequestController> logger)
        {
            _refundRequestService = refundRequestService ?? throw new ArgumentNullException(nameof(refundRequestService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }



        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Added for duplicate detection
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RefundRequestResponse>> CreateRefundRequest([FromForm] CreateRefundRequestDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Create refund request failed: UserId was null or empty");
                    return Unauthorized(new { message = "User identification failed" });
                }

                _logger.LogInformation("Creating refund request for user {UserId}", userId);

                // Add duplicate check (implement this in your service layer)
                if (await _refundRequestService.RefundExistsForTransactionAsync(request.TransactionId))
                {
                    _logger.LogWarning("Duplicate refund request for transaction {TransactionId}", request.TransactionId);
                    return Conflict(new { message = "Refund request already exists for this transaction" });
                }

                var response = await _refundRequestService.CreateRefundRequestAsync(request, userId);

                _logger.LogInformation("Refund request created successfully for user {UserId}", userId);
                return CreatedAtAction(
                    nameof(GetRefundRequest),
                    new { refundId = response.RefundId }, // Now matches your Get action parameter
                    response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when creating refund request");
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access when creating refund request");
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating refund request");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refund request");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the refund request" });
            }
        }
        [HttpPost("{refundId}/process")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RefundRequestResponse>> ProcessRefundRequest(string refundId, [FromBody] ProcessRefundRequestDto request)
        {
            try
            {
                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminUserId))
                {
                    _logger.LogWarning("Process refund request failed: Admin UserId was null or empty");
                    return Unauthorized(new { message = "Admin identification failed" });
                }

                _logger.LogInformation("Processing refund request {RefundId} by admin {AdminUserId}", refundId, adminUserId);
                request.RefundId = refundId;
                var response = await _refundRequestService.ProcessRefundRequestAsync(request, adminUserId);

                _logger.LogInformation("Refund request {RefundId} processed successfully", refundId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when processing refund request {RefundId}", refundId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when processing refund request {RefundId}", refundId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund request {RefundId}", refundId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while processing the refund request" });
            }
        }

        [HttpGet("{refundId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RefundRequestResponse>> GetRefundRequest(string refundId)
        {
            try
            {
                _logger.LogInformation("Retrieving refund request {RefundId}", refundId);
                var refundRequest = await _refundRequestService.GetRefundRequestAsync(refundId);

                if (refundRequest == null)
                {
                    _logger.LogWarning("Refund request {RefundId} not found", refundId);
                    return NotFound(new { message = "Refund request not found" });
                }

                // Authorization check
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin && refundRequest.UserFullName != User.Identity?.Name)
                {
                    _logger.LogWarning("User {UserId} unauthorized to access refund request {RefundId}", userId, refundId);
                    return Forbid();
                }

                return Ok(refundRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refund request {RefundId}", refundId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the refund request" });
            }
        }

        [HttpGet("my-refund-requests")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<RefundRequestResponse>>> GetMyRefundRequests(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Get my refund requests failed: UserId was null or empty");
                    return Unauthorized(new { message = "User identification failed" });
                }

                _logger.LogInformation("Retrieving refund requests for user {UserId}, page {Page}, pageSize {PageSize}",
                    userId, page, pageSize);
                var refundRequests = await _refundRequestService.GetUserRefundRequestsAsync(userId, page, pageSize);

                return Ok(refundRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user refund requests");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving refund requests" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<RefundRequestResponse>>> GetAllRefundRequests(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Admin retrieving all refund requests, page {Page}, pageSize {PageSize}",
                    page, pageSize);
                var refundRequests = await _refundRequestService.GetAllRefundRequestsAsync(page, pageSize);

                return Ok(refundRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all refund requests");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving refund requests" });
            }
        }

        [HttpGet("by-status/{status}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<RefundRequestResponse>>> GetRefundRequestsByStatus(
            RefundStatus status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Admin retrieving refund requests by status {Status}, page {Page}, pageSize {PageSize}",
                    status, page, pageSize);
                var refundRequests = await _refundRequestService.GetRefundRequestsByStatusAsync(status, page, pageSize);

                return Ok(refundRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refund requests by status {Status}", status);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving refund requests" });
            }
        }
    }
}