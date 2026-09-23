using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Internship.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Internship.Infrastructure.Repositories
{
    public class TransactionLogRepository : ITransactionLogRepository
    {
        private readonly RefundDisputeContext _context;

        public TransactionLogRepository(RefundDisputeContext context)
        {
            _context = context;
        }

        public async Task<TransactionLog> CreateAsync(TransactionLog log)
        {
            _context.TransactionLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<IEnumerable<TransactionLog>> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.TransactionLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.Transaction)
                .Where(l => l.TransactionId == transactionId)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<TransactionLog>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.TransactionLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.Transaction)
                .Where(l => l.ChangedByUserId == userId)
                .OrderByDescending(l => l.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<TransactionLog>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.TransactionLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.Transaction)
                .OrderByDescending(l => l.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<TransactionLog>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            return await _context.TransactionLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.Transaction)
                .Where(l => l.ChangedAt >= from && l.ChangedAt <= to)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();
        }
    }
}