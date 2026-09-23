using Internship.Application.DTOs.Auth;
using Internship.Application.Interfaces;
using Internship.Domain.Entities;
using Internship.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace Internship.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableCors("AllowAngularApp")] // Add this attribute

    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IRefreshTokenService refreshTokenService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(nameof(refreshTokenService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "User with this email already exists" });
                }

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName ?? string.Empty,
                    LastName = request.LastName ?? string.Empty,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        message = "Registration failed",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }

                await _userManager.AddToRoleAsync(user, "User");

                _logger.LogInformation("User {Email} registered successfully", request.Email);
                return Ok(new { message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {Email}", request.Email);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    return BadRequest(new { message = "Invalid email or password" });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    if (result.IsLockedOut)
                    {
                        return BadRequest(new { message = "Account is locked due to multiple failed attempts" });
                    }
                    return BadRequest(new { message = "Invalid email or password" });
                }

                var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
                var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(user.Id, GetIpAddress());

                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                var tokenModel = new TokenModel
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    TokenType = "Bearer"
                };

                _logger.LogInformation("User {Email} logged in successfully", request.Email);
                return Ok(tokenModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging in user {Email}", request.Email);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred during login" });
            }
        }

        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return BadRequest(new { message = "Invalid access token" });

                var refreshToken = await _refreshTokenService.GetActiveRefreshTokenAsync(userId, request.RefreshToken);

                if (refreshToken == null)
                {
                    _logger.LogWarning("Refresh token not found for user {UserId}. Token: {Token}", userId, request.RefreshToken);
                    return BadRequest(new { message = "Invalid refresh token" });
                }

                if (!refreshToken.IsActive)
                {
                    return BadRequest(new { message = "Refresh token is expired or revoked" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null || !user.IsActive)
                    return BadRequest(new { message = "User not found or inactive" });

                var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user);
                var newRefreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(userId, GetIpAddress());

                // Revoke the current token using the tracked entity
                refreshToken.RevokedAt = DateTime.UtcNow;
                refreshToken.RevokedByIp = GetIpAddress();
                refreshToken.ReasonRevoked = "Replaced by new token";

                await _refreshTokenService.SaveChangesAsync();

                var tokenModel = new TokenModel
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken.Token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(15),
                    TokenType = "Bearer"
                };

                _logger.LogInformation("Token refreshed for user {UserId}", userId);
                return Ok(tokenModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return BadRequest(new { message = "Token refresh failed" });
            }
        }



        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation(">>> Entered Logout endpoint");

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Extracted UserId: {UserId}", userId);

                if (!string.IsNullOrEmpty(userId))
                {
                    await _refreshTokenService.RevokeAllUserRefreshTokensAsync(userId, GetIpAddress());
                    _logger.LogInformation("User {UserId} logged out successfully", userId);
                }
                else
                {
                    _logger.LogWarning("Logout failed: UserId was null or empty");
                }

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "An error occurred during logout" });
            }
        }

        [HttpGet("me")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser()
        {
            _logger.LogInformation(">>> Entered GetCurrentUser (me) endpoint");

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Extracted UserId: {UserId}", userId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Invalid user — no UserId found in claims");
                    return BadRequest(new { message = "Invalid user" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found with id: {UserId}", userId);
                    return NotFound(new { message = "User not found" });
                }

                var userRoles = await _userManager.GetRolesAsync(user);

                _logger.LogInformation("Successfully retrieved user data for userId: {UserId}", userId);

                return Ok(new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    roles = userRoles,
                    lastLogin = user.LastLoginAt,
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Failed to get user information" });
            }
        }

        [HttpPost("change-password")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Null check for the request
            if (request == null)
            {
                _logger.LogWarning("[ChangePassword] Request body is null");
                return BadRequest(new { Message = "Request body cannot be null" });
            }

            _logger.LogInformation("[ChangePassword] Password change request initiated");

            // Model validation
            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                _logger.LogWarning("[ChangePassword] Validation failed with {ErrorCount} errors", validationErrors.Count);
                return BadRequest(new { Message = "Validation failed", Errors = validationErrors });
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("[ChangePassword] Missing user ID in claims");
                    return BadRequest(new { Message = "User identification failed" });
                }

                // Password confirmation check
                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    _logger.LogWarning("[ChangePassword] Password confirmation mismatch");
                    return BadRequest(new { Message = "New password and confirmation do not match" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError("[ChangePassword] User {UserId} not found", userId);
                    return BadRequest(new { Message = "User account not found" });
                }

                // Attempt password change
                var result = await _userManager.ChangePasswordAsync(
                    user,
                    request.CurrentPassword,
                    request.NewPassword
                );

                if (!result.Succeeded)
                {
                    _logger.LogError("[ChangePassword] Password change failed");
                    return BadRequest(new
                    {
                        Message = "Password change failed",
                        Errors = result.Errors.Select(e => e.Description)
                    });
                }

                // Revoke tokens without tracking count
                var ipAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                _logger.LogInformation("[ChangePassword] Revoking all refresh tokens for {UserId}", userId);
                await _refreshTokenService.RevokeAllUserRefreshTokensAsync(userId, ipAddress);

                _logger.LogInformation("[ChangePassword] Password changed successfully for {UserId}", userId);
                return Ok(new { Message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ChangePassword] Critical error during password change");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred during password change" });
            }
        }

        private string GetIpAddress()
        {
            if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                return forwardedFor.ToString();
            }
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "Unknown";
        }
        
    }
}