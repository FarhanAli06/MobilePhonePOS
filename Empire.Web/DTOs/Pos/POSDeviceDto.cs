namespace Empire.Web.DTOs.Pos
{
    public class POSDeviceDto
    {
        public int Id { get; set; }
        // Matches the camelCase JSON returned by API POSController.MapDevices
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string? Imei { get; set; }
        public string? Network { get; set; }
        public string? Storage { get; set; }
        public string? Condition { get; set; }
        public string? Scratches { get; set; }
        public int? BatteryHealth { get; set; }
        public int Stock { get; set; }
        public decimal BuyingPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public bool IsAvailable { get; set; }
        // Backward-compat aliases
        public string Brand => BrandName;
        public string Category => CategoryName;
        public string Model => ModelName;
        public decimal? PurchasePrice => BuyingPrice;
        public decimal? SalePrice => SellingPrice;
        public string? SerialNumber => Imei;
        public string Status => IsAvailable ? "available" : "sold";
    }
}
