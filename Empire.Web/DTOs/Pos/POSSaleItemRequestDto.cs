namespace Empire.Web.DTOs.Pos
{
    public class POSSaleItemRequestDto
    {
        public string ItemType { get; set; } = string.Empty; // "Device", "Inventory", "Repair", "Custom"
        public int? ItemReferenceId { get; set; }
        public int? InventoryItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal? Cost { get; set; }           // matches SaleItemRequest.Cost for profit calculation
        public decimal DiscountAmount { get; set; }
        public bool IsCustomItem { get; set; }
        public bool IsTaxable { get; set; }
        public string? Note { get; set; }
        public int? RepairId { get; set; }
        public decimal? OriginalPrice { get; set; }
    }
}
