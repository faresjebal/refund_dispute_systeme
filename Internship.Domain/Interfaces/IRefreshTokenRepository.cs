using Internship.Domain.Entities;

namespace Internship.Domain.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task<RefreshToken?> GetActiveByUserAndTokenAsync(string userId, string token);
        Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(string userId);
        Task AddAsync(RefreshToken refreshToken);
        Task UpdateAsync(RefreshToken refreshToken);
        Task DeleteAsync(RefreshToken refreshToken);
        Task<bool> ExistsAsync(string token);
        Task SaveChangesAsync();
    }
}