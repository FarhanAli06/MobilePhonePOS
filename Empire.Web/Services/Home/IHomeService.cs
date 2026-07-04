using Empire.Web.DTOs.Sale;

namespace Empire.Web.Services.Home;

public interface IHomeService
{
    Task<decimal> GetDailySalesAsync(int shopId, DateTime date);
    Task<decimal> GetDailyProfitAsync(int shopId, DateTime date);
    Task<List<SaleDto>> GetTodaySalesWithItemsAsync(int shopId, DateTime date);
}
