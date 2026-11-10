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
    Task<SaleDto> UpdateSaleAsync(int saleId, CreateSaleRequest request);
    Task<IEnumerable<SaleHistoryDto>> GetSaleHistoryAsync(int saleId, int shopId);
    Task<IEnumerable<SaleReturnHistoryDto>> GetSaleReturnsAsync(int saleId, int shopId);
    Task<SaleReturnResultDto> ProcessReturnAsync(int saleId, ProcessSaleReturnRequest request);
    Task<SalesReportData?> GetSalesReportAsync(int shopId, DateTime startDate, DateTime endDate);
    Task<ProfitReportData?> GetProfitReportAsync(int shopId, DateTime startDate, DateTime endDate);
    Task<SaleSummaryData?> GetSaleSummaryAsync(int shopId);
}
