using Internship.Domain.Entities;

namespace Internship.Application.Interfaces
{
    public interface IRefundRequestRepository
    {
        Task<RefundRequest> CreateAsync(RefundRequest refundRequest);
        Task<RefundRequest> UpdateAsync(RefundRequest refundRequest);
        Task<RefundRequest?> GetByIdAsync(string id);
        Task<IEnumerable<RefundRequest>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<RefundRequest>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<RefundRequest>> GetByStatusAsync(RefundStatus status, int page = 1, int pageSize = 10);
        Task<IEnumerable<RefundRequest>> GetByTransactionIdAsync(string transactionId);
        Task<bool> ExistsForTransactionAsync(string transactionId);
        Task<decimal> GetTotalRefundedAmountForTransactionAsync(string transactionId);
    }
}