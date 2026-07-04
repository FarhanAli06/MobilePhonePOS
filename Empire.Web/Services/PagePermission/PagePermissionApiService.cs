using Empire.Web.DTOs.Page;
using Empire.Web.Services.Http;
namespace Empire.Web.Services.PagePermission;
public class PagePermissionApiService : IPagePermissionApiService
{
    private readonly IHttpClientService _http;
    public PagePermissionApiService(IHttpClientService http)
    {
        _http = http;
    }
    public async Task<List<PageDto>> GetAllPagesAsync()
    {
        var result = await _http.GetAsync<List<PageDto>>("api/pages");
        return result ?? new List<PageDto>();
    }
    public async Task<List<PageDto>> GetPagesForUserAsync(int userId, int shopId)
    {
        var result = await _http.GetAsync<List<PageDto>>($"api/pages/user/{userId}?shopId={shopId}");
        return result ?? new List<PageDto>();
    }
    public async Task<UserPermissionSummaryDto?> GetUserPermissionSummaryAsync(int userId, int shopId)
    {
        return await _http.GetAsync<UserPermissionSummaryDto>($"api/pages/user/{userId}/summary?shopId={shopId}");
    }
    public async Task<List<string>> GetGrantedPageKeysAsync(int userId, int shopId)
    {
        var result = await _http.GetAsync<List<string>>($"api/pages/user/{userId}/keys?shopId={shopId}");
        return result ?? new List<string>();
    }
    public async Task<List<string>> GetGrantedPageKeysAsync(int userId, int shopId, string bearerToken)
    {
        var result = await _http.GetWithTokenAsync<List<string>>(
            $"api/pages/user/{userId}/keys?shopId={shopId}", bearerToken);
        return result ?? new List<string>();
    }
    public async Task<bool> AssignPagesToUserAsync(int userId, int shopId, List<int> pageIds)
    {
        var request = new AssignUserPagesRequest { UserId = userId, ShopId = shopId, PageIds = pageIds };
        var response = await _http.PostRawAsync("api/pages/assign", request);
        return response.IsSuccessStatusCode;
    }
}
