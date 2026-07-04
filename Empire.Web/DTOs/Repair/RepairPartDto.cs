namespace Empire.Web.DTOs.Repair;

public class RepairPartDto
{
    public int Id { get; set; }
    public int RepairId { get; set; }
    public int InventoryItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
