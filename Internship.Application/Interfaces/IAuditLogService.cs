using Internship.Domain.Entities;

public interface IAuditLogService
{
    // Logging methods
    Task LogTransactionStatusChangeAsync(string transactionId, TransactionStatus previousStatus,
        TransactionStatus newStatus, string? changedByUserId, string? notes = null);
    Task LogRefundStatusChangeAsync(string refundId, RefundStatus previousStatus,
        RefundStatus newStatus, string? changedByUserId, string? notes = null);
    Task LogDisputeStatusChangeAsync(string disputeId, DisputeStatus previousStatus,
        DisputeStatus newStatus, string? changedByUserId, string? notes = null);

    // Specific entity log retrieval
    Task<IEnumerable<TransactionLog>> GetTransactionAuditLogsAsync(string transactionId);
    Task<IEnumerable<RefundRequestLog>> GetRefundAuditLogsAsync(string refundId);
    Task<IEnumerable<DisputeLog>> GetDisputeAuditLogsAsync(string disputeId);

    // General log retrieval
    Task<IEnumerable<object>> GetUserAuditActivityAsync(string userId, int page = 1, int pageSize = 10);
    Task<IEnumerable<object>> GetAllAuditLogsAsync(DateTime? from = null, DateTime? to = null,
        string? userId = null, int page = 1, int pageSize = 10);
    Task<int> GetAuditLogsCountAsync(DateTime? from = null, DateTime? to = null, string? userId = null);
    Task<IEnumerable<object>> GetAuditLogsByTypeAsync(string type, DateTime? from = null,
        DateTime? to = null, string? userId = null, int page = 1, int pageSize = 10);
}