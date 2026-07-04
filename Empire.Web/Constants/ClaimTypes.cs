namespace Empire.Web.Constants;

/// <summary>
/// Constants for custom claim type names
/// </summary>
public static class ClaimTypes
{
    /// <summary>
    /// User-related claims
    /// </summary>
    public const string UserId = "user_id";
    public const string Username = "username";
    public const string Email = "email";
    public const string FullName = "full_name";
    public const string Role = "role";
    
    /// <summary>
    /// Shop-related claims
    /// </summary>
    public const string ShopId = "shop_id";
    public const string ShopName = "shop_name";
    public const string ShopRole = "shop_role";
    
    /// <summary>
    /// Company-related claims
    /// </summary>
    public const string CompanyId = "company_id";
    public const string CompanyName = "company_name";
    
    /// <summary>
    /// Permission claims
    /// </summary>
    public const string Permissions = "permissions";
    public const string CanManageUsers = "can_manage_users";
    public const string CanManageShops = "can_manage_shops";
    public const string CanManageInventory = "can_manage_inventory";
    public const string CanManageRepairs = "can_manage_repairs";
    public const string CanManageSales = "can_manage_sales";
    public const string CanViewReports = "can_view_reports";
    public const string CanManageSettings = "can_manage_settings";
    
    /// <summary>
    /// Standard claim types (from System.Security.Claims.ClaimTypes)
    /// </summary>
    public const string NameIdentifier = System.Security.Claims.ClaimTypes.NameIdentifier;
    public const string Name = System.Security.Claims.ClaimTypes.Name;
    public const string EmailClaim = System.Security.Claims.ClaimTypes.Email;
    public const string RoleClaim = System.Security.Claims.ClaimTypes.Role;
}
