namespace Empire.Application.DTOs.Repair;

public class WaitingPartAlertDto
{
    public int RepairPartId { get; set; }
    public int RepairId { get; set; }
    public string RepairNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string Device { get; set; } = string.Empty;
    public int InventoryItemId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int QuantityNeeded { get; set; }
    public int AvailableStock { get; set; }
    public DateTime RequestedDate { get; set; }
    public int DaysWaiting { get; set; }
}
