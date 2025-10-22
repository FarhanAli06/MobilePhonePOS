using Empire.Application.DTOs.Repair;
using Empire.Application.DTOs.Inventory;

namespace Empire.Web.Models;

public class DashboardViewModel
{
    public string CurrentShopName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    
    // Statistics
    public int TotalRepairs { get; set; }
    public int InProgressRepairs { get; set; }
    public int CompletedRepairs { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockItemsCount { get; set; }
    
    // Recent data
    public IEnumerable<RepairDto> RecentRepairs { get; set; } = new List<RepairDto>();
    public IEnumerable<InventoryDto> LowStockItems { get; set; } = new List<InventoryDto>();
}

