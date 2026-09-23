namespace Internship.Infrastructure.Settings
{
    public class JwtSettings
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int AccessTokenExpiryMinutes { get; set; } = 15;
        public int RefreshTokenExpiryDays { get; set; } = 7;

        // Add this property that was referenced in TokenService
        public int ExpiryMinutes => AccessTokenExpiryMinutes;
    }
}