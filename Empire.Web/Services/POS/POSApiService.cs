using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.Inventory;
using Empire.Web.DTOs.Pos;
using Empire.Web.DTOs.Repair;
using Empire.Web.Services.Http;
using Empire.Web.Services.POS;

namespace Empire.Web.Services.API;

/// <summary>
/// API service for POS (Point of Sale) operations
/// </summary>
public class POSApiService: IPOSApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<POSApiService> _logger;
    private const string BaseEndpoint = "/api/pos";

    public POSApiService(
        IHttpClientService httpClient,
        ILogger<POSApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<POSProductDto>?> SearchProductsAsync(POSProductSearchRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<POSProductSearchRequestDto, List<POSProductDto>>($"{BaseEndpoint}/products/search", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return null;
        }
    }

    public async Task<List<POSCustomerDto>?> SearchCustomersAsync(int shopId, string searchTerm)
    {
        try
        {
            return await _httpClient.GetAsync<List<POSCustomerDto>>($"{BaseEndpoint}/customers?shopId={shopId}&search={searchTerm}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching customers");
            return null;
        }
    }

    public async Task<List<POSRepairDto>?> SearchRepairsAsync(int shopId, string searchTerm)
    {
        try
        {
            return await _httpClient.GetAsync<List<POSRepairDto>>($"{BaseEndpoint}/repairs?shopId={shopId}&search={searchTerm}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching repairs");
            return null;
        }
    }

    public async Task<POSSaleResponseDto?> CreateSaleAsync(POSCreateSaleRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<POSCreateSaleRequestDto, POSSaleResponseDto>($"{BaseEndpoint}/sales", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sale");
            return null;
        }
    }

    public async Task<POSSaleResponseDto?> UpdateSaleAsync(int saleId, POSUpdateSaleRequestDto request)
    {
        try
        {
            return await _httpClient.PutAsync<POSUpdateSaleRequestDto, POSSaleResponseDto>($"{BaseEndpoint}/sales/{saleId}", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sale");
            return null;
        }
    }

    public async Task<POSSaleDetailDto?> GetSaleAsync(int saleId)
    {
        try
        {
            return await _httpClient.GetAsync<POSSaleDetailDto>($"{BaseEndpoint}/sales/{saleId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale {SaleId}", saleId);
            return null;
        }
    }

    public async Task<POSSaleDetailDto?> GetLastSaleAsync(int shopId)
    {
        try
        {
            return await _httpClient.GetAsync<POSSaleDetailDto>($"{BaseEndpoint}/sales/last?shopId={shopId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last sale");
            return null;
        }
    }

    public async Task<string?> GenerateInvoiceNumberAsync(int shopId)
    {
        try
        {
            var result = await _httpClient.GetAsync<InvoiceNumberDto>($"{BaseEndpoint}/invoice-number?shopId={shopId}");
            return result?.InvoiceNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice number");
            return null;
        }
    }

    public async Task<List<POSInventoryDto>?> GetInventoryAsync(POSInventorySearchRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<POSInventorySearchRequestDto, List<POSInventoryDto>>($"{BaseEndpoint}/inventory/search", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory");
            return null;
        }
    }

    public async Task<List<POSDeviceDto>?> GetDevicesAsync(POSDeviceSearchRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<POSDeviceSearchRequestDto, List<POSDeviceDto>>($"{BaseEndpoint}/devices/search", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting devices");
            return null;
        }
    }

    public async Task<List<POSSaleDto>?> GetSalesAsync(POSSalesSearchRequestDto request)
    {
        try
        {
            return await _httpClient.PostAsync<POSSalesSearchRequestDto, List<POSSaleDto>>($"{BaseEndpoint}/sales/search", request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales");
            return null;
        }
    }

    public async Task<List<DeviceDto>> SearchDevicesAsync(string? brand = null, string? category = null, string? model = null)
    {
        try
        {
            var query = $"{BaseEndpoint}/devices/search?";
            if (!string.IsNullOrEmpty(brand)) query += $"brand={Uri.EscapeDataString(brand)}&";
            if (!string.IsNullOrEmpty(category)) query += $"category={Uri.EscapeDataString(category)}&";
            if (!string.IsNullOrEmpty(model)) query += $"model={Uri.EscapeDataString(model)}&";
            
            var result = await _httpClient.GetAsync<List<DeviceDto>>(query.TrimEnd('&'));
            return result ?? new List<DeviceDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching devices");
            return new List<DeviceDto>();
        }
    }

    public async Task<List<InventoryItemDto>> SearchInventoryAsync(string? brand = null, string? category = null, string? model = null)
    {
        try
        {
            var query = $"{BaseEndpoint}/inventory/search?";
            if (!string.IsNullOrEmpty(brand)) query += $"brand={Uri.EscapeDataString(brand)}&";
            if (!string.IsNullOrEmpty(category)) query += $"category={Uri.EscapeDataString(category)}&";
            if (!string.IsNullOrEmpty(model)) query += $"model={Uri.EscapeDataString(model)}&";
            
            var result = await _httpClient.GetAsync<List<InventoryItemDto>>(query.TrimEnd('&'));
            return result ?? new List<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching inventory");
            return new List<InventoryItemDto>();
        }
    }

    public async Task<bool> ProcessPaymentAsync(int saleId, PaymentRequestDto request)
    {
        try
        {
            var result = await _httpClient.PostAsync<PaymentRequestDto, object>($"{BaseEndpoint}/sales/{saleId}/payment", request);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for sale {SaleId}", saleId);
            return false;
        }
    }
}
