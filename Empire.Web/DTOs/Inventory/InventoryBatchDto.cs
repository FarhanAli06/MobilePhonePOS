namespace Empire.Web.DTOs.Inventory;

public class InventoryBatchDto
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public int ReceivedQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class ReceiveInventoryBatchRequest
{
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? ReceivedDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}
