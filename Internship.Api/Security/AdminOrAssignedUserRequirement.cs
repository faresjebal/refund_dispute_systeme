using Microsoft.AspNetCore.Authorization;

namespace Internship.API.Security
{
    /// <summary>
    /// Authorization requirement for allowing access
    /// to Admins or the user assigned to the dispute.
    /// </summary>
    public class AdminOrAssignedUserRequirement : IAuthorizationRequirement
    {
    }
}
