namespace Empire.Web.Constants;

/// <summary>
/// Constants for session key names
/// </summary>
public static class SessionKeys
{
    /// <summary>
    /// User session keys
    /// </summary>
    public const string UserId = "UserId";
    public const string Username = "Username";
    public const string UserEmail = "UserEmail";
    public const string UserRole = "UserRole";
    public const string UserFullName = "UserFullName";
    
    /// <summary>
    /// Shop session keys
    /// </summary>
    public const string CurrentShopId = "CurrentShopId";
    public const string CurrentShopName = "CurrentShopName";
    public const string ShopCurrency = "ShopCurrency";
    public const string ShopTimezone = "ShopTimezone";
    
    /// <summary>
    /// Company session keys
    /// </summary>
    public const string CompanyId = "CompanyId";
    public const string CompanyName = "CompanyName";
    
    /// <summary>
    /// Authentication session keys
    /// </summary>
    public const string IsAuthenticated = "IsAuthenticated";
    public const string LoginTime = "LoginTime";
    public const string LastActivityTime = "LastActivityTime";
    public const string AuthToken = "AuthToken";
    public const string RefreshToken = "RefreshToken";

    /// <summary>
    /// Page permission session keys
    /// </summary>
    /// <summary>Comma-separated list of page keys the user is allowed to access.</summary>
    public const string GrantedPageKeys = "GrantedPageKeys";
    
    /// <summary>
    /// UI preference session keys
    /// </summary>
    public const string Theme = "Theme";
    public const string Language = "Language";
    public const string PageSize = "PageSize";
    public const string SidebarCollapsed = "SidebarCollapsed";
    
    /// <summary>
    /// Temporary data keys
    /// </summary>
    public const string ReturnUrl = "ReturnUrl";
    public const string ErrorMessage = "ErrorMessage";
    public const string SuccessMessage = "SuccessMessage";
    public const string WarningMessage = "WarningMessage";
    public const string InfoMessage = "InfoMessage";
}
