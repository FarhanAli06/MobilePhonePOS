using Empire.Web.DTOs.Inventory;
using Empire.Web.DTOs.InventoryTransaction;

namespace Empire.Web.Services.InventoryTransaction;

public interface IInventoryTransactionApiService
{
    Task<List<InventoryTransactionDto>> GetTransactionsAsync(
        int shopId,
        string? transactionType = null,
        int? inventoryItemId = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<InventoryItemDto>> GetInventoryItemsByShopAsync(int shopId);
    
    Task<InventoryTransactionDto?> GetTransactionByIdAsync(int id, int shopId);
    
    Task<List<InventoryTransactionDto>> GetItemHistoryAsync(int inventoryItemId);
    
    Task<InventoryItemDto?> GetInventoryItemWithDetailsAsync(int id, int shopId);
    
    Task CreateStockInTransactionAsync(
        int inventoryItemId,
        int quantity,
        string reason,
        int userId,
        string referenceType,
        int? referenceId = null,
        string? referenceNumber = null,
        decimal? unitCost = null,
        string? notes = null);
    
    Task CreateStockOutTransactionAsync(
        int inventoryItemId,
        int quantity,
        string reason,
        int userId,
        string referenceType,
        int? referenceId = null,
        string? referenceNumber = null,
        string? notes = null);
}
