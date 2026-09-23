using Internship.Domain.Entities;

namespace Internship.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        Task CreateRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetRefreshTokenAsync(string token);
        Task<RefreshToken?> GetActiveRefreshTokenAsync(string userId, string token);
        Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
        Task DeleteRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken> GenerateRefreshTokenAsync(string userId, string ipAddress);
        Task RevokeRefreshTokenAsync(string token, string ipAddress, string reason = "Revoked without replacement");
        Task RevokeAllUserRefreshTokensAsync(string userId, string ipAddress);
        Task SaveChangesAsync();
    }
}