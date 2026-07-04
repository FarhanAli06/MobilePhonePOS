using Empire.Web.DTOs.Lookup;
using Empire.Web.Services.Http;
using Empire.Web.Services.LookupValue;

namespace Empire.Web.Services.API;

/// <summary>
/// API service for LookupValue operations
/// </summary>
public class LookupValueApiService : ILookupValueApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<LookupValueApiService> _logger;
    private const string BaseEndpoint = "/api/lookups";

    public LookupValueApiService(
        IHttpClientService httpClient,
        ILogger<LookupValueApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<LookupValueDto>?> GetAllAsync(string? category = null, string? type = null)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(category))
                queryParams.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(type))
                queryParams.Add($"type={Uri.EscapeDataString(type)}");

            var endpoint = BaseEndpoint;
            if (queryParams.Any())
                endpoint += $"?{string.Join("&", queryParams)}";
                
            return await _httpClient.GetAsync<List<LookupValueDto>>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup values");
            return null;
        }
    }

    public async Task<LookupValueDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<LookupValueDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup value {Id}", id);
            return null;
        }
    }

    public async Task<LookupValueDto?> CreateAsync(CreateLookupValueRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<CreateLookupValueRequestDto, LookupValueDto>(BaseEndpoint, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lookup value");
            return null;
        }
    }

    public async Task<LookupValueDto?> UpdateAsync(int id, UpdateLookupValueRequestDto request)
    {
        try
        {
            return await _httpClient.PutAsync<UpdateLookupValueRequestDto, LookupValueDto>($"{BaseEndpoint}/{id}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lookup value {Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            return await _httpClient.DeleteAsync($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lookup value {Id}", id);
            return false;
        }
    }
}
