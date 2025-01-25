using System.Security.Claims;
using TournamentSystem.Domain.Exceptions;

namespace TournamentSystem.Application.Helpers
{
    public static class ClaimsHelper
    {
        public static string GetClaimValue(ClaimsPrincipal user, string claimType)
        {
            return user?.FindFirst(claimType)?.Value;
        }

        public static int GetUserId(ClaimsPrincipal user)
        {
            var userIdString = GetClaimValue(user, ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString))
            {
                throw new UnauthorizedException("User ID is missing or invalid.");
            }

            return int.Parse(userIdString);
        }

        public static string GetUserEmail(ClaimsPrincipal user)
        {
            return GetClaimValue(user, ClaimTypes.Email);
        }

        public static string GetUserName(ClaimsPrincipal user)
        {
            return GetClaimValue(user, ClaimTypes.Name);
        }

        public static string GetUserRole(ClaimsPrincipal user)
        {
            return GetClaimValue(user, ClaimTypes.Role);
        }
    }
}
