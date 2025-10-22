using Microsoft.AspNetCore.Authorization;
using Empire.Domain.Enums;

namespace Empire.Web.Authorization;

public class ShopAccessRequirement : IAuthorizationRequirement
{
    public int ShopId { get; }
    public UserRole? RequiredRole { get; }

    public ShopAccessRequirement(int shopId, UserRole? requiredRole = null)
    {
        ShopId = shopId;
        RequiredRole = requiredRole;
    }
}

