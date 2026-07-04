using Empire.Web.DTOs.Page;

namespace Empire.Web.Services.PagePermission;

public interface IPagePermissionApiService
{
    Task<List<PageDto>> GetAllPagesAsync();

    /// <summary>Get all pages with the user's assignment status for a specific shop.</summary>
    Task<List<PageDto>> GetPagesForUserAsync(int userId, int shopId);

    /// <summary>Get the user's full permission summary for a specific shop.</summary>
    Task<UserPermissionSummaryDto?> GetUserPermissionSummaryAsync(int userId, int shopId);

    /// <summary>Get the list of page keys granted to the user in a specific shop.</summary>
    Task<List<string>> GetGrantedPageKeysAsync(int userId, int shopId);

    /// <summary>Overload that accepts an explicit bearer token — used immediately after login
    /// before the token is committed to session/cookie.</summary>
    Task<List<string>> GetGrantedPageKeysAsync(int userId, int shopId, string bearerToken);

    /// <summary>Assign a set of pages to a user for a specific shop.</summary>
    Task<bool> AssignPagesToUserAsync(int userId, int shopId, List<int> pageIds);
}
