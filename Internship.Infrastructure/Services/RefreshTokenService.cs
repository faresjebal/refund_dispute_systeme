using Internship.Application.Interfaces;
using Internship.Domain.Entities;
using Internship.Domain.Interfaces;

namespace Internship.Application.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;

        public RefreshTokenService(
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService)
        {
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public async Task CreateRefreshTokenAsync(RefreshToken refreshToken)
        {
            if (refreshToken == null) throw new ArgumentNullException(nameof(refreshToken));

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            return await _refreshTokenRepository.GetByTokenAsync(token);
        }

        public async Task<RefreshToken?> GetActiveRefreshTokenAsync(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token)) return null;

            return await _refreshTokenRepository.GetActiveByUserAndTokenAsync(userId, token);
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            if (refreshToken == null) throw new ArgumentNullException(nameof(refreshToken));

            await _refreshTokenRepository.SaveChangesAsync();
        }

        public async Task DeleteRefreshTokenAsync(RefreshToken refreshToken)
        {
            if (refreshToken == null) throw new ArgumentNullException(nameof(refreshToken));

            await _refreshTokenRepository.DeleteAsync(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(string userId, string ipAddress)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            if (string.IsNullOrEmpty(ipAddress)) throw new ArgumentException("IP Address cannot be null or empty", nameof(ipAddress));

            var token = _tokenService.GenerateRefreshToken();
            if (string.IsNullOrEmpty(token)) throw new InvalidOperationException("Failed to generate refresh token");

            var refreshToken = new RefreshToken
            {
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ipAddress,
                UserId = userId
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            await _refreshTokenRepository.SaveChangesAsync();

            return refreshToken;
        }

        public async Task RevokeRefreshTokenAsync(string token, string ipAddress, string? reason = null)
        {
            if (string.IsNullOrEmpty(token)) return;

            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
            if (refreshToken == null || !refreshToken.IsActive) return;

            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress ?? string.Empty;
            refreshToken.ReasonRevoked = reason ?? "Revoked without replacement";

            await _refreshTokenRepository.SaveChangesAsync();
        }

        public async Task RevokeAllUserRefreshTokensAsync(string userId, string ipAddress)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var userTokens = await _refreshTokenRepository.GetActiveTokensByUserIdAsync(userId);
            if (userTokens == null || !userTokens.Any()) return;

            foreach (var token in userTokens)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedByIp = ipAddress ?? string.Empty;
                token.ReasonRevoked = "All tokens revoked";
            }

            await _refreshTokenRepository.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _refreshTokenRepository.SaveChangesAsync();
        }
    }
}
