using Internship.Application.DTOs.Transaction;
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
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IRefundRequestService _refundRequestService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            ITransactionService transactionService,
            IRefundRequestService refundRequestService,
            ILogger<TransactionController> logger)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _refundRequestService = refundRequestService ?? throw new ArgumentNullException(nameof(refundRequestService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<TransactionResponse>> CreateTransaction([FromBody] CreateTransactionRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Create transaction failed: UserId was null or empty");
                    return Unauthorized(new { message = "User identification failed" });
                }

                _logger.LogInformation("Creating transaction for user {UserId}", userId);
                var response = await _transactionService.CreateTransactionAsync(request, userId);

                _logger.LogInformation("Transaction created successfully for user {UserId}", userId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while creating the transaction" });
            }
        }

        [HttpPost("{transactionId}/process")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TransactionResponse>> ProcessTransaction(string transactionId)
        {
            try
            {
                _logger.LogInformation("Processing transaction {TransactionId}", transactionId);
                var response = await _transactionService.ProcessTransactionAsync(transactionId);

                _logger.LogInformation("Transaction {TransactionId} processed successfully", transactionId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Transaction {TransactionId} not found: {Message}", transactionId, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation for transaction {TransactionId}: {Message}", transactionId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction {TransactionId}", transactionId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while processing the transaction" });
            }
        }



        [HttpPost("{transactionId}/refund")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> ProcessRefund(
    string transactionId,
    [FromBody] ProcessRefundTransactionRequest request)
        {
            try
            {
                var adminUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(adminUserId))
                {
                    _logger.LogWarning("Process refund failed: Admin UserId was null or empty");
                    return Unauthorized(new { message = "Admin identification failed" });
                }

                _logger.LogInformation("Processing refund for transaction {TransactionId}, amount: {Amount}, RefundRequestId: {RefundRequestId}",
                    transactionId, request.Amount, request.RefundRequestId);

                // Validate that the transaction exists first
                var transaction = await _transactionService.GetTransactionAsync(transactionId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction {TransactionId} not found for refund processing", transactionId);
                    return NotFound(new { message = "Transaction not found" });
                }

                // Validate transaction is refundable
                if (!transaction.IsRefundable || transaction.Status != TransactionStatus.Completed)
                {
                    _logger.LogWarning("Transaction {TransactionId} is not refundable. Status: {Status}, IsRefundable: {IsRefundable}",
                        transactionId, transaction.Status, transaction.IsRefundable);
                    return BadRequest(new { message = $"Transaction is not refundable. Status: {transaction.Status}" });
                }

                // Validate refund amount
                if (request.Amount <= 0)
                {
                    _logger.LogWarning("Invalid refund amount {Amount} for transaction {TransactionId}", request.Amount, transactionId);
                    return BadRequest(new { message = "Refund amount must be greater than 0" });
                }

                if (request.Amount > transaction.Amount)
                {
                    _logger.LogWarning("Refund amount {Amount} exceeds transaction amount {TransactionAmount} for transaction {TransactionId}",
                        request.Amount, transaction.Amount, transactionId);
                    return BadRequest(new { message = $"Refund amount cannot exceed transaction amount of {transaction.Amount:C}" });
                }

                // If RefundRequestId is provided, validate it exists and is in Approved status
                if (!string.IsNullOrEmpty(request.RefundRequestId))
                {
                    var refundRequest = await _refundRequestService.GetRefundRequestAsync(request.RefundRequestId);
                    if (refundRequest == null)
                    {
                        _logger.LogWarning("Refund request {RefundRequestId} not found", request.RefundRequestId);
                        return NotFound(new { message = "Refund request not found" });
                    }

                    if (refundRequest.Status != RefundStatus.Approved)
                    {
                        _logger.LogWarning("Refund request {RefundRequestId} is not in Approved status. Current status: {Status}",
                            request.RefundRequestId, refundRequest.Status);
                        return BadRequest(new { message = $"Refund request is not in Approved status. Current status: {refundRequest.Status}" });
                    }

                    // Ensure the refund request belongs to this transaction
                    if (refundRequest.TransactionId != transactionId)
                    {
                        _logger.LogWarning("Refund request {RefundRequestId} does not belong to transaction {TransactionId}. Expected: {ExpectedTransactionId}",
                            request.RefundRequestId, transactionId, refundRequest.TransactionId);
                        return BadRequest(new { message = "Refund request does not belong to this transaction" });
                    }

                    // Validate refund amount matches approved amount
                    var approvedAmount = refundRequest.ApprovedAmount ?? refundRequest.RequestedAmount;
                    if (request.Amount != approvedAmount)
                    {
                        _logger.LogWarning("Refund amount {RequestAmount} does not match approved amount {ApprovedAmount} for refund request {RefundRequestId}",
                            request.Amount, approvedAmount, request.RefundRequestId);
                        return BadRequest(new { message = $"Refund amount must match approved amount of {approvedAmount:C}" });
                    }
                }

                // Process the refund transaction
                var refundReason = !string.IsNullOrEmpty(request.Reason)
                    ? request.Reason
                    : "Manual refund processed by admin";

                var refundSuccess = await _transactionService.RefundTransactionAsync(
                    transactionId,
                    request.Amount,
                    refundReason
                );

                if (refundSuccess)
                {
                    _logger.LogInformation("Refund transaction processed successfully for transaction {TransactionId}", transactionId);

                    // If a refund request ID is provided, update its status to Completed
                    if (!string.IsNullOrEmpty(request.RefundRequestId))
                    {
                        try
                        {
                            var adminNotes = $"Refund transaction completed successfully by admin {adminUserId}. Amount refunded: {request.Amount:C}";
                            if (!string.IsNullOrEmpty(request.Reason))
                            {
                                adminNotes += $" | Reason: {request.Reason}";
                            }

                            var updatedRefundRequest = await _refundRequestService.UpdateRefundStatusAsync(
                                request.RefundRequestId,
                                RefundStatus.Completed,
                                adminNotes
                            );

                            _logger.LogInformation("Refund request {RefundRequestId} updated to Completed status",
                                request.RefundRequestId);

                            return Ok(new
                            {
                                message = "Refund processed successfully and refund request completed",
                                success = true,
                                refundRequestId = request.RefundRequestId,
                                refundRequestStatus = updatedRefundRequest.Status,
                                transactionId = transactionId,
                                refundAmount = request.Amount
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to update refund request {RefundRequestId} status to Completed",
                                request.RefundRequestId);

                            // Return success for the refund but mention the status update issue
                            return Ok(new
                            {
                                message = "Refund processed successfully, but failed to update refund request status",
                                success = true,
                                warning = "Refund request status update failed",
                                transactionId = transactionId,
                                refundAmount = request.Amount,
                                error = ex.Message
                            });
                        }
                    }

                    return Ok(new
                    {
                        message = "Refund processed successfully",
                        success = true,
                        transactionId = transactionId,
                        refundAmount = request.Amount
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to process refund transaction for transaction {TransactionId}", transactionId);

                    // If a refund request ID is provided, update its status to Failed
                    if (!string.IsNullOrEmpty(request.RefundRequestId))
                    {
                        try
                        {
                            var failureNotes = $"Failed to process refund transaction by admin {adminUserId}. Attempted amount: {request.Amount:C}";
                            if (!string.IsNullOrEmpty(request.Reason))
                            {
                                failureNotes += $" | Reason: {request.Reason}";
                            }

                            await _refundRequestService.UpdateRefundStatusAsync(
                                request.RefundRequestId,
                                RefundStatus.Failed,
                                failureNotes
                            );

                            _logger.LogInformation("Refund request {RefundRequestId} updated to Failed status",
                                request.RefundRequestId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to update refund request {RefundRequestId} status to Failed",
                                request.RefundRequestId);
                        }
                    }

                    return BadRequest(new
                    {
                        message = "Failed to process refund transaction",
                        success = false,
                        transactionId = transactionId,
                        attemptedAmount = request.Amount
                    });
                }
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when processing refund for transaction {TransactionId}", transactionId);

                // Update refund request status to Failed if provided
                if (!string.IsNullOrEmpty(request.RefundRequestId))
                {
                    try
                    {
                        await _refundRequestService.UpdateRefundStatusAsync(
                            request.RefundRequestId,
                            RefundStatus.Failed,
                            $"Invalid argument: {ex.Message}"
                        );
                    }
                    catch (Exception statusEx)
                    {
                        _logger.LogError(statusEx, "Failed to update refund request status after argument error");
                    }
                }

                return BadRequest(new { message = ex.Message, success = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);

                // Update refund request status to Failed if provided
                if (!string.IsNullOrEmpty(request.RefundRequestId))
                {
                    try
                    {
                        await _refundRequestService.UpdateRefundStatusAsync(
                            request.RefundRequestId,
                            RefundStatus.Failed,
                            $"Error processing refund: {ex.Message}"
                        );
                    }
                    catch (Exception statusEx)
                    {
                        _logger.LogError(statusEx, "Failed to update refund request status after error");
                    }
                }

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while processing the refund", success = false });
            }
        }

        [HttpGet("{transactionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TransactionResponse>> GetTransaction(string transactionId)
        {
            try
            {
                _logger.LogInformation("Retrieving transaction {TransactionId}", transactionId);
                var transaction = await _transactionService.GetTransactionAsync(transactionId);

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction {TransactionId} not found", transactionId);
                    return NotFound(new { message = "Transaction not found" });
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction {TransactionId}", transactionId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the transaction" });
            }
        }

        [HttpGet("reference/{transactionId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TransactionResponse>> GetTransactionByReference(string transactionId)
        {
            try
            {
                _logger.LogInformation("Retrieving transaction by reference {TransactionReference}", transactionId);
                var transaction = await _transactionService.GetTransactionByReferenceAsync(transactionId);

                if (transaction == null)
                {
                    _logger.LogWarning("Transaction with reference {TransactionReference} not found", transactionId);
                    return NotFound(new { message = "Transaction not found" });
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction by reference {TransactionReference}", transactionId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving the transaction" });
            }
        }

        [HttpGet("my-transactions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetMyTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Get my transactions failed: UserId was null or empty");
                    return Unauthorized(new { message = "User identification failed" });
                }

                _logger.LogInformation("Retrieving transactions for user {UserId}, page {Page}, pageSize {PageSize}",
                    userId, page, pageSize);
                var transactions = await _transactionService.GetUserTransactionsAsync(userId, page, pageSize);

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user transactions");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving transactions" });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetAllTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Admin retrieving all transactions, page {Page}, pageSize {PageSize}",
                    page, pageSize);
                var transactions = await _transactionService.GetAllTransactionsAsync(page, pageSize);

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all transactions");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while retrieving transactions" });
            }
        }

        [HttpPost("{transactionId}/simulate-outcome")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TransactionResponse>> SimulateTransactionOutcome(
            string transactionId,
            [FromBody] TransactionStatus desiredStatus)
        {
            try
            {
                _logger.LogInformation("Admin simulating outcome for transaction {TransactionId} to status {Status}",
                    transactionId, desiredStatus);
                var response = await _transactionService.SimulateTransactionOutcomeAsync(transactionId, desiredStatus);

                _logger.LogInformation("Successfully simulated outcome for transaction {TransactionId}", transactionId);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Transaction {TransactionId} not found for simulation: {Message}", transactionId, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Invalid operation for transaction {TransactionId} simulation: {Message}", transactionId, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error simulating transaction outcome for {TransactionId}", transactionId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred while simulating the transaction outcome" });
            }
        }
    }
}