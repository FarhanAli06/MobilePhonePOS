namespace Empire.Application.DTOs.Sale;

public class POSSearchResult
{
    public string Id { get; set; } = string.Empty; // Format: "dev_123", "inv_456", "rep_789"
    public string Type { get; set; } = string.Empty; // "Device", "Inventory", "Repair"
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CostPrice { get; set; }
    public int Stock { get; set; }
    public string? IMEI { get; set; }
    public string? NetworkStatus { get; set; }
    public string? RepairNumber { get; set; }
    public string? CustomerName { get; set; }
    public bool IsAvailable { get; set; } = true;
}

