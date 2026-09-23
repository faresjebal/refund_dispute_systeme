using Internship.Domain.Entities;

namespace Internship.Domain.Interfaces
{
    public interface IRefundRequestLogRepository
    {
        Task<RefundRequestLog> CreateAsync(RefundRequestLog log);
        Task<IEnumerable<RefundRequestLog>> GetByRefundIdAsync(string refundId);
        Task<IEnumerable<RefundRequestLog>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<RefundRequestLog>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<RefundRequestLog>> GetByDateRangeAsync(DateTime from, DateTime to);
    }
}
