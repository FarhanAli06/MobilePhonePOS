using Empire.Web.DTOs.Sale;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.Home;

public class HomeService : IHomeService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<HomeService> _logger;
    private const string BaseEndpoint = "/api/sales";

    public HomeService(
        IHttpClientService httpClient,
        ILogger<HomeService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<decimal> GetDailySalesAsync(int shopId, DateTime date)
    {
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync<decimal>($"{BaseEndpoint}/daily-total?shopId={shopId}&date={dateStr}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily sales for shop {ShopId} on {Date}", shopId, date);
            return 0;
        }
    }

    public async Task<decimal> GetDailyProfitAsync(int shopId, DateTime date)
    {
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync<decimal>($"{BaseEndpoint}/daily-profit?shopId={shopId}&date={dateStr}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily profit for shop {ShopId} on {Date}", shopId, date);
            return 0;
        }
    }

    public async Task<List<SaleDto>> GetTodaySalesWithItemsAsync(int shopId, DateTime date)
    {
        try
        {
            var dateStr = date.ToString("yyyy-MM-dd");
            var response = await _httpClient.GetAsync<List<SaleDto>>($"{BaseEndpoint}/by-date?shopId={shopId}&date={dateStr}");
            return response ?? new List<SaleDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting today's sales for shop {ShopId} on {Date}", shopId, date);
            return new List<SaleDto>();
        }
    }
}
