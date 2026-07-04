namespace Empire.Web.Services.Auth;

public interface ITokenStorageService
{
    /// <summary>
    /// Store JWT token
    /// </summary>
    void SetToken(string token);
    
    /// <summary>
    /// Retrieve JWT token
    /// </summary>
    string? GetToken();
    
    /// <summary>
    /// Remove JWT token
    /// </summary>
    void RemoveToken();
    
    /// <summary>
    /// Check if user is authenticated (has valid token)
    /// </summary>
    bool IsAuthenticated();
    
    /// <summary>
    /// Get user ID from token
    /// </summary>
    int? GetUserId();
    
    /// <summary>
    /// Get shop ID from token
    /// </summary>
    int? GetShopId();
    
    /// <summary>
    /// Get username from token
    /// </summary>
    string? GetUsername();
}
