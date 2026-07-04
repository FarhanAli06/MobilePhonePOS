using Microsoft.AspNetCore.Authorization;

namespace Empire.Web.Authorization;

public class ShopAccessRequirement : IAuthorizationRequirement
{
    public int ShopId { get; }
    public string? RequiredRole { get; }

    public ShopAccessRequirement(int shopId, string? requiredRole = null)
    {
        ShopId = shopId;
        RequiredRole = requiredRole;
    }
}

