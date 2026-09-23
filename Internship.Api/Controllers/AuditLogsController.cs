using Internship.Application.DTOs;
using Internship.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Internship.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(
            IAuditLogService auditLogService,
            ILogger<AuditLogsController> logger)
        {
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all audit logs with optional filtering by date range, type, and user ID
        /// </summary>
        /// <param name="from">Start date for filtering (optional)</param>
        /// <param name="to">End date for filtering (optional)</param>
        /// <param name="type">Type of logs: Transaction, Refund, or Dispute (optional)</param>
        /// <param name="userId">User ID to filter by (optional)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 20)</param>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaginatedAuditLogResponse>> GetAll(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? type,
            [FromQuery] string? userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                _logger.LogInformation("Admin retrieving audit logs with filters - From: {From}, To: {To}, Type: {Type}, UserId: {UserId}, Page: {Page}, PageSize: {PageSize}",
                    from, to, type, userId, page, pageSize);

                IEnumerable<object> logs;
                int totalCount;

                if (!string.IsNullOrEmpty(type))
                {
                    // Get logs by specific type with optional date and user filtering
                    logs = await _auditLogService.GetAuditLogsByTypeAsync(type, from, to, userId, page, pageSize);
                    totalCount = await _auditLogService.GetAuditLogsCountAsync(from, to, userId);
                }
                else
                {
                    // Get all logs with optional date and user filtering
                    logs = await _auditLogService.GetAllAuditLogsAsync(from, to, userId, page, pageSize);
                    totalCount = await _auditLogService.GetAuditLogsCountAsync(from, to, userId);
                }

                var response = new PaginatedAuditLogResponse
                {
                    Items = logs,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                _logger.LogInformation("Successfully retrieved {Count} audit logs", logs.Count());
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving audit logs" });
            }
        }

        /// <summary>
        /// Get audit logs for a specific transaction
        /// </summary>
        [HttpGet("transactions/{transactionId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TransactionLogResponse>>> GetTransactionLogs(string transactionId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(transactionId))
                {
                    _logger.LogWarning("Get transaction logs failed: TransactionId is null or empty");
                    return BadRequest(new { message = "Transaction ID is required" });
                }

                _logger.LogInformation("Retrieving transaction logs for {TransactionId}", transactionId);
                var logs = await _auditLogService.GetTransactionAuditLogsAsync(transactionId);

                if (!logs.Any())
                {
                    _logger.LogWarning("No transaction logs found for {TransactionId}", transactionId);
                    return NotFound(new { message = "No audit logs found for this transaction" });
                }

                var response = logs.Select(log => new TransactionLogResponse
                {
                    Id = log.Id,
                    TransactionId = log.TransactionId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedByUserId = log.ChangedByUserId,
                    ChangedBy = log.ChangedByUser != null
                        ? $"{log.ChangedByUser.FirstName} {log.ChangedByUser.LastName}".Trim()
                        : "System"
                });

                _logger.LogInformation("Successfully retrieved {Count} transaction logs for {TransactionId}", response.Count(), transactionId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction logs for {TransactionId}", transactionId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving transaction logs" });
            }
        }

        /// <summary>
        /// Get audit logs for a specific refund
        /// </summary>
        [HttpGet("refunds/{refundId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<RefundLogResponse>>> GetRefundLogs(string refundId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(refundId))
                {
                    _logger.LogWarning("Get refund logs failed: RefundId is null or empty");
                    return BadRequest(new { message = "Refund ID is required" });
                }

                _logger.LogInformation("Retrieving refund logs for {RefundId}", refundId);
                var logs = await _auditLogService.GetRefundAuditLogsAsync(refundId);

                if (!logs.Any())
                {
                    _logger.LogWarning("No refund logs found for {RefundId}", refundId);
                    return NotFound(new { message = "No audit logs found for this refund" });
                }

                var response = logs.Select(log => new RefundLogResponse
                {
                    Id = log.Id,
                    RefundId = log.RefundId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedByUserId = log.ChangedByUserId,
                    ChangedBy = log.ChangedByUser != null
                        ? $"{log.ChangedByUser.FirstName} {log.ChangedByUser.LastName}".Trim()
                        : "System"
                });

                _logger.LogInformation("Successfully retrieved {Count} refund logs for {RefundId}", response.Count(), refundId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refund logs for {RefundId}", refundId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving refund logs" });
            }
        }

        /// <summary>
        /// Get audit logs for a specific dispute
        /// </summary>
        [HttpGet("disputes/{disputeId}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DisputeLogResponse>>> GetDisputeLogs(string disputeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(disputeId))
                {
                    _logger.LogWarning("Get dispute logs failed: DisputeId is null or empty");
                    return BadRequest(new { message = "Dispute ID is required" });
                }

                _logger.LogInformation("Retrieving dispute logs for {DisputeId}", disputeId);
                var logs = await _auditLogService.GetDisputeAuditLogsAsync(disputeId);

                if (!logs.Any())
                {
                    _logger.LogWarning("No dispute logs found for {DisputeId}", disputeId);
                    return NotFound(new { message = "No audit logs found for this dispute" });
                }

                var response = logs.Select(log => new DisputeLogResponse
                {
                    Id = log.Id,
                    DisputeId = log.DisputeId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedByUserId = log.ChangedByUserId,
                    ChangedBy = log.ChangedByUser != null
                        ? $"{log.ChangedByUser.FirstName} {log.ChangedByUser.LastName}".Trim()
                        : "System"
                });

                _logger.LogInformation("Successfully retrieved {Count} dispute logs for {DisputeId}", response.Count(), disputeId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dispute logs for {DisputeId}", disputeId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving dispute logs" });
            }
        }
    }
}