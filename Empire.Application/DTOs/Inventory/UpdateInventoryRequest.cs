using System.ComponentModel.DataAnnotations;
using Empire.Domain.Enums;

namespace Empire.Application.DTOs.Inventory;

public class UpdateInventoryRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }
    
    public DeviceType? DeviceType { get; set; }
    
    [MaxLength(50)]
    public string? Category { get; set; }
    
    public int? Stock { get; set; }
    
    public int? ReorderPoint { get; set; }
    
    public decimal? CostPrice { get; set; }
    
    public decimal? RetailPrice { get; set; }
    
    public bool? LowStockNotification { get; set; }
}

