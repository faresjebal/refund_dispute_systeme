using System.ComponentModel.DataAnnotations;

namespace Internship.Application.DTOs.Auth
{
    public class TokenModel
    {
        [Required]
        public string AccessToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
}