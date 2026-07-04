using Empire.Web.Constants;
using Empire.Web.DTOs.Repair;

namespace Empire.Web.Services.Repair;

/// <summary>
/// Implementation of Repair API service for HTTP communication with backend
/// </summary>
public class RepairApiService : BaseApiService, IRepairApiService
{
    private readonly ILogger<RepairApiService> _typedLogger;

    public RepairApiService(
        HttpClient httpClient,
        ILogger<RepairApiService> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(httpClient, logger, httpContextAccessor)
    {
        _typedLogger = logger;
    }

    public async Task<IEnumerable<RepairDto>> GetByShopAsync(int shopId)
    {
        try
        {
            _typedLogger.LogInformation("Getting repairs for shop {ShopId}", shopId);
            var result = await GetAsync<IEnumerable<RepairDto>>(
                $"{RouteConstants.ApiPaths.Repairs}/shop/{shopId}");
            return result ?? Enumerable.Empty<RepairDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting repairs for shop {ShopId}", shopId);
            throw;
        }
    }

    public async Task<RepairDto?> GetByIdAsync(int repairId)
    {
        try
        {
            _typedLogger.LogInformation("Getting repair {RepairId}", repairId);
            return await GetAsync<RepairDto>(
                $"{RouteConstants.ApiPaths.Repairs}/{repairId}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting repair {RepairId}", repairId);
            throw;
        }
    }

    public async Task<RepairDto?> CreateAsync(CreateRepairRequest request)
    {
        try
        {
            _typedLogger.LogInformation("Creating new repair for customer {CustomerId}", request.CustomerId);
            return await PostAsync<CreateRepairRequest, RepairDto>(
                RouteConstants.ApiPaths.Repairs, request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error creating repair");
            throw;
        }
    }

    public async Task<RepairDto?> UpdateAsync(int repairId, UpdateRepairRequestDto request)
    {
        try
        {
            _typedLogger.LogInformation("Updating repair {RepairId}", repairId);
            return await PutAsync<UpdateRepairRequestDto, RepairDto>(
                $"{RouteConstants.ApiPaths.Repairs}/{repairId}", request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error updating repair {RepairId}", repairId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int repairId)
    {
        try
        {
            _typedLogger.LogInformation("Deleting repair {RepairId}", repairId);
            return await DeleteAsync(
                $"{RouteConstants.ApiPaths.Repairs}/{repairId}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error deleting repair {RepairId}", repairId);
            throw;
        }
    }

    public async Task<IEnumerable<RepairDto>> GetByCustomerAsync(int customerId, int shopId)
    {
        try
        {
            _typedLogger.LogInformation("Getting repairs for customer {CustomerId}", customerId);
            var result = await GetAsync<IEnumerable<RepairDto>>(
                $"{RouteConstants.ApiPaths.Repairs}/customer/{customerId}?shopId={shopId}");
            return result ?? Enumerable.Empty<RepairDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting repairs for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<bool> AddPartsToRepairAsync(int repairId, object request)
    {
        try
        {
            _typedLogger.LogInformation("Adding parts to repair {RepairId}", repairId);
            var result = await PostAsync<object, object>(
                $"{RouteConstants.ApiPaths.Repairs}/{repairId}/parts", request);
            return result != null;
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error adding parts to repair {RepairId}", repairId);
            throw;
        }
    }

    public async Task<IEnumerable<RepairDto>> SearchAsync(string searchTerm)
    {
        try
        {
            _typedLogger.LogInformation("Searching repairs with term {SearchTerm}", searchTerm);
            var result = await GetAsync<IEnumerable<RepairDto>>(
                $"{RouteConstants.ApiPaths.Repairs}/search?term={Uri.EscapeDataString(searchTerm)}");
            return result ?? Enumerable.Empty<RepairDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error searching repairs");
            throw;
        }
    }

    public async Task<IEnumerable<RepairDto>> GetByCustomerIdAsync(int customerId)
    {
        try
        {
            _typedLogger.LogInformation("Getting repairs for customer {CustomerId}", customerId);
            var result = await GetAsync<IEnumerable<RepairDto>>(
                $"{RouteConstants.ApiPaths.Repairs}/customer/{customerId}");
            return result ?? Enumerable.Empty<RepairDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting repairs for customer {CustomerId}", customerId);
            throw;
        }
    }

    public async Task<IEnumerable<RepairDto>> GetByStatusAsync(string status)
    {
        try
        {
            _typedLogger.LogInformation("Getting repairs with status {Status}", status);
            var result = await GetAsync<IEnumerable<RepairDto>>(
                $"{RouteConstants.ApiPaths.Repairs}/status/{status}");
            return result ?? Enumerable.Empty<RepairDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting repairs by status");
            throw;
        }
    }

    public async Task<Microsoft.AspNetCore.Mvc.Rendering.SelectList> GetRepairStatusesAsync()
    {
        try
        {
            var statuses = await GetAsync<IEnumerable<Empire.Web.DTOs.Lookup.LookupValueDto>>(
                $"{RouteConstants.ApiPaths.Lookups}/category/RepairStatus");
            return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                statuses ?? Enumerable.Empty<Empire.Web.DTOs.Lookup.LookupValueDto>(), 
                "Value", "DisplayName");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting repair statuses");
            return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(Enumerable.Empty<object>());
        }
    }

    public async Task<Microsoft.AspNetCore.Mvc.Rendering.SelectList> GetPaymentStatusesAsync()
    {
        try
        {
            var statuses = await GetAsync<IEnumerable<Empire.Web.DTOs.Lookup.LookupValueDto>>(
                $"{RouteConstants.ApiPaths.Lookups}/category/PaymentStatus");
            return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                statuses ?? Enumerable.Empty<Empire.Web.DTOs.Lookup.LookupValueDto>(), 
                "Value", "DisplayName");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting payment statuses");
            return new Microsoft.AspNetCore.Mvc.Rendering.SelectList(Enumerable.Empty<object>());
        }
    }

    public async Task<bool> ReplacePartsAsync(int repairId, object request)
    {
        try
        {
            _typedLogger.LogInformation("Replacing parts on repair {RepairId}", repairId);
            var result = await PutAsync<object, object>(
                $"{RouteConstants.ApiPaths.Repairs}/{repairId}/parts", request);
            return result != null;
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error replacing parts on repair {RepairId}", repairId);
            throw;
        }
    }

    public async Task<IEnumerable<RepairDto>> GetRepairsAsync(int shopId)
    {
        return await GetByShopAsync(shopId);
    }

    public async Task<IEnumerable<RepairPartDto>> GetRepairPartsAsync(int repairId)
    {
        try
        {
            _typedLogger.LogInformation("Getting parts for repair {RepairId}", repairId);
            var result = await GetAsync<IEnumerable<RepairPartDto>>(
                $"{RouteConstants.ApiPaths.Repairs}/{repairId}/parts");
            return result ?? Enumerable.Empty<RepairPartDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting parts for repair {RepairId}", repairId);
            return Enumerable.Empty<RepairPartDto>();
        }
    }

    public async Task<IEnumerable<RepairActivityDto>> GetRepairActivitiesAsync(int repairId)
    {
        try
        {
            _typedLogger.LogInformation("Getting activities for repair {RepairId}", repairId);
            var result = await GetAsync<IEnumerable<RepairActivityDto>>(
                $"{RouteConstants.ApiPaths.Repairs}/{repairId}/activities");
            return result ?? Enumerable.Empty<RepairActivityDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting activities for repair {RepairId}", repairId);
            return Enumerable.Empty<RepairActivityDto>();
        }
    }

    // ── Payment Ledger ────────────────────────────────────────────────────────

    public async Task<IEnumerable<RepairPaymentDto>> GetRepairPaymentsAsync(int repairId)
    {
        try
        {
            var result = await GetAsync<IEnumerable<RepairPaymentDto>>(
                $"{RouteConstants.ApiPaths.Repairs}/{repairId}/payments");
            return result ?? Enumerable.Empty<RepairPaymentDto>();
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error getting payments for repair {RepairId}", repairId);
            return Enumerable.Empty<RepairPaymentDto>();
        }
    }

    public async Task<RepairPaymentDto?> AddRepairPaymentAsync(int repairId, AddRepairPaymentRequestDto request)
    {
        try
        {
            return await PostAsync<AddRepairPaymentRequestDto, RepairPaymentDto>(
                $"{RouteConstants.ApiPaths.Repairs}/{repairId}/payments", request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error adding payment to repair {RepairId}", repairId);
            return null;
        }
    }

    public async Task<RepairPaymentDto?> UpdateRepairPaymentAsync(int paymentId, UpdateRepairPaymentRequestDto request)
    {
        try
        {
            return await PutAsync<UpdateRepairPaymentRequestDto, RepairPaymentDto>(
                $"{RouteConstants.ApiPaths.Repairs}/payments/{paymentId}", request);
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error updating payment {PaymentId}", paymentId);
            return null;
        }
    }

    public async Task<bool> DeleteRepairPaymentAsync(int paymentId)
    {
        try
        {
            return await DeleteAsync($"{RouteConstants.ApiPaths.Repairs}/payments/{paymentId}");
        }
        catch (Exception ex)
        {
            _typedLogger.LogError(ex, "Error deleting payment {PaymentId}", paymentId);
            return false;
        }
    }
}

// DTO for adding parts to repair
public class AddRepairPartsRequest
{
    /// <summary>Legacy flat list — quantity defaults to 1 each.</summary>
    public List<int> InventoryPartIds { get; set; } = new();

    /// <summary>Detailed lines with explicit quantity and unit price. Takes precedence over InventoryPartIds when provided.</summary>
    public List<Empire.Web.DTOs.Repair.RepairPartRequest>? Parts { get; set; }

    public int ShopId { get; set; }
    public int UserId { get; set; }
}
