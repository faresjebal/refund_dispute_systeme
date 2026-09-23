using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Internship.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public  ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        // Computed property
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Refresh token properties
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}