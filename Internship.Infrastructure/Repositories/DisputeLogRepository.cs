using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Internship.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Internship.Infrastructure.Repositories
{
    public class DisputeLogRepository : IDisputeLogRepository
    {
        private readonly RefundDisputeContext _context;

        public DisputeLogRepository(RefundDisputeContext context)
        {
            _context = context;
        }

        public async Task<DisputeLog> CreateAsync(DisputeLog log)
        {
            _context.DisputeLogs.Add(log);
            await _context.SaveChangesAsync();
            return log;
        }

        public async Task<IEnumerable<DisputeLog>> GetByDisputeIdAsync(string disputeId)
        {
            return await _context.DisputeLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.Dispute)
                .Where(l => l.DisputeId == disputeId)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<DisputeLog>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.DisputeLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.Dispute)
                .Where(l => l.ChangedByUserId == userId)
                .OrderByDescending(l => l.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<DisputeLog>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.DisputeLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.Dispute)
                .OrderByDescending(l => l.ChangedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<DisputeLog>> GetByDateRangeAsync(DateTime from, DateTime to)
        {
            return await _context.DisputeLogs
                .Include(l => l.ChangedByUser)
                .Include(l => l.Dispute)
                .Where(l => l.ChangedAt >= from && l.ChangedAt <= to)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();
        }
    }
}