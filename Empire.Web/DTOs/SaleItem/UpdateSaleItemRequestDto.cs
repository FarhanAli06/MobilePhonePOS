namespace Empire.Web.DTOs.SaleItem
{
    public class UpdateSaleItemRequestDto
    {
        public string ItemName { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? CostPrice { get; set; }
        public int? InventoryItemId { get; set; }
        public bool IsCustomItem { get; set; }
        public string? ItemType { get; set; }
        public int? ItemReferenceId { get; set; }
        public decimal? TotalPrice { get; set; }
    }
}
