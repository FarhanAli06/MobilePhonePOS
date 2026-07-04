namespace Empire.Web.DTOs.Pos
{
    public class POSSaleItemDto
    {
        public int Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal CostPrice { get; set; }
        public int? InventoryItemId { get; set; }
        public bool IsCustomItem { get; set; }
        public int? RepairId { get; set; }
    }
}
