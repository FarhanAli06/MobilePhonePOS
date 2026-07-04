using Microsoft.AspNetCore.Authorization;

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
        if (!string.IsNullOrEmpty(requirement.RequiredRole))
        {
            var roleClaimType = $"Shop_{requirement.ShopId}_Role";
            var roleClaim = user.Claims.FirstOrDefault(c => c.Type == roleClaimType);
            
            if (roleClaim == null || string.IsNullOrEmpty(roleClaim.Value))
            {
                return Task.CompletedTask;
            }

            // Manager has access to everything, Technician has limited access
            if (requirement.RequiredRole.Equals("Manager", StringComparison.OrdinalIgnoreCase) && 
                !roleClaim.Value.Equals("Manager", StringComparison.OrdinalIgnoreCase))
            {
                return Task.CompletedTask;
            }
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

