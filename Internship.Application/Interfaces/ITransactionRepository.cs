using Internship.Domain.Entities;

namespace Internship.Application.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction> CreateAsync(Transaction transaction);
        Task<Transaction> UpdateAsync(Transaction transaction);
        Task<Transaction?> GetByIdAsync(string id);
        Task<Transaction?> GetByTransactionIdAsync(string transactionId);
        Task<IEnumerable<Transaction>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<Transaction>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<Transaction>> GetByStatusAsync(TransactionStatus status, int page = 1, int pageSize = 10);
        Task<bool> ExistsAsync(string id);
        Task<decimal> GetTotalAmountByUserAsync(string userId);
    }
}