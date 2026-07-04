using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.Inventory;
using Empire.Web.DTOs.Pos;
using Empire.Web.DTOs.Repair;

namespace Empire.Web.Services.POS
{
    public interface IPOSApiService
    {
        // Product search
        Task<List<POSProductDto>?> SearchProductsAsync(POSProductSearchRequestDto request);

        // Customer search
        Task<List<POSCustomerDto>?> SearchCustomersAsync(int shopId, string searchTerm);

        // Repair search
        Task<List<POSRepairDto>?> SearchRepairsAsync(int shopId, string searchTerm);

        // Sale operations
        Task<POSSaleResponseDto?> CreateSaleAsync(POSCreateSaleRequestDto request);
        Task<POSSaleResponseDto?> UpdateSaleAsync(int saleId, POSUpdateSaleRequestDto request);
        Task<POSSaleDetailDto?> GetSaleAsync(int saleId);
        Task<POSSaleDetailDto?> GetLastSaleAsync(int shopId);
        Task<List<POSSaleDto>?> GetSalesAsync(POSSalesSearchRequestDto request);
        Task<string?> GenerateInvoiceNumberAsync(int shopId);

        // Inventory
        Task<List<POSInventoryDto>?> GetInventoryAsync(POSInventorySearchRequestDto request);

        // Devices
        Task<List<POSDeviceDto>?> GetDevicesAsync(POSDeviceSearchRequestDto request);

        // Payment
        Task<bool> ProcessPaymentAsync(int saleId, PaymentRequestDto request);

        // Legacy search helpers
        Task<List<DeviceDto>> SearchDevicesAsync(string? brand = null, string? category = null, string? model = null);
        Task<List<InventoryItemDto>> SearchInventoryAsync(string? brand = null, string? category = null, string? model = null);
    }
}
