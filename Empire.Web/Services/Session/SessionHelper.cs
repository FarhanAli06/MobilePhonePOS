using System.Linq;

namespace Empire.Web.Services.Session;

/// <summary>
/// Implementation of session helper service
/// </summary>
public class SessionHelper : ISessionHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public SessionHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    
    private ISession Session => _httpContextAccessor.HttpContext?.Session 
        ?? throw new InvalidOperationException("Session is not available");
    
    /// <summary>
    /// Updates the current shop information in session
    /// </summary>
    public void UpdateCurrentShop(int shopId, string shopName)
    {
        Session.SetString("CurrentShopId", shopId.ToString());
        Session.SetString("CurrentShopName", shopName);
    }
    
    /// <summary>
    /// Gets the current shop name from session
    /// </summary>
    public string GetCurrentShopName()
    {
        return Session.GetString("CurrentShopName") ?? "Shop";
    }
    
    /// <summary>
    /// Gets the current user name from session
    /// </summary>
    public string GetCurrentUserName()
    {
        return Session.GetString("UserName") ?? "User";
    }
    
    /// <summary>
    /// Gets the current user role from session
    /// </summary>
    public string GetCurrentUserRole()
    {
        return Session.GetString("UserRole") ?? "User";
    }
    
    /// <summary>
    /// Clears all session data
    /// </summary>
    public void ClearSession()
    {
        Session.Clear();
    }
    
    /// <summary>
    /// Checks if user is authenticated
    /// </summary>
    public bool IsAuthenticated()
    {
        var isAuthenticated = Session.GetString("IsAuthenticated");
        var userId = Session.GetString("UserId");
        return !string.IsNullOrEmpty(isAuthenticated) && 
               isAuthenticated == "true" && 
               !string.IsNullOrEmpty(userId);
    }
    
    /// <summary>
    /// Gets the current user ID from session
    /// </summary>
    public int GetCurrentUserId()
    {
        var userIdStr = Session.GetString("UserId");
        return int.TryParse(userIdStr, out var userId) ? userId : 0;
    }
    
    /// <summary>
    /// Gets the current shop ID from session
    /// </summary>
    public int GetCurrentShopId()
    {
        var shopIdStr = Session.GetString("CurrentShopId");
        return int.TryParse(shopIdStr, out var shopId) ? shopId : 0;
    }

    /// <summary>
    /// Returns the set of page keys the user is allowed to access.
    /// Reads from the comma-separated GrantedPageKeys session value.
    /// </summary>
    public HashSet<string> GetGrantedPageKeys()
    {
        var raw = Session.GetString("GrantedPageKeys");
        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return new HashSet<string>(
            raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(k => k.Trim()),
            StringComparer.OrdinalIgnoreCase);
    }
}
