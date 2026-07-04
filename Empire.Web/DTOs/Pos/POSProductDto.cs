namespace Empire.Web.DTOs.Pos
{
    public class POSProductDto
    {
        public string Id { get; set; } = string.Empty; // Format: "inv_123", "dev_456", "rep_789"
        public string Type { get; set; } = string.Empty; // "inventory", "device", "repair"
        public string Name { get; set; } = string.Empty;
        public string? Brand { get; set; }
        public string? Category { get; set; }
        public string? Sku { get; set; }
        public decimal Price { get; set; }
        public decimal WholesalePrice { get; set; }
        public int Stock { get; set; }
        public string? Description { get; set; }
    }
}
