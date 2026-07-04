using Empire.Web.Services.Http;
using Empire.Web.DTOs.Sale;
using Empire.Web.DTOs.Reports;

namespace Empire.Web.Services.Sales;

/// <summary>
/// API service for Sales operations
/// </summary>
public class SalesApiService : ISalesApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<SalesApiService> _logger;
    private const string BaseEndpoint = "/api/sales";

    public SalesApiService(
        IHttpClientService httpClient,
        ILogger<SalesApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<SaleDto>?> GetByShopAsync(int shopId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var queryParams = new List<string> { $"shopId={shopId}" };
            if (fromDate.HasValue)
                queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
            if (toDate.HasValue)
                queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

            var endpoint = $"{BaseEndpoint}?{string.Join("&", queryParams)}";
            return await _httpClient.GetAsync<List<SaleDto>>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales for shop {ShopId}", shopId);
            return null;
        }
    }

    public async Task<SaleDto?> GetByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetAsync<SaleDto>($"{BaseEndpoint}/{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale {Id}", id);
            return null;
        }
    }

    public async Task<ProfitReportDto?> GetProfitReportAsync(int shopId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/profit-report?shopId={shopId}&startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
            return await _httpClient.GetAsync<ProfitReportDto>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profit report");
            return null;
        }
    }

    public async Task<SaleSummaryDto?> GetSaleSummaryAsync(int shopId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/sale-summary?shopId={shopId}&startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
            return await _httpClient.GetAsync<SaleSummaryDto>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale summary");
            return null;
        }
    }

    public async Task<SalesReportDto?> GetSalesReportAsync(int shopId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var endpoint = $"{BaseEndpoint}/sales-report-data?shopId={shopId}&startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
            return await _httpClient.GetAsync<SalesReportDto>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales report data");
            return null;
        }
    }

    public async Task<object?> GetSaleDetailAsync(int saleId)
    {
        try
        {
            return await _httpClient.GetAsync<object>($"{BaseEndpoint}/{saleId}/detail");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale detail {SaleId}", saleId);
            return null;
        }
    }

    public async Task<object?> AddSalePaymentAsync(int saleId, string paymentMethod, decimal amount, string? transactionId)
    {
        try
        {
            var body = new { paymentMethod, amount, transactionId };
            return await _httpClient.PostAsync<object, object>($"{BaseEndpoint}/{saleId}/payments", body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to sale {SaleId}", saleId);
            return null;
        }
    }

    public async Task<bool> UpdateSalePaymentAsync(int paymentId, string paymentMethod, decimal amount, string? transactionId)
    {
        try
        {
            var body = new { paymentMethod, amount, transactionId };
            var result = await _httpClient.PutAsync<object, object>($"{BaseEndpoint}/payments/{paymentId}", body);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sale payment {PaymentId}", paymentId);
            return false;
        }
    }

    public async Task<bool> DeleteSalePaymentAsync(int paymentId)
    {
        try
        {
            return await _httpClient.DeleteAsync($"{BaseEndpoint}/payments/{paymentId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sale payment {PaymentId}", paymentId);
            return false;
        }
    }
}
