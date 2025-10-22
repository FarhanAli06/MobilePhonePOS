using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Empire.Domain.Enums;

namespace Empire.Web.Authorization;

public class ShopAccessHandler : AuthorizationHandler<ShopAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ShopAccessRequirement requirement)
    {
        var user = context.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Task.CompletedTask;
        }

        // Check if user has access to the specific shop
        var shopClaims = user.Claims.Where(c => c.Type == "ShopId").ToList();
        var hasShopAccess = shopClaims.Any(c => c.Value == requirement.ShopId.ToString());

        if (!hasShopAccess)
        {
            return Task.CompletedTask;
        }

        // If a specific role is required, check for it
        if (requirement.RequiredRole.HasValue)
        {
            var roleClaimType = $"Shop_{requirement.ShopId}_Role";
            var roleClaim = user.Claims.FirstOrDefault(c => c.Type == roleClaimType);
            
            if (roleClaim == null || !Enum.TryParse<UserRole>(roleClaim.Value, out var userRole))
            {
                return Task.CompletedTask;
            }

            // Manager has access to everything, Technician has limited access
            if (requirement.RequiredRole == UserRole.Manager && userRole != UserRole.Manager)
            {
                return Task.CompletedTask;
            }
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

