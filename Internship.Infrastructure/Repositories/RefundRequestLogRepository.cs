using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Internship.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Internship.Infrastructure.Repositories
{
    public class RefundRequestLogRepository : IRefundRequestLogRepository
    {
        private readonly RefundDisputeContext _context;

        public RefundRequestLogRepository(RefundDisputeContext context)
        {
            _context = context;
        }

        public async Task<RefundRequestLog> CreateAsync(RefundRequestLog log)
        {
            _context.RefundRequestLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<IEnumerable<RefundRequestLog>> GetByRefundIdAsync(string refundId)
        {
            return await _context.RefundRequestLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.RefundRequest)
                .Where(l => l.RefundId == refundId)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<RefundRequestLog>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.RefundRequestLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.RefundRequest)
                .Where(l => l.ChangedByUserId == userId)
                .OrderByDescending(l => l.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<RefundRequestLog>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.RefundRequestLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.RefundRequest)
                .OrderByDescending(l => l.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<RefundRequestLog>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            return await _context.RefundRequestLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.RefundRequest)
                .Where(l => l.ChangedAt >= from && l.ChangedAt <= to)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();
        }
    }
}