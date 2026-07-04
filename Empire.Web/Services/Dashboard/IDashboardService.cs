using Empire.Web.Models;

namespace Empire.Web.Services.Dashboard;

public interface IDashboardService
{
    /// <summary>
    /// Gets complete dashboard data for a specific shop
    /// </summary>
    Task<DashboardViewModel> GetDashboardDataAsync(int shopId);
    
    /// <summary>
    /// Gets dashboard statistics (repairs, inventory, sales)
    /// </summary>
    Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(int shopId);
    
    /// <summary>
    /// Validates and ensures user has shop access, sets current shop if needed
    /// </summary>
    Task<ShopValidationResult> ValidateAndSetCurrentShopAsync(int userId, int currentShopId);
}

/// <summary>
/// DTO for dashboard statistics
/// </summary>
public class DashboardStatisticsDto
{
    public int TotalRepairs { get; set; }
    public int InProgressRepairs { get; set; }
    public int CompletedRepairs { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockItemsCount { get; set; }
    public decimal DailySales { get; set; }
    public decimal DailyProfit { get; set; }
}

/// <summary>
/// Result of shop validation
/// </summary>
public class ShopValidationResult
{
    public bool IsValid { get; set; }
    public bool NeedsShopCreation { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
