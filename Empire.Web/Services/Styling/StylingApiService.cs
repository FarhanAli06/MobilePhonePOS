using Empire.Web.DTOs.Styling;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.Styling;

public class StylingApiService : IStylingApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<StylingApiService> _logger;
    private const string BaseEndpoint = "/api/styling";

    public StylingApiService(IHttpClientService httpClient, ILogger<StylingApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<StylingDto>?> GetAllAsync()
    {
        try
        {
            return await _httpClient.GetAsync<List<StylingDto>>(BaseEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all stylings");
            return null;
        }
    }

    public async Task<List<StylingSelectionDto>?> GetSelectionsAsync()
    {
        try
        {
            return await _httpClient.GetAsync<List<StylingSelectionDto>>($"{BaseEndpoint}/selections");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting styling selections");
            return null;
        }
    }

    public async Task<StylingDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<StylingDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting styling {Id}", id);
            return null;
        }
    }

    public async Task<StylingDto?> CreateAsync(CreateStylingRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<CreateStylingRequestDto, StylingDto>(BaseEndpoint, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating styling");
            return null;
        }
    }

    public async Task<StylingDto?> UpdateAsync(int id, UpdateStylingRequestDto request)
    {
        try
        {
            return await _httpClient.PutAsync<UpdateStylingRequestDto, StylingDto>($"{BaseEndpoint}/{id}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating styling {Id}", id);
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
            _logger.LogError(ex, "Error deleting styling {Id}", id);
            return false;
        }
    }

    public async Task<bool> CheckDuplicateNameAsync(string name, int? excludeId = null)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/check-duplicate?name={Uri.EscapeDataString(name)}";
            if (excludeId.HasValue)
                endpoint += $"&excludeId={excludeId.Value}";
            return await _httpClient.GetAsync<bool>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking duplicate styling name");
            return false;
        }
    }

    public async Task<object?> GetAssignmentsAsync()
    {
        try
        {
            return await _httpClient.GetAsync<object>($"{BaseEndpoint}/assignments");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting styling assignments");
            return null;
        }
    }

    public async Task<bool> AssignToBrandAsync(int brandId, int? stylingId)
    {
        try
        {
            var result = await _httpClient.PutAsync<AssignStylingRequestDto, object>(
                $"{BaseEndpoint}/assign/brand/{brandId}",
                new AssignStylingRequestDto { StylingId = stylingId });
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning styling to brand {BrandId}", brandId);
            return false;
        }
    }

    public async Task<bool> AssignToCategoryAsync(int categoryId, int? stylingId)
    {
        try
        {
            var result = await _httpClient.PutAsync<AssignStylingRequestDto, object>(
                $"{BaseEndpoint}/assign/category/{categoryId}",
                new AssignStylingRequestDto { StylingId = stylingId });
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning styling to category {CategoryId}", categoryId);
            return false;
        }
    }

    public async Task<bool> AssignToModelAsync(int modelId, int? stylingId)
    {
        try
        {
            var result = await _httpClient.PutAsync<AssignStylingRequestDto, object>(
                $"{BaseEndpoint}/assign/model/{modelId}",
                new AssignStylingRequestDto { StylingId = stylingId });
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning styling to model {ModelId}", modelId);
            return false;
        }
    }
}
