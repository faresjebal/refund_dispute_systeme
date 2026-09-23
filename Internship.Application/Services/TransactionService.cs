using Internship.Application.DTOs.Transaction;
using Internship.Application.Interfaces;
using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Internship.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly ILogger<TransactionService> _logger;
        private readonly Random _random = new();
        private readonly IAuditLogService _auditLogService;

        public TransactionService(
            ITransactionRepository transactionRepository,
            IAuditLogService auditLogService,
            ILogger<TransactionService> logger)
        {
            _transactionRepository = transactionRepository;
            _auditLogService = auditLogService;
            _logger = logger;
        }

        public async Task<TransactionResponse> CreateTransactionAsync(CreateTransactionRequest request, string userId)
        {
            try
            {
                var transactionId = GenerateTransactionId();
                var transaction = new Transaction
                {
                    TransactionId = transactionId,
                    UserId = userId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    Type = TransactionType.Payment,
                    Status = TransactionStatus.Pending,
                    PaymentMethod = request.PaymentMethod,
                    Description = request.Description,
                    MerchantReference = request.MerchantReference,
                    PaymentProviderTransactionId = GenerateProviderTransactionId(),
                    PaymentProviderName = "SimulatedPaymentProvider",
                    MaskedCardNumber = MaskCardNumber(request.CardNumber),
                    CardHolderName = request.CardHolderName,
                    CardType = DetectCardType(request.CardNumber),
                    CreatedAt = DateTime.UtcNow
                };

                var createdTransaction = await _transactionRepository.CreateAsync(transaction);

                await _auditLogService.LogTransactionStatusChangeAsync(
                    createdTransaction.TransactionId,
                    TransactionStatus.Pending,
                    TransactionStatus.Pending,
                    userId,
                    $"Transaction created for amount {request.Amount:C} using {request.PaymentMethod}"
                );

                _logger.LogInformation("Transaction created with ID: {TransactionId} by user {UserId}",
                    createdTransaction.TransactionId, userId);

                return MapToResponse(createdTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction for user {UserId}", userId);
                throw;
            }
        }

        public async Task<TransactionResponse> ProcessTransactionAsync(string transactionId)
        {
            try
            {
                var transaction = await _transactionRepository.GetByIdAsync(transactionId);
                if (transaction == null)
                    throw new ArgumentException("Transaction not found", nameof(transactionId));

                if (transaction.Status != TransactionStatus.Pending)
                    throw new InvalidOperationException($"Transaction is already {transaction.Status}");

                var previousStatus = transaction.Status;
                await SimulatePaymentProcessing();
                var outcome = SimulateTransactionOutcome(transaction);

                transaction.Status = outcome.Status;
                transaction.ProcessedAt = DateTime.UtcNow;

                if (outcome.Status == TransactionStatus.Completed)
                {
                    transaction.CompletedAt = DateTime.UtcNow;
                }
                else if (outcome.Status == TransactionStatus.Failed)
                {
                    transaction.FailedAt = DateTime.UtcNow;
                    transaction.ErrorCode = outcome.ErrorCode;
                    transaction.ErrorMessage = outcome.ErrorMessage;
                }

                var updatedTransaction = await _transactionRepository.UpdateAsync(transaction);

                await _auditLogService.LogTransactionStatusChangeAsync(
                    transaction.TransactionId,
                    previousStatus,
                    outcome.Status,
                    transaction.UserId,
                    outcome.Status == TransactionStatus.Completed
                        ? "Payment processed successfully"
                        : $"Payment failed: {outcome.ErrorMessage}"
                );

                _logger.LogInformation("Transaction {TransactionId} processed by user {UserId} with status: {Status}",
                    transaction.TransactionId, transaction.UserId, transaction.Status);

                return MapToResponse(updatedTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction {TransactionId}", transactionId);
                throw;
            }
        }

        public async Task<TransactionResponse?> GetTransactionAsync(string transactionId)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            return transaction != null ? MapToResponse(transaction) : null;
        }

        public async Task<TransactionResponse?> GetTransactionByReferenceAsync(string transactionId)
        {
            var transaction = await _transactionRepository.GetByTransactionIdAsync(transactionId);
            return transaction != null ? MapToResponse(transaction) : null;
        }

        public async Task<IEnumerable<TransactionResponse>> GetUserTransactionsAsync(string userId, int page = 1, int pageSize = 10)
        {
            var transactions = await _transactionRepository.GetByUserIdAsync(userId, page, pageSize);
            return transactions.Select(MapToResponse);
        }

        public async Task<IEnumerable<TransactionResponse>> GetAllTransactionsAsync(int page = 1, int pageSize = 10)
        {
            var transactions = await _transactionRepository.GetAllAsync(page, pageSize);
            return transactions.Select(MapToResponse);
        }

        public async Task<bool> RefundTransactionAsync(string transactionId, decimal amount, string reason)
        {
            try
            {
                var transaction = await _transactionRepository.GetByIdAsync(transactionId);
                if (transaction == null)
                {
                    _logger.LogWarning("Transaction {TransactionId} not found for refund processing", transactionId);
                    return false;
                }

                if (!transaction.IsRefundable)
                {
                    _logger.LogWarning("Transaction {TransactionId} is not refundable", transactionId);
                    return false;
                }

                if (amount > transaction.Amount)
                {
                    _logger.LogWarning("Refund amount {Amount} exceeds transaction amount {TransactionAmount} for transaction {TransactionId}",
                        amount, transaction.Amount, transactionId);
                    return false;
                }

                if (transaction.Status != TransactionStatus.Completed)
                {
                    _logger.LogWarning("Transaction {TransactionId} is not in Completed status. Current status: {Status}",
                        transactionId, transaction.Status);
                    return false;
                }

                var originalStatus = transaction.Status;
                var refundTransactionId = GenerateTransactionId();

                var refundTransaction = new Transaction
                {
                    TransactionId = refundTransactionId,
                    UserId = transaction.UserId,
                    Amount = amount,
                    Currency = transaction.Currency,
                    Type = TransactionType.Refund,
                    Status = TransactionStatus.Completed,
                    PaymentMethod = transaction.PaymentMethod,
                    Description = $"Refund for transaction {transaction.TransactionId}: {reason}",
                    MerchantReference = transaction.MerchantReference,
                    PaymentProviderTransactionId = GenerateProviderTransactionId(),
                    PaymentProviderName = transaction.PaymentProviderName,
                    CreatedAt = DateTime.UtcNow,
                    ProcessedAt = DateTime.UtcNow,
                    CompletedAt = DateTime.UtcNow
                };

                var createdRefundTransaction = await _transactionRepository.CreateAsync(refundTransaction);

                await _auditLogService.LogTransactionStatusChangeAsync(
                    refundTransaction.TransactionId,
                    TransactionStatus.Pending,
                    TransactionStatus.Completed,
                    transaction.UserId,
                    $"Refund transaction created for original transaction {transactionId}. Amount: {amount:C}. Reason: {reason}"
                );

                transaction.Status = amount >= transaction.Amount
                    ? TransactionStatus.Refunded
                    : TransactionStatus.PartiallyRefunded;

                await _transactionRepository.UpdateAsync(transaction);

                await _auditLogService.LogTransactionStatusChangeAsync(
                    transaction.TransactionId,
                    originalStatus,
                    transaction.Status,
                    transaction.UserId,
                    $"Transaction status updated due to refund processing. Refund amount: {amount:C}. Refund transaction ID: {refundTransactionId}. Reason: {reason}"
                );

                _logger.LogInformation("Refund processed by user {UserId} for transaction {TransactionId}, amount: {Amount}. New status: {Status}",
                    transaction.UserId, transaction.TransactionId, amount, transaction.Status);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);

                try
                {
                    var transaction = await _transactionRepository.GetByIdAsync(transactionId);
                    await _auditLogService.LogTransactionStatusChangeAsync(
                        transactionId,
                        TransactionStatus.Completed,
                        TransactionStatus.Completed,
                        transaction?.UserId,
                        $"Refund processing failed for amount {amount:C}. Error: {ex.Message}. Reason: {reason}"
                    );
                }
                catch
                {
                    // Suppress audit logging errors
                }

                return false;
            }
        }

        public async Task<TransactionResponse> SimulateTransactionOutcomeAsync(string transactionId, TransactionStatus desiredStatus)
        {
            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
                throw new ArgumentException("Transaction not found", nameof(transactionId));

            var previousStatus = transaction.Status;
            transaction.Status = desiredStatus;
            transaction.ProcessedAt = DateTime.UtcNow;

            if (desiredStatus == TransactionStatus.Completed)
            {
                transaction.CompletedAt = DateTime.UtcNow;
            }
            else if (desiredStatus == TransactionStatus.Failed)
            {
                transaction.FailedAt = DateTime.UtcNow;
                transaction.ErrorCode = "SIM_001";
                transaction.ErrorMessage = "Simulated failure for testing";
            }

            var updatedTransaction = await _transactionRepository.UpdateAsync(transaction);

            await _auditLogService.LogTransactionStatusChangeAsync(
                transaction.TransactionId,
                previousStatus,
                desiredStatus,
                transaction.UserId,
                $"Transaction outcome simulated. Status changed to {desiredStatus}"
            );

            return MapToResponse(updatedTransaction);
        }

        public async Task<TransactionResponse> UpdateTransactionStatusAsync(string transactionId, TransactionStatus newStatus, string? userId = null, string? notes = null)
        {
            try
            {
                var transaction = await _transactionRepository.GetByIdAsync(transactionId);
                if (transaction == null)
                    throw new ArgumentException("Transaction not found", nameof(transactionId));

                var previousStatus = transaction.Status;
                transaction.Status = newStatus;

                switch (newStatus)
                {
                    case TransactionStatus.Completed:
                        transaction.CompletedAt = DateTime.UtcNow;
                        break;
                    case TransactionStatus.Failed:
                        transaction.FailedAt = DateTime.UtcNow;
                        break;
                }

                if (transaction.ProcessedAt == null)
                {
                    transaction.ProcessedAt = DateTime.UtcNow;
                }

                var updatedTransaction = await _transactionRepository.UpdateAsync(transaction);

                await _auditLogService.LogTransactionStatusChangeAsync(
                    transaction.TransactionId,
                    previousStatus,
                    newStatus,
                    userId ?? transaction.UserId,
                    notes ?? $"Transaction status updated to {newStatus}"
                );

                _logger.LogInformation("Transaction {TransactionId} status updated by user {UserId} from {PreviousStatus} to {NewStatus}",
                    transactionId, userId ?? transaction.UserId, previousStatus, newStatus);

                return MapToResponse(updatedTransaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction status for {TransactionId}", transactionId);
                throw;
            }
        }

        private async Task SimulatePaymentProcessing()
        {
            await Task.Delay(_random.Next(500, 2000));
        }

        private (TransactionStatus Status, string? ErrorCode, string? ErrorMessage) SimulateTransactionOutcome(Transaction transaction)
        {
            var randomValue = _random.NextDouble();
            if (randomValue < 0.85) return (TransactionStatus.Completed, null, null);

            var failureScenarios = new[]
            {
                ("INSUFFICIENT_FUNDS", "Insufficient funds in the account"),
                ("INVALID_CARD", "Invalid card details provided"),
                ("EXPIRED_CARD", "Card has expired"),
                ("DECLINED", "Transaction declined by issuer"),
                ("NETWORK_ERROR", "Network error during processing")
            };

            var scenario = failureScenarios[_random.Next(failureScenarios.Length)];
            return (TransactionStatus.Failed, scenario.Item1, scenario.Item2);
        }

        private string GenerateTransactionId()
        {
            return $"TXN_{Guid.NewGuid().ToString("N").ToUpper()}";
        }

        private string GenerateProviderTransactionId()
        {
            return $"SPP_{DateTime.UtcNow.Ticks}_{_random.Next(1000, 9999)}";
        }

        private string? MaskCardNumber(string? cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
                return null;

            var cleaned = cardNumber.Replace(" ", "").Replace("-", "");
            return cleaned.Length < 4 ? null : $"****-****-****-{cleaned[^4..]}";
        }

        private string? DetectCardType(string? cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber))
                return null;

            var cleaned = cardNumber.Replace(" ", "").Replace("-", "");

            return cleaned.StartsWith("4") ? "Visa" :
                   cleaned.StartsWith("5") || cleaned.StartsWith("2") ? "Mastercard" :
                   cleaned.StartsWith("3") ? "American Express" : "Unknown";
        }

        private TransactionResponse MapToResponse(Transaction transaction)
        {
            return new TransactionResponse
            {
                TransactionId = transaction.TransactionId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Type = transaction.Type,
                Status = transaction.Status,
                PaymentMethod = transaction.PaymentMethod,
                Description = transaction.Description,
                MerchantReference = transaction.MerchantReference,
                PaymentProviderTransactionId = transaction.PaymentProviderTransactionId,
                PaymentProviderName = transaction.PaymentProviderName,
                MaskedCardNumber = transaction.MaskedCardNumber,
                CardHolderName = transaction.CardHolderName,
                CardType = transaction.CardType,
                CreatedAt = transaction.CreatedAt,
                ProcessedAt = transaction.ProcessedAt,
                CompletedAt = transaction.CompletedAt,
                ErrorMessage = transaction.ErrorMessage,
                ErrorCode = transaction.ErrorCode,
                IsRefundable = transaction.IsRefundable,
                IsDisputable = transaction.IsDisputable
            };
        }
    }
}