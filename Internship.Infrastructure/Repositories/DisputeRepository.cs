using Internship.Domain.Entities;
using Internship.Application.Interfaces;
using Internship.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Internship.Infrastructure.Repositories
{
    public class DisputeRepository : IDisputeRepository
    {
        private readonly RefundDisputeContext  _context;

        public DisputeRepository(RefundDisputeContext context)
        {
            _context = context;
        }

        public async Task<Dispute> CreateAsync(Dispute dispute)
        {
            _context.Disputes.Add(dispute);
            await _context.SaveChangesAsync();
            return dispute;
        }

        public async Task<Dispute> UpdateAsync(Dispute dispute)
        {
            _context.Disputes.Update(dispute);
            await _context.SaveChangesAsync();
            return dispute;
        }

        public async Task<Dispute?> GetByIdAsync(string id)
        {
            return await _context.Disputes
                .Include(d => d.Transaction)
                .Include(d => d.User)
                .Include(d => d.AssignedToUser)
                .FirstOrDefaultAsync(d => d.DisputeId == id);
        }

        public async Task<Dispute?> GetByDisputeIdAsync(string disputeId)
        {
            return await _context.Disputes
                .Include(d => d.Transaction)
                .Include(d => d.User)
                .Include(d => d.AssignedToUser)
                .FirstOrDefaultAsync(d => d.DisputeId == disputeId);
        }

        public async Task<IEnumerable<Dispute>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.Disputes
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Dispute>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.Disputes
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }


        // In DisputeRepository implementation
        public async Task<IEnumerable<Dispute>> GetAssignedDisputesAsync(string assignedToUserId, int page = 1, int pageSize = 10)
        {
            return await _context.Disputes
                .Include(d => d.Transaction)
                .Include(d => d.User)
                .Include(d => d.AssignedToUser)
                .Where(d => d.AssignedToUserId == assignedToUserId)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task<IEnumerable<Dispute>> GetByStatusAsync(DisputeStatus status, int page = 1, int pageSize = 10)
        {
            return await _context.Disputes
                .Where(d => d.Status == status)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Dispute>> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.Disputes
                .Where(d => d.TransactionId == transactionId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Dispute>> GetByAssignedUserAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.Disputes
                .Where(d => d.AssignedToUserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> ExistsForTransactionAsync(string transactionId)
        {
            return await _context.Disputes
                .AnyAsync(d => d.TransactionId == transactionId);
        }

       
    }
}