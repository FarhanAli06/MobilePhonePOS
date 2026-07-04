namespace Empire.Web.DTOs.SaleItem
{
    public class SaleItemDto
    {
        public int SaleId { get; set; }
        public string ItemType { get; set; } = string.Empty; // "Device", "Inventory", "Repair", "Custom"
        public int? ItemReferenceId { get; set; } // Id of Device, Inventory, or Repair
        public int? InventoryItemId { get; set; } // Added for Inventory
        public string ItemName { get; set; } = string.Empty; // Added for Custom/Repair/Device
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; } // Added TotalPrice
        public decimal? Cost { get; set; } // Cost per unit (for custom items to calculate profit)
        public decimal DiscountAmount { get; set; }
        public bool IsCustomItem { get; set; } // Added IsCustomItem
        public bool IsTaxable { get; set; } // Added IsTaxable
        public string? Note { get; set; } // Added Note
        public int? RepairId { get; set; } // Added RepairId for tracking repair items

        public decimal CostPrice { get; set; }
    }
}
