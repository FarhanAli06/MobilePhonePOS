using Empire.Web.Constants;
using Empire.Web.DTOs.Customer;
using Empire.Web.Services.Customer;

namespace Empire.Web.Services;

/// <summary>
/// Implementation of Customer API service for HTTP communication with backend
/// </summary>
public class CustomerApiService : BaseApiService, ICustomerApiService
{
    private readonly ILogger<CustomerApiService> _typedLogger;

    public CustomerApiService(
        HttpClient httpClient,
        ILogger<CustomerApiService> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(httpClient, logger, httpContextAccessor)
    {
        _typedLogger = logger;
    }

    public async Task<IEnumerable<CustomerDto>> GetByShopAsync(int shopId)
    {
        try
        {
            _typedLogger.LogInformation("Getting customers for shop {ShopId}", shopId);
            // The API reads shopId from the JWT claim — no shopId in the URL
            var result = await GetAsync<IEnumerable<CustomerDto>>(
                RouteConstants.ApiPaths.Customers);
            return result ?? Enumerable.Empty<CustomerDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting customers for shop {ShopId}", shopId);
            throw;
        }
    }

    public async Task<CustomerDto?> GetByIdAsync(int customerId)
    {
        try
        {
            _typedLogger.LogInformation("Getting customer {CustomerId}", customerId);
            return await GetAsync<CustomerDto>(
                $"{RouteConstants.ApiPaths.Customers}/{customerId}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<CustomerDto?> CreateAsync(CreateCustomerRequestDto request)
    {
        try
        {
            _typedLogger.LogInformation("Creating new customer");
            return await PostAsync<CreateCustomerRequestDto, CustomerDto>(
                RouteConstants.ApiPaths.Customers, request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error creating customer");
            throw;
        }
    }

    public async Task<CustomerDto?> UpdateAsync(int customerId, UpdateCustomerRequestDto request)
    {
        try
        {
            _typedLogger.LogInformation("Updating customer {CustomerId}", customerId);
            return await PutAsync<UpdateCustomerRequestDto, CustomerDto>(
                $"{RouteConstants.ApiPaths.Customers}/{customerId}", request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error updating customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int customerId)
    {
        try
        {
            _typedLogger.LogInformation("Deleting customer {CustomerId}", customerId);
            return await DeleteAsync(
                $"{RouteConstants.ApiPaths.Customers}/{customerId}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error deleting customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<IEnumerable<CustomerDto>> SearchAsync(string searchTerm, int shopId)
    {
        try
        {
            _typedLogger.LogInformation("Searching customers with term: {SearchTerm}", searchTerm);
            // The API reads shopId from the JWT claim — only pass the search term
            var result = await GetAsync<IEnumerable<CustomerDto>>(
                $"{RouteConstants.ApiPaths.Customers}/search?term={Uri.EscapeDataString(searchTerm ?? string.Empty)}");
            return result ?? Enumerable.Empty<CustomerDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error searching customers");
            throw;
        }
    }

    public async Task<IEnumerable<CustomerDto>> GetCustomersAsync(int shopId)
    {
        return await GetByShopAsync(shopId);
    }
}
