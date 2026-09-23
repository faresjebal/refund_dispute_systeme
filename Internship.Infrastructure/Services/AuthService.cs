using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Internship.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
        }

        public async Task<(bool Success, ApplicationUser? User, string? Error)> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, null, "Email is required");
            if (string.IsNullOrWhiteSpace(password))
                return (false, null, "Password is required");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null || !user.IsActive)
                return (false, null, "Invalid credentials");

            var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
                return (false, null, "Invalid credentials");

            return (true, user, null);
        }

        public async Task<(bool Success, ApplicationUser? User, string? Error)> RegisterAsync(string email, string password, string? firstName = null, string? lastName = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return (false, null, "Email is required");
            if (string.IsNullOrWhiteSpace(password))
                return (false, null, "Password is required");

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
                return (false, null, "Email already in use");

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return (false, null, errors);
            }

            await _userManager.AddToRoleAsync(user, "User");
            return (true, user, null);
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _userManager.FindByIdAsync(userId);
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            return await _userManager.GetRolesAsync(user) ?? new List<string>();
        }
    }
}