namespace Empire.Application.DTOs.Sale;

public class CreateSaleRequest
{
    public int? CustomerId { get; set; } // Made nullable for custom sales
    public List<SaleItemRequest> Items { get; set; } = new();
    public List<PaymentRequest> Payments { get; set; } = new(); // Added Payments property
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentStatus { get; set; } // Added for partial payment support
    public string? Notes { get; set; }
}

public class SaleItemRequest
{
    public string ItemType { get; set; } = string.Empty; // "Device", "Inventory", "Repair", "Custom"
    public int? ItemReferenceId { get; set; } // Id of Device, Inventory, or Repair
    public int? InventoryItemId { get; set; } // Added for Inventory
    public string ItemName { get; set; } = string.Empty; // Added for Custom/Repair/Device
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; } // Added TotalPrice
    public decimal DiscountAmount { get; set; }
    public bool IsCustomItem { get; set; } // Added IsCustomItem
    public bool IsTaxable { get; set; } // Added IsTaxable
    public string? Note { get; set; } // Added Note
    public int? RepairId { get; set; } // Added RepairId for tracking repair items
}

public class PaymentRequest
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
}
