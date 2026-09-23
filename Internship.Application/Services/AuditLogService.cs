using Internship.Application.Interfaces;
using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Internship.Application.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly ITransactionLogRepository _transactionLogRepository;
        private readonly IRefundRequestLogRepository _refundLogRepository;
        private readonly IDisputeLogRepository _disputeLogRepository;
        private readonly ILogger<AuditLogService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditLogService(
            ITransactionLogRepository transactionLogRepository,
            IRefundRequestLogRepository refundLogRepository,
            IDisputeLogRepository disputeLogRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<AuditLogService> logger)
        {
            _transactionLogRepository = transactionLogRepository ?? throw new ArgumentNullException(nameof(transactionLogRepository));
            _refundLogRepository = refundLogRepository ?? throw new ArgumentNullException(nameof(refundLogRepository));
            _disputeLogRepository = disputeLogRepository ?? throw new ArgumentNullException(nameof(disputeLogRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task LogTransactionStatusChangeAsync(string transactionId, TransactionStatus previousStatus, TransactionStatus newStatus, string? changedByUserId, string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                _logger.LogWarning("Cannot log transaction status change: TransactionId is null or empty");
                return;
            }

            try
            {
                var log = new TransactionLog
                {
                    TransactionId = transactionId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    Notes = notes,
                    ChangedByUserId = changedByUserId,
                    ChangedAt = DateTime.UtcNow
                };

                await _transactionLogRepository.CreateAsync(log);

                _logger.LogInformation("Transaction status change logged: {TransactionId} from {PreviousStatus} to {NewStatus} by {UserId}",
                    transactionId, previousStatus, newStatus, changedByUserId ?? "System");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log transaction status change for {TransactionId}", transactionId);
                // Don't throw - logging failure shouldn't break the main process
            }
        }

        public async Task LogRefundStatusChangeAsync(string refundId, RefundStatus previousStatus, RefundStatus newStatus, string? changedByUserId, string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(refundId))
            {
                _logger.LogWarning("Cannot log refund status change: RefundId is null or empty");
                return;
            }

            try
            {
                var log = new RefundRequestLog
                {
                    RefundId = refundId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    Notes = notes,
                    ChangedByUserId = changedByUserId,
                    ChangedAt = DateTime.UtcNow
                };

                await _refundLogRepository.CreateAsync(log);

                _logger.LogInformation("Refund status change logged: {RefundId} from {PreviousStatus} to {NewStatus} by {UserId}",
                    refundId, previousStatus, newStatus, changedByUserId ?? "System");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log refund status change for {RefundId}", refundId);
                // Don't throw - logging failure shouldn't break the main process
            }
        }

        public async Task LogDisputeStatusChangeAsync(string disputeId, DisputeStatus previousStatus, DisputeStatus newStatus, string? changedByUserId, string? notes = null)
        {
            if (string.IsNullOrWhiteSpace(disputeId))
            {
                _logger.LogWarning("Cannot log dispute status change: DisputeId is null or empty");
                return;
            }

            try
            {
                var log = new DisputeLog
                {
                    DisputeId = disputeId,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    Notes = notes,
                    ChangedByUserId = changedByUserId,
                    ChangedAt = DateTime.UtcNow
                };

                await _disputeLogRepository.CreateAsync(log);

                _logger.LogInformation("Dispute status change logged: {DisputeId} from {PreviousStatus} to {NewStatus} by {UserId}",
                    disputeId, previousStatus, newStatus, changedByUserId ?? "System");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log dispute status change for {DisputeId}", disputeId);
                // Don't throw - logging failure shouldn't break the main process
            }
        }

        public async Task<IEnumerable<TransactionLog>> GetTransactionAuditLogsAsync(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                return Enumerable.Empty<TransactionLog>();

            try
            {
                return await _transactionLogRepository.GetByTransactionIdAsync(transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction audit logs for {TransactionId}", transactionId);
                return Enumerable.Empty<TransactionLog>();
            }
        }

        public async Task<IEnumerable<RefundRequestLog>> GetRefundAuditLogsAsync(string refundId)
        {
            if (string.IsNullOrWhiteSpace(refundId))
                return Enumerable.Empty<RefundRequestLog>();

            try
            {
                return await _refundLogRepository.GetByRefundIdAsync(refundId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refund audit logs for {RefundId}", refundId);
                return Enumerable.Empty<RefundRequestLog>();
            }
        }

        public async Task<IEnumerable<DisputeLog>> GetDisputeAuditLogsAsync(string disputeId)
        {
            if (string.IsNullOrWhiteSpace(disputeId))
                return Enumerable.Empty<DisputeLog>();

            try
            {
                return await _disputeLogRepository.GetByDisputeIdAsync(disputeId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dispute audit logs for {DisputeId}", disputeId);
                return Enumerable.Empty<DisputeLog>();
            }
        }

        public async Task<IEnumerable<object>> GetUserAuditActivityAsync(string userId, int page = 1, int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return Enumerable.Empty<object>();

            try
            {
                var activities = new List<object>();

                // Get transaction logs
                var transactionLogs = await _transactionLogRepository.GetByUserIdAsync(userId, page, pageSize);
                activities.AddRange(transactionLogs.Select(log => new
                {
                    Type = "Transaction",
                    Id = log.TransactionId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedBy = GetUserDisplayName(log.ChangedByUser)
                }));

                // Get refund logs
                var refundLogs = await _refundLogRepository.GetByUserIdAsync(userId, page, pageSize);
                activities.AddRange(refundLogs.Select(log => new
                {
                    Type = "Refund",
                    Id = log.RefundId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedBy = GetUserDisplayName(log.ChangedByUser)
                }));

                // Get dispute logs
                var disputeLogs = await _disputeLogRepository.GetByUserIdAsync(userId, page, pageSize);
                activities.AddRange(disputeLogs.Select(log => new
                {
                    Type = "Dispute",
                    Id = log.DisputeId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedBy = GetUserDisplayName(log.ChangedByUser)
                }));

                return activities.OrderByDescending(a => ((dynamic)a).ChangedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user audit activity for {UserId}", userId);
                return Enumerable.Empty<object>();
            }
        }

        public async Task<IEnumerable<object>> GetAllAuditLogsAsync(DateTime? from = null, DateTime? to = null, string? userId = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var activities = new List<object>();
                var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
                var toDate = to ?? DateTime.UtcNow;

                // If userId is specified, use the existing method that filters by user
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    return await GetUserAuditActivityAsync(userId, page, pageSize);
                }

                // If userId is specified, use the existing method that filters by user
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    return await GetUserAuditActivityAsync(userId, page, pageSize);
                }

                // Get all transaction logs in date range
                var transactionLogs = await _transactionLogRepository.GetByDateRangeAsync(fromDate, toDate);
                activities.AddRange(transactionLogs.Select(log => new
                {
                    Type = "Transaction",
                    Id = log.TransactionId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedBy = GetUserDisplayName(log.ChangedByUser),
                    TransactionAmount = log.Transaction?.Amount
                }));

                // Get all refund logs in date range
                var refundLogs = await _refundLogRepository.GetByDateRangeAsync(fromDate, toDate);
                activities.AddRange(refundLogs.Select(log => new
                {
                    Type = "Refund",
                    Id = log.RefundId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedBy = GetUserDisplayName(log.ChangedByUser),
                    RefundAmount = log.RefundRequest?.RequestedAmount
                }));

                // Get all dispute logs in date range
                var disputeLogs = await _disputeLogRepository.GetByDateRangeAsync(fromDate, toDate);
                activities.AddRange(disputeLogs.Select(log => new
                {
                    Type = "Dispute",
                    Id = log.DisputeId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedBy = GetUserDisplayName(log.ChangedByUser)
                }));

                // Apply pagination and ordering
                var orderedActivities = activities
                    .OrderByDescending(a => ((dynamic)a).ChangedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                return orderedActivities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs from {FromDate} to {ToDate} for user {UserId}", from, to, userId);
                return Enumerable.Empty<object>();
            }
        }

        public async Task<int> GetAuditLogsCountAsync(DateTime? from = null, DateTime? to = null, string? userId = null)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
                var toDate = to ?? DateTime.UtcNow;

                // If userId is specified, get counts for that specific user
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var userLogs = await GetUserAuditActivityAsync(userId, 1, int.MaxValue);
                    return userLogs.Count();
                }

                // If userId is specified, get counts for that specific user
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var userLogs = await GetUserAuditActivityAsync(userId, 1, int.MaxValue);
                    return userLogs.Count();
                }

                var transactionLogs = await _transactionLogRepository.GetByDateRangeAsync(fromDate, toDate);
                var refundLogs = await _refundLogRepository.GetByDateRangeAsync(fromDate, toDate);
                var disputeLogs = await _disputeLogRepository.GetByDateRangeAsync(fromDate, toDate);

                return transactionLogs.Count() + refundLogs.Count() + disputeLogs.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs count from {FromDate} to {ToDate} for user {UserId}", from, to, userId);
                return 0;
            }
        }

        public async Task<IEnumerable<object>> GetAuditLogsByTypeAsync(string type, DateTime? from = null, DateTime? to = null, string? userId = null, int page = 1, int pageSize = 10)
        {
            try
            {
                var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
                var toDate = to ?? DateTime.UtcNow;

                return type.ToLower() switch
                {
                    "transaction" => await GetTransactionAuditLogsByDateRangeAsync(fromDate, toDate, userId, page, pageSize),
                    "refund" => await GetRefundAuditLogsByDateRangeAsync(fromDate, toDate, userId, page, pageSize),
                    "dispute" => await GetDisputeAuditLogsByDateRangeAsync(fromDate, toDate, userId, page, pageSize),
                    _ => Enumerable.Empty<object>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit logs by type {Type} for user {UserId}", type, userId);
                return Enumerable.Empty<object>();
            }
        }

        private async Task<IEnumerable<object>> GetTransactionAuditLogsByDateRangeAsync(DateTime from, DateTime to, string? userId, int page, int pageSize)
        {
            IEnumerable<TransactionLog> logs;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                logs = await _transactionLogRepository.GetByUserIdAsync(userId, 1, int.MaxValue);
                logs = logs.Where(log => log.ChangedAt >= from && log.ChangedAt <= to);
            }
            else
            {
                logs = await _transactionLogRepository.GetByDateRangeAsync(from, to);
            }

            return logs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new
                {
                    Type = "Transaction",
                    Id = log.TransactionId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedBy = GetUserDisplayName(log.ChangedByUser),
                    TransactionAmount = log.Transaction?.Amount
                });
        }

        private async Task<IEnumerable<object>> GetRefundAuditLogsByDateRangeAsync(DateTime from, DateTime to, string? userId, int page, int pageSize)
        {
            IEnumerable<RefundRequestLog> logs;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                logs = await _refundLogRepository.GetByUserIdAsync(userId, 1, int.MaxValue);
                logs = logs.Where(log => log.ChangedAt >= from && log.ChangedAt <= to);
            }
            else
            {
                logs = await _refundLogRepository.GetByDateRangeAsync(from, to);
            }

            return logs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new
                {
                    Type = "Refund",
                    Id = log.RefundId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedBy = GetUserDisplayName(log.ChangedByUser),
                    RefundAmount = log.RefundRequest?.RequestedAmount
                });
        }

        private async Task<IEnumerable<object>> GetDisputeAuditLogsByDateRangeAsync(DateTime from, DateTime to, string? userId, int page, int pageSize)
        {
            IEnumerable<DisputeLog> logs;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                logs = await _disputeLogRepository.GetByUserIdAsync(userId, 1, int.MaxValue);
                logs = logs.Where(log => log.ChangedAt >= from && log.ChangedAt <= to);
            }
            else
            {
                logs = await _disputeLogRepository.GetByDateRangeAsync(from, to);
            }

            return logs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new
                {
                    Type = "Dispute",
                    Id = log.DisputeId,
                    PreviousStatus = log.PreviousStatus.ToString(),
                    NewStatus = log.NewStatus.ToString(),
                    Notes = log.Notes,
                    ChangedAt = log.ChangedAt,
                    ChangedBy = GetUserDisplayName(log.ChangedByUser)
                });
        }

        private static string GetUserDisplayName(ApplicationUser? user)
        {
            if (user == null) return "System";

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrEmpty(fullName) ? user.UserName ?? "Unknown" : fullName;
        }
    }
}