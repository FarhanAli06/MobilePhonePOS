namespace Empire.Web.DTOs.Device;

public class DeviceDto
{
    public int Id { get; set; }
    public string IMEI { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? Condition { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? NetworkStatus { get; set; }
    public int? ShopId { get; set; }
    public string? ShopName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? IMEISerialNumber { get; set; }
    public bool IsAvailableForSale { get; set; }
    public bool IsSold { get; set; }
    public string? DisplayName { get; set; }
    public string? Category { get; set; }
    public string? ModelNumber { get; set; }
    public int? Year { get; set; }
    public string? DeviceType { get; set; }
    public int? BatteryHealthPercentage { get; set; }
    public string? ScratchesCondition { get; set; }
    public decimal? BuyingPrice { get; set; }
    public string? Source { get; set; }
    public string? Notes { get; set; }
    public int? SoldToCustomerId { get; set; }
    public DateTime? SoldDate { get; set; }
    // Lookup IDs — populated from the API's DeviceSelectionDto response
    public int BrandId { get; set; }
    public int DeviceCategoryId { get; set; }
    public int DeviceModelId { get; set; }
}
