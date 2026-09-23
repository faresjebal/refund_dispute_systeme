using Internship.Domain.Entities;

namespace Internship.Domain.Interfaces
{
    public interface IDisputeLogRepository
    {
        Task<DisputeLog> CreateAsync(DisputeLog log);
        Task<IEnumerable<DisputeLog>> GetByDisputeIdAsync(string disputeId);
        Task<IEnumerable<DisputeLog>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<DisputeLog>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<DisputeLog>> GetByDateRangeAsync(DateTime from, DateTime to);
    }
}