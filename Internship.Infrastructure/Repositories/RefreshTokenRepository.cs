using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Internship.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Internship.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly RefundDisputeContext _context;

        public RefreshTokenRepository(RefundDisputeContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task<RefreshToken?> GetActiveByUserAndTokenAsync(string userId, string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.UserId == userId &&
                                          rt.Token == token &&
                                          rt.RevokedAt == null &&
                                          rt.ExpiresAt > DateTime.UtcNow);
        }

        public Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(string userId)
        {
            return _context.RefreshTokens
                .Where(rt => rt.UserId == userId &&
                             rt.RevokedAt == null &&
                             rt.ExpiresAt > DateTime.UtcNow)
                
                .ToListAsync();
        }

        public Task AddAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(RefreshToken refreshToken)
        {
            _context.Entry(refreshToken).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Remove(refreshToken);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string token)
        {
            return _context.RefreshTokens
                .AnyAsync(rt => rt.Token == token);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        Task IRefreshTokenRepository.SaveChangesAsync()
        {
            return SaveChangesAsync();
        }
    }
}
