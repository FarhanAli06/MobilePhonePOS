namespace Empire.Web.Services.Session;

/// <summary>
/// Helper service for managing session-related operations
/// Abstracts HttpContext.Session access from controllers
/// </summary>
public interface ISessionHelper
{
    /// <summary>
    /// Updates the current shop information in session
    /// </summary>
    void UpdateCurrentShop(int shopId, string shopName);
    
    /// <summary>
    /// Gets the current shop name from session
    /// </summary>
    string GetCurrentShopName();
    
    /// <summary>
    /// Gets the current user name from session
    /// </summary>
    string GetCurrentUserName();
    
    /// <summary>
    /// Gets the current user role from session
    /// </summary>
    string GetCurrentUserRole();
    
    /// <summary>
    /// Clears all session data
    /// </summary>
    void ClearSession();
    
    /// <summary>
    /// Checks if user is authenticated
    /// </summary>
    bool IsAuthenticated();
    
    /// <summary>
    /// Gets the current user ID from session
    /// </summary>
    int GetCurrentUserId();
    
    /// <summary>
    /// Gets the current shop ID from session
    /// </summary>
    int GetCurrentShopId();

    /// <summary>
    /// Gets the comma-separated list of page keys the user is allowed to access.
    /// Returns an empty HashSet if no permissions are stored.
    /// </summary>
    HashSet<string> GetGrantedPageKeys();
}
