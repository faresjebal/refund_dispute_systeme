using Internship.Domain.Entities;
using Internship.Application.Interfaces;
using Internship.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Internship.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly RefundDisputeContext _context;

        public TransactionRepository(RefundDisputeContext context)
        {
            _context = context;
        }

        public async Task<Transaction> CreateAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction> UpdateAsync(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction?> GetByIdAsync(string id)
        {
            return await _context.Transactions
                .Include(t => t.User)
                .Include(t => t.RefundRequests)
                .Include(t => t.Disputes)
                .FirstOrDefaultAsync(t => t.TransactionId == id);
        }

        public async Task<Transaction?> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.Transactions
                .Include(t => t.User)
                .Include(t => t.RefundRequests)
                .Include(t => t.Disputes)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<IEnumerable<Transaction>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.Transactions
                .Include(t => t.User)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Transactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetByStatusAsync(TransactionStatus status, int page = 1, int pageSize = 10)
        {
            return await _context.Transactions
                .Include(t => t.User)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _context.Transactions.AnyAsync(t => t.TransactionId == id);
        }

        public async Task<decimal> GetTotalAmountByUserAsync(string userId)
        {
            return await _context.Transactions
                .Where(t => t.UserId == userId && t.Status == TransactionStatus.Completed && t.Type == TransactionType.Payment)
                .SumAsync(t => t.Amount);
        }

        
       
    }
}