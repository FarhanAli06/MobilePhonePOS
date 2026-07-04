using Empire.Web.DTOs.Role;
using Empire.Web.Services.Role;

namespace Empire.Web.Services;

/// <summary>
/// Implementation of Role API service for HTTP communication with backend
/// </summary>
public class RoleApiService : BaseApiService, IRoleApiService
{
    private readonly ILogger<RoleApiService> _typedLogger;
    private const string BaseEndpoint = "api/roles";

    public RoleApiService(
        HttpClient httpClient,
        ILogger<RoleApiService> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(httpClient, logger, httpContextAccessor)
    {
        _typedLogger = logger;
    }

    public async Task<List<RoleDto>> GetAllAsync()
    {
        try
        {
            _typedLogger.LogInformation("Getting all roles");
            var result = await GetAsync<List<RoleDto>>(BaseEndpoint);
            return result ?? new List<RoleDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting all roles");
            return new List<RoleDto>();
        }
    }

    public async Task<List<RoleDto>> GetActiveAsync()
    {
        try
        {
            _typedLogger.LogInformation("Getting active roles");
            var result = await GetAsync<List<RoleDto>>($"{BaseEndpoint}/active");
            return result ?? new List<RoleDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting active roles");
            return new List<RoleDto>();
        }
    }

    public async Task<RoleDto?> GetByIdAsync(int id)
    {
        try
        {
            _typedLogger.LogInformation("Getting role {Id}", id);
            var result = await GetAsync<RoleDto>($"{BaseEndpoint}/{id}");
            return result;
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting role {Id}", id);
            return null;
        }
    }
}
