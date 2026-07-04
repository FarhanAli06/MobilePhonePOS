using Empire.Application.DTOs.Sale;
using Empire.Application.DTOs.Inventory;

namespace Empire.Application.Interfaces;

public interface IPOSService
{
    Task<IEnumerable<SaleDto>> GetSalesByShopAsync(int shopId);
    Task<SaleDto?> GetSaleByIdAsync(int id);
    Task<IEnumerable<SaleDto>> GetByShopAsync(int shopId);
    Task<IEnumerable<InventoryDto>> GetAvailableProductsAsync(int shopId);
    Task<SaleDto> CreateSaleAsync(CreateSaleRequest request);
    Task<SalesReportData?> GetSalesReportAsync(int shopId, DateTime startDate, DateTime endDate);
    Task<ProfitReportData?> GetProfitReportAsync(int shopId, DateTime startDate, DateTime endDate);
    Task<SaleSummaryData?> GetSaleSummaryAsync(int shopId);
    Task<SalePaymentDto?> AddSalePaymentAsync(int saleId, string paymentMethod, decimal amount, string? transactionId, int userId);
    Task<bool> UpdateSalePaymentAsync(int paymentId, string paymentMethod, decimal amount, string? transactionId);
    Task<bool> DeleteSalePaymentAsync(int paymentId);
}
