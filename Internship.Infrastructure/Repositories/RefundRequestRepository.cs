using Internship.Domain.Entities;
using Internship.Application.Interfaces;
using Internship.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Internship.Infrastructure.Repositories
{
    public class RefundRequestRepository : IRefundRequestRepository
    {
        private readonly RefundDisputeContext _context;

        public RefundRequestRepository(RefundDisputeContext context)
        {
            _context = context;
        }

        public async Task<RefundRequest> CreateAsync(RefundRequest refundRequest)
        {
            _context.RefundRequests.Add(refundRequest);
            await _context.SaveChangesAsync();
            return refundRequest;
        }

        public async Task<RefundRequest> UpdateAsync(RefundRequest refundRequest)
        {
            _context.RefundRequests.Update(refundRequest);
            await _context.SaveChangesAsync();
            return refundRequest;
        }

        public async Task<RefundRequest?> GetByIdAsync(string id)
        {
            return await _context.RefundRequests
                .Include(r => r.Transaction)
                .Include(r => r.User)
                .Include(r => r.ProcessedByUser)
                .FirstOrDefaultAsync(r => r.RefundId == id);
        }

        public async Task<IEnumerable<RefundRequest>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
        {
            return await _context.RefundRequests
                .Include(r => r.Transaction)
                .Include(r => r.User)
                .Include(r => r.ProcessedByUser)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<RefundRequest>> GetAllAsync(int page = 1, int pageSize = 10)
        {
            return await _context.RefundRequests
                .Include(r => r.Transaction)
                .Include(r => r.User)
                .Include(r => r.ProcessedByUser)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<RefundRequest>> GetByStatusAsync(RefundStatus status, int page = 1, int pageSize = 10)
        {
            return await _context.RefundRequests
                .Include(r => r.Transaction)
                .Include(r => r.User)
                .Include(r => r.ProcessedByUser)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<RefundRequest>> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.RefundRequests
                .Include(r => r.Transaction)
                .Include(r => r.User)
                .Include(r => r.ProcessedByUser)
                .Where(r => r.TransactionId == transactionId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ExistsForTransactionAsync(string transactionId)
        {
            return await _context.RefundRequests
                .AnyAsync(r => r.TransactionId == transactionId);
        }

        public async Task<decimal> GetTotalRefundedAmountForTransactionAsync(string transactionId)
        {
            return await _context.RefundRequests
                .Where(r => r.TransactionId == transactionId &&
                           (r.Status == RefundStatus.Approved || r.Status == RefundStatus.Completed))
                .SumAsync(r => r.ApprovedAmount ?? 0);
        }
    }
}