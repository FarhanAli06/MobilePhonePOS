using Empire.Web.Constants;
using Empire.Web.DTOs.Lookup;
using Empire.Web.Services.Lookup;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empire.Web.Services;

/// <summary>
/// Implementation of Lookup API service for HTTP communication with backend
/// </summary>
public class LookupApiService : BaseApiService, ILookupApiService
{
    private readonly ILogger<LookupApiService> _typedLogger;

    public LookupApiService(
        HttpClient httpClient,
        ILogger<LookupApiService> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(httpClient, logger, httpContextAccessor)
    {
        _typedLogger = logger;
    }

    public async Task<IEnumerable<LookupValueDto>> GetLookupsByCategoryAsync(string category)
    {
        try
        {
            _typedLogger.LogInformation("Getting lookups for category {Category}", category);
            var result = await GetAsync<IEnumerable<LookupValueDto>>(
                $"{RouteConstants.ApiPaths.Lookups}/category/{category}");
            return result ?? Enumerable.Empty<LookupValueDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting lookups for category {Category}", category);
            throw;
        }
    }

    public async Task<IEnumerable<LookupValueDto>> GetAllLookupsAsync()
    {
        try
        {
            _typedLogger.LogInformation("Getting all lookups");
            var result = await GetAsync<IEnumerable<LookupValueDto>>(
                RouteConstants.ApiPaths.Lookups);
            return result ?? Enumerable.Empty<LookupValueDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting all lookups");
            throw;
        }
    }

    public async Task<SelectList> GetLookupSelectListAsync(string category, object? selectedValue = null)
    {
        try
        {
            _typedLogger.LogInformation("Getting lookup select list for category {Category}", category);
            var lookups = await GetLookupsByCategoryAsync(category);
            return new SelectList(lookups, "Value", "Description", selectedValue);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting lookup select list for category {Category}", category);
            throw;
        }
    }

    // Interface methods
    public async Task<IEnumerable<LookupValueDto>> GetByCategoryAsync(string category)
    {
        return await GetLookupsByCategoryAsync(category);
    }

    public async Task<SelectList> GetSelectListAsync(string category, string? selectedValue = null)
    {
        return await GetLookupSelectListAsync(category, selectedValue);
    }
}
