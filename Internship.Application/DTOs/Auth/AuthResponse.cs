using Internship.Domain.Entities;

namespace Internship.Application.DTOs;

public class AuthResponse
{
    public required string Token { get; set; }
    public required DateTime Expiration { get; set; }
    public required string RefreshToken { get; set; } // Add this
    public required string AccessToken { get; set; }

    public required string Message { get; set; }
    public required bool Success { get; set; }

    public ApplicationUser User { get; set; } = null!; // Use null! if you're sure it will be initialized
}