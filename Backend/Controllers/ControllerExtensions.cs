using System.Security.Claims;
using Ecommerce.Backend.Services;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Ecommerce.Backend.Controllers;

public static class ControllerExtensions
{
    /// <summary>Reads the authenticated user's id from the JWT "sub" claim.</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(sub, out var id) ? id : throw ApiException.Unauthorized("Invalid token.");
    }
}
