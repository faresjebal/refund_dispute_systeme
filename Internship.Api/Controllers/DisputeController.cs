using Internship.Application.DTOs.Dispute;
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
    public class DisputeController : ControllerBase
    {
        private readonly IDisputeService _disputeService;
        private readonly ILogger<DisputeController> _logger;

        public DisputeController(
            IDisputeService disputeService,
            ILogger<DisputeController> logger)
        {
            _disputeService = disputeService ?? throw new ArgumentNullException(nameof(disputeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DisputeResponse>> CreateDispute([FromForm] CreateDisputeDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Create dispute failed: UserId was null or empty");
                    return Unauthorized(new { message = "User identification failed" });
                }

                _logger.LogInformation("Creating dispute for user {UserId}", userId);
                var response = await _disputeService.CreateDisputeAsync(request, userId);

                _logger.LogInformation("Dispute created successfully for user {UserId}", userId);
                return CreatedAtAction(nameof(GetDispute), new { disputeId = response.DisputeId }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when creating dispute");
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access when creating dispute");
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating dispute");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dispute");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the dispute" });
            }
        }

        [HttpPost("{disputeId}/process")]
        [Authorize(Policy = "AdminOrAssignedUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DisputeResponse>> ProcessDispute(string disputeId, [FromBody] ProcessDisputeDto request)
        {
            try
            {
                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminUserId))
                {
                    _logger.LogWarning("Process dispute failed: Admin UserId was null or empty");
                    return Unauthorized(new { message = "Admin identification failed" });
                }

                _logger.LogInformation("Processing dispute {DisputeId} by admin {AdminUserId}", disputeId, adminUserId);
                request.DisputeId = disputeId;
                var response = await _disputeService.ProcessDisputeAsync(request, adminUserId);

                _logger.LogInformation("Dispute {DisputeId} processed successfully", disputeId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when processing dispute {DisputeId}", disputeId);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when processing dispute {DisputeId}", disputeId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dispute {DisputeId}", disputeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while processing the dispute" });
            }
        }

        [HttpPost("{disputeId}/assign")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DisputeResponse>> AssignDispute(string disputeId, [FromBody] string assignedToUserId)
        {
            try
            {
                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminUserId))
                {
                    _logger.LogWarning("Assign dispute failed: Admin UserId was null or empty");
                    return Unauthorized(new { message = "Admin identification failed" });
                }

                // Get the dispute to check if it exists and validate business rules
                var dispute = await _disputeService.GetDisputeAsync(disputeId);
                if (dispute == null)
                {
                    _logger.LogWarning("Cannot assign dispute {DisputeId}: Dispute not found", disputeId);
                    return NotFound(new { message = "Dispute not found" });
                }

                // Check if assigned user is different from dispute user
                if (dispute.UserId == assignedToUserId)
                {
                    _logger.LogWarning("Cannot assign dispute {DisputeId}: Assigned user cannot be the same as dispute user", disputeId);
                    return BadRequest(new { message = "Cannot assign dispute to the same user who created it" });
                }

                _logger.LogInformation("Assigning dispute {DisputeId} to user {AssignedToUserId} by admin {AdminUserId}",
                    disputeId, assignedToUserId, adminUserId);

                var response = await _disputeService.AssignDisputeAsync(disputeId, assignedToUserId, adminUserId);

                _logger.LogInformation("Dispute {DisputeId} assigned successfully", disputeId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when assigning dispute {DisputeId}", disputeId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning dispute {DisputeId}", disputeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while assigning the dispute" });
            }
        }

        [HttpGet("{disputeId}")]
        [Authorize(Policy = "AdminOrAssignedUser")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DisputeResponse>> GetDispute(string disputeId)
        {
            try
            {
                _logger.LogInformation("Retrieving dispute {DisputeId}", disputeId);
                var dispute = await _disputeService.GetDisputeAsync(disputeId);

                if (dispute == null)
                {
                    _logger.LogWarning("Dispute {DisputeId} not found", disputeId);
                    return NotFound(new { message = "Dispute not found" });
                }

                // The authorization policy has already verified access at this point
                _logger.LogInformation("Dispute {DisputeId} retrieved successfully", disputeId);
                return Ok(dispute);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dispute {DisputeId}", disputeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the dispute" });
            }
        }

        [HttpGet("my-disputes")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DisputeResponse>>> GetMyDisputes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Get my disputes failed: UserId was null or empty");
                    return Unauthorized(new { message = "User identification failed" });
                }

                _logger.LogInformation("Retrieving disputes for user {UserId}, page {Page}, pageSize {PageSize}",
                    userId, page, pageSize);
                var disputes = await _disputeService.GetUserDisputesAsync(userId, page, pageSize);

                return Ok(disputes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user disputes");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving disputes" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DisputeResponse>>> GetAllDisputes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Admin retrieving all disputes, page {Page}, pageSize {PageSize}",
                    page, pageSize);
                var disputes = await _disputeService.GetAllDisputesAsync(page, pageSize);

                return Ok(disputes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all disputes");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving disputes" });
            }
        }

        [HttpGet("by-status/{status}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DisputeResponse>>> GetDisputesByStatus(
            DisputeStatus status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Admin retrieving disputes by status {Status}, page {Page}, pageSize {PageSize}",
                    status, page, pageSize);
                var disputes = await _disputeService.GetDisputesByStatusAsync(status, page, pageSize);

                return Ok(disputes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving disputes by status {Status}", status);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving disputes" });
            }
        }

        [HttpGet("assigned-to-me")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DisputeResponse>>> GetAssignedDisputes(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Get assigned disputes failed: UserId was null or empty");
                    return Unauthorized(new { message = "User identification failed" });
                }

                _logger.LogInformation("Retrieving disputes assigned to user {UserId}, page {Page}, pageSize {PageSize}",
                    userId, page, pageSize);

                var assignedDisputes = await _disputeService.GetAssignedDisputesAsync(userId, page, pageSize);

                _logger.LogInformation("User {UserId} retrieved {Count} assigned disputes", userId, assignedDisputes.Count());
                return Ok(assignedDisputes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving assigned disputes for user {UserId}",
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving assigned disputes" });
            }
        }
    }
}