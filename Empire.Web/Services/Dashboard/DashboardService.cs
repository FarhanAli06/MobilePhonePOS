using Empire.Web.Models;
using Empire.Web.Services.Repair;
using Empire.Web.Services.Inventory;
using Empire.Web.Services.Home;
using Empire.Web.Services.Shop;
using Empire.Web.Services.Helper;
using Microsoft.Extensions.Logging;

namespace Empire.Web.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IRepairApiService _repairService;
    private readonly IInventoryApiService _inventoryService;
    private readonly IHomeService _homeService;
    private readonly IShopApiService _shopService;
    private readonly IHelperService _helperService;
    private readonly ILogger<DashboardService> _logger;

    // Constants for repair status matching
    private static readonly string[] InProgressStatuses = { "InProgress", "In Progress", "Pending" };
    private static readonly string[] CompletedStatuses = { "Completed", "Complete", "Done" };

    public DashboardService(
        IRepairApiService repairService,
        IInventoryApiService inventoryService,
        IHomeService homeService,
        IShopApiService shopService,
        IHelperService helperService,
        ILogger<DashboardService> logger)
    {
        _repairService = repairService;
        _inventoryService = inventoryService;
        _homeService = homeService;
        _shopService = shopService;
        _helperService = helperService;
        _logger = logger;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(int shopId)
    {
        try
        {
            _logger.LogInformation("Loading dashboard data for shop {ShopId}", shopId);

            // Load all data in parallel for better performance
            var repairsTask = _repairService.GetRepairsAsync(shopId);
            var inventoryTask = _inventoryService.GetInventoryAsync(shopId);
            var lowStockTask = _inventoryService.GetLowStockItemsAsync(shopId);
            var statisticsTask = GetDashboardStatisticsAsync(shopId);

            await Task.WhenAll(repairsTask, inventoryTask, lowStockTask, statisticsTask);

            var repairs = await repairsTask;
            var inventory = await inventoryTask;
            var lowStockItems = await lowStockTask;
            var statistics = await statisticsTask;

            // Debug logging
            _logger.LogInformation("Dashboard - Total Repairs: {Count}", repairs.Count());
            _logger.LogInformation("Dashboard - Repair Statuses: {Statuses}", 
                string.Join(", ", repairs.Select(r => r.Status).Distinct()));
            _logger.LogInformation("Dashboard - Low Stock Items Count: {Count}", lowStockItems.Count());
            _logger.LogInformation("Dashboard - Low Stock Items: {Items}", 
                string.Join(", ", lowStockItems.Select(i => $"{i.Name} (Stock: {i.CurrentStock}, Reorder: {i.ReorderPoint})")));

            var model = new DashboardViewModel
            {
                TotalRepairs = statistics.TotalRepairs,
                InProgressRepairs = statistics.InProgressRepairs,
                CompletedRepairs = statistics.CompletedRepairs,
                TotalInventoryValue = statistics.TotalInventoryValue,
                LowStockItemsCount = statistics.LowStockItemsCount,
                DailySales = statistics.DailySales,
                DailyProfit = statistics.DailyProfit,
                RecentRepairs = repairs.OrderByDescending(r => r.CreatedDate).Take(5).ToList(),
                LowStockItems = lowStockItems.Take(5).ToList()
            };

            _logger.LogInformation("Dashboard data loaded successfully for shop {ShopId}", shopId);
            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard data for shop {ShopId}", shopId);
            throw;
        }
    }

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(int shopId)
    {
        try
        {
            _logger.LogInformation("Calculating dashboard statistics for shop {ShopId}", shopId);

            // Load required data
            var repairsTask = _repairService.GetRepairsAsync(shopId);
            var inventoryTask = _inventoryService.GetInventoryAsync(shopId);
            var lowStockTask = _inventoryService.GetLowStockItemsAsync(shopId);
            
            var today = DateTime.UtcNow.Date;
            var dailySalesTask = _homeService.GetDailySalesAsync(shopId, today);
            var dailyProfitTask = _homeService.GetDailyProfitAsync(shopId, today);

            await Task.WhenAll(repairsTask, inventoryTask, lowStockTask, dailySalesTask, dailyProfitTask);

            var repairs = await repairsTask;
            var inventory = await inventoryTask;
            var lowStockItems = await lowStockTask;

            var statistics = new DashboardStatisticsDto
            {
                TotalRepairs = repairs.Count(),
                InProgressRepairs = _helperService.CountByStatus(repairs, InProgressStatuses, r => r.Status),
                CompletedRepairs = _helperService.CountByStatus(repairs, CompletedStatuses, r => r.Status),
                TotalInventoryValue = _helperService.CalculateInventoryValue(inventory, i => i.CurrentStock, i => i.RetailPrice),
                LowStockItemsCount = lowStockItems.Count(),
                DailySales = await dailySalesTask,
                DailyProfit = await dailyProfitTask
            };

            _logger.LogInformation("Dashboard statistics calculated: Repairs={TotalRepairs}, InProgress={InProgress}, Completed={Completed}, InventoryValue={Value}, LowStock={LowStock}",
                statistics.TotalRepairs, statistics.InProgressRepairs, statistics.CompletedRepairs, 
                statistics.TotalInventoryValue, statistics.LowStockItemsCount);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating dashboard statistics for shop {ShopId}", shopId);
            throw;
        }
    }

    public async Task<ShopValidationResult> ValidateAndSetCurrentShopAsync(int userId, int currentShopId)
    {
        try
        {
            _logger.LogInformation("Validating shop access for user {UserId}, current shop {ShopId}", userId, currentShopId);

            // Get user's shops
            var userShops = await _shopService.GetShopsByUserAsync(userId);
            
            if (!userShops.Any())
            {
                _logger.LogWarning("User {UserId} has no shops", userId);
                return new ShopValidationResult
                {
                    IsValid = false,
                    NeedsShopCreation = true,
                    ErrorMessage = "Welcome! Please create your first shop to get started."
                };
            }

            // If no current shop is set, use the first shop
            if (currentShopId == 0)
            {
                var firstShop = userShops.First();
                _logger.LogInformation("Setting first shop {ShopId} as current for user {UserId}", firstShop.Id, userId);
                
                return new ShopValidationResult
                {
                    IsValid = true,
                    NeedsShopCreation = false,
                    ShopId = firstShop.Id,
                    ShopName = firstShop.Name
                };
            }

            // Validate that the current shop belongs to the user
            var currentShop = userShops.FirstOrDefault(s => s.Id == currentShopId);
            if (currentShop == null)
            {
                _logger.LogWarning("User {UserId} does not have access to shop {ShopId}", userId, currentShopId);
                var firstShop = userShops.First();
                
                return new ShopValidationResult
                {
                    IsValid = true,
                    NeedsShopCreation = false,
                    ShopId = firstShop.Id,
                    ShopName = firstShop.Name
                };
            }

            return new ShopValidationResult
            {
                IsValid = true,
                NeedsShopCreation = false,
                ShopId = currentShop.Id,
                ShopName = currentShop.Name
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating shop access for user {UserId}", userId);
            throw;
        }
    }

    // Helper methods moved to centralized HelperService
}
