using Internship.Domain.Entities;

namespace Internship.Domain.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, ApplicationUser? User, string? Error)> LoginAsync(string email, string password);
        Task<(bool Success, ApplicationUser? User, string? Error)> RegisterAsync(string email, string password, string? firstName = null, string? lastName = null);
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<bool> DeleteUserAsync(string userId);
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    }
}