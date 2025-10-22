namespace Empire.Application.DTOs.Sale;

public class CreateSaleRequest
{
    public int CustomerId { get; set; }
    public List<SaleItemRequest> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Notes { get; set; }
}

public class SaleItemRequest
{
    public string ItemType { get; set; } = string.Empty; // "Device", "Inventory", "Repair"
    public int? ItemReferenceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
}

