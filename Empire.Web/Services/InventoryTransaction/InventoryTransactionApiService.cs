
using Empire.Web.DTOs.Inventory;
using Empire.Web.DTOs.InventoryTransaction;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.InventoryTransaction;

public class InventoryTransactionApiService : IInventoryTransactionApiService
{
    private readonly IHttpClientService _httpClient;
    private readonly ILogger<InventoryTransactionApiService> _logger;
    private const string BaseEndpoint = "/api/inventory-transactions";

    public InventoryTransactionApiService(
        IHttpClientService httpClient,
        ILogger<InventoryTransactionApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<InventoryTransactionDto>> GetTransactionsAsync(
        int shopId,
        string? transactionType = null,
        int? inventoryItemId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var queryParams = new List<string> { $"shopId={shopId}" };
            
            if (!string.IsNullOrEmpty(transactionType))
                queryParams.Add($"transactionType={transactionType}");
            if (inventoryItemId.HasValue && inventoryItemId.Value > 0)
                queryParams.Add($"inventoryItemId={inventoryItemId.Value}");
            if (startDate.HasValue)
                queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            if (endDate.HasValue)
                queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");

            var endpoint = $"{BaseEndpoint}?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync<List<InventoryTransactionDto>>(endpoint);
            
            return response ?? new List<InventoryTransactionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory transactions for shop {ShopId}", shopId);
            return new List<InventoryTransactionDto>();
        }
    }

    public async Task<List<InventoryItemDto>> GetInventoryItemsByShopAsync(int shopId)
    {
        try
        {
            var response = await _httpClient.GetAsync<List<InventoryItemDto>>($"/api/inventory?shopId={shopId}&isActive=true");
            return response ?? new List<InventoryItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory items for shop {ShopId}", shopId);
            return new List<InventoryItemDto>();
        }
    }

    public async Task<InventoryTransactionDto?> GetTransactionByIdAsync(int id, int shopId)
    {
        try
        {
            var response = await _httpClient.GetAsync<InventoryTransactionDto>(
                $"{BaseEndpoint}/{id}?shopId={shopId}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction {Id}", id);
            return null;
        }
    }

    public async Task<List<InventoryTransactionDto>> GetItemHistoryAsync(int inventoryItemId)
    {
        try
        {
            var response = await _httpClient.GetAsync<List<InventoryTransactionDto>>(
                $"{BaseEndpoint}/item-history/{inventoryItemId}");
            return response ?? new List<InventoryTransactionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting item history for {ItemId}", inventoryItemId);
            return new List<InventoryTransactionDto>();
        }
    }

    public async Task<InventoryItemDto?> GetInventoryItemWithDetailsAsync(int id, int shopId)
    {
        try
        {
            var response = await _httpClient.GetAsync<InventoryItemDto>($"/api/inventory/{id}?shopId={shopId}");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory item {Id}", id);
            return null;
        }
    }

    public async Task CreateStockInTransactionAsync(
        int inventoryItemId,
        int quantity,
        string reason,
        int userId,
        string referenceType,
        int? referenceId = null,
        string? referenceNumber = null,
        decimal? unitCost = null,
        string? notes = null)
    {
        try
        {
            var request = new CreateTransactionRequest
            {
                InventoryItemId = inventoryItemId,
                TransactionType = "IN",
                Quantity = quantity,
                Reason = reason,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                ReferenceNumber = referenceNumber,
                UnitCost = unitCost,
                UserId = userId,
                Notes = notes
            };

            await _httpClient.PostAsync<CreateTransactionRequest, object>($"{BaseEndpoint}/stock-in", request);
            
            _logger.LogInformation(
                "Stock IN transaction created: Item={ItemId}, Qty={Qty}, User={UserId}",
                inventoryItemId, quantity, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock IN transaction");
            throw;
        }
    }

    public async Task CreateStockOutTransactionAsync(
        int inventoryItemId,
        int quantity,
        string reason,
        int userId,
        string referenceType,
        int? referenceId = null,
        string? referenceNumber = null,
        string? notes = null)
    {
        try
        {
            var request = new CreateTransactionRequest
            {
                InventoryItemId = inventoryItemId,
                TransactionType = "OUT",
                Quantity = quantity,
                Reason = reason,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                ReferenceNumber = referenceNumber,
                UserId = userId,
                Notes = notes
            };

            await _httpClient.PostAsync<CreateTransactionRequest, object>($"{BaseEndpoint}/stock-out", request);
            
            _logger.LogInformation(
                "Stock OUT transaction created: Item={ItemId}, Qty={Qty}, User={UserId}",
                inventoryItemId, quantity, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating stock OUT transaction");
            throw;
        }
    }
}

// Request DTO
public class CreateTransactionRequest
{
    public int InventoryItemId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string ReferenceType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public decimal? UnitCost { get; set; }
    public int UserId { get; set; }
    public string? Notes { get; set; }
}
