using Internship.API.Security;
using Internship.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Internship.Api.Security
{
    public class AdminOrAssignedUserHandler : AuthorizationHandler<AdminOrAssignedUserRequirement>
    {
        private readonly IDisputeRepository _disputeRepository;
        private readonly ILogger<AdminOrAssignedUserHandler> _logger;

        public AdminOrAssignedUserHandler(IDisputeRepository disputeRepository, ILogger<AdminOrAssignedUserHandler> logger)
        {
            _disputeRepository = disputeRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AdminOrAssignedUserRequirement requirement)
        {
            try
            {
                _logger.LogInformation("Authorization handler called");

                // Get HttpContext from the resource
                var httpContext = context.Resource as HttpContext;

                // If that fails, try to get it from AuthorizationFilterContext
                if (httpContext == null)
                {
                    var filterContext = context.Resource as AuthorizationFilterContext;
                    httpContext = filterContext?.HttpContext;
                }

                if (httpContext == null)
                {
                    _logger.LogWarning("HttpContext is null, failing authorization");
                    context.Fail();
                    return;
                }

                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("User ID: {UserId}", userId ?? "null");

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID is null or empty, failing authorization");
                    context.Fail();
                    return;
                }

                var isAdmin = context.User.IsInRole("Admin");
                _logger.LogInformation("User is admin: {IsAdmin}", isAdmin);

                if (isAdmin)
                {
                    _logger.LogInformation("Admin user, succeeding authorization");
                    context.Succeed(requirement);
                    return;
                }

                if (!httpContext.Request.RouteValues.TryGetValue("disputeId", out var disputeIdObj))
                {
                    _logger.LogWarning("No dispute ID found in route values, failing authorization");
                    context.Fail();
                    return;
                }

                var disputeId = disputeIdObj?.ToString();
                if (string.IsNullOrEmpty(disputeId))
                {
                    _logger.LogWarning("Dispute ID is null or empty, failing authorization");
                    context.Fail();
                    return;
                }

                _logger.LogInformation("Looking up dispute ID: {DisputeId}", disputeId);
                var dispute = await _disputeRepository.GetByIdAsync(disputeId);
                if (dispute == null)
                {
                    _logger.LogWarning("Dispute {DisputeId} not found, failing authorization", disputeId);
                    context.Fail();
                    return;
                }

                _logger.LogInformation("Dispute assigned to: {AssignedUserId}, Current user: {UserId}",
                    dispute.AssignedToUserId ?? "null", userId);

                if (dispute.AssignedToUserId == userId)
                {
                    _logger.LogInformation("User is assigned to dispute, succeeding authorization");
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogWarning("User is not assigned to dispute, failing authorization");
                    context.Fail();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in authorization handler");
                context.Fail();
            }
        }
    }
}
