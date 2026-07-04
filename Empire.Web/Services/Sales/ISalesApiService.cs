using Empire.Web.DTOs.Sale;
using Empire.Web.DTOs.Reports;

namespace Empire.Web.Services.Sales
{
    public interface ISalesApiService
    {
        Task<List<SaleDto>?> GetByShopAsync(int shopId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<SaleDto?> GetByIdAsync(int id);
        Task<ProfitReportDto?> GetProfitReportAsync(int shopId, DateTime startDate, DateTime endDate);
        Task<SaleSummaryDto?> GetSaleSummaryAsync(int shopId, DateTime startDate, DateTime endDate);
        Task<SalesReportDto?> GetSalesReportAsync(int shopId, DateTime startDate, DateTime endDate);
        Task<object?> GetSaleDetailAsync(int saleId);
        Task<object?> AddSalePaymentAsync(int saleId, string paymentMethod, decimal amount, string? transactionId);
        Task<bool> UpdateSalePaymentAsync(int paymentId, string paymentMethod, decimal amount, string? transactionId);
        Task<bool> DeleteSalePaymentAsync(int paymentId);
    }
}
