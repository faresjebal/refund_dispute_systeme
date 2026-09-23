using Internship.Domain.Entities;

namespace Internship.Domain.Interfaces
{
    public interface ITransactionLogRepository
    {
        Task<TransactionLog> CreateAsync(TransactionLog log);
        Task<IEnumerable<TransactionLog>> GetByTransactionIdAsync(string transactionId);
        Task<IEnumerable<TransactionLog>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10);
        Task<IEnumerable<TransactionLog>> GetAllAsync(int page = 1, int pageSize = 10);
        Task<IEnumerable<TransactionLog>> GetByDateRangeAsync(DateTime from, DateTime to);
    }
}