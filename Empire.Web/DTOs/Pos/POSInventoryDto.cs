namespace Empire.Web.DTOs.Pos
{
    public class POSInventoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public string? Brand { get; set; }
        public string? Category { get; set; }
        public int Stock { get; set; }
        public decimal CostPrice { get; set; }
        public decimal RetailPrice { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
