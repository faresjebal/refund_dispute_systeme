using Internship.Domain.Entities;

namespace Internship.Application.Interfaces
{
    public interface IDisputeRepository
    {
        Task<Dispute> CreateAsync(Dispute dispute);
        Task<Dispute> UpdateAsync(Dispute dispute);
        Task<Dispute?> GetByIdAsync(string id);
        Task<IEnumerable<Dispute>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<Dispute>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<Dispute>> GetAssignedDisputesAsync(string assignedToUserId, int page = 1, int pageSize = 10);
        Task<IEnumerable<Dispute>> GetByStatusAsync(DisputeStatus status, int page = 1, int pageSize = 10);
        Task<IEnumerable<Dispute>> GetByTransactionIdAsync(string transactionId);
        Task<IEnumerable<Dispute>> GetByAssignedUserAsync(string userId, int page = 1, int pageSize = 10);
        Task<bool> ExistsForTransactionAsync(string transactionId);
    }
}