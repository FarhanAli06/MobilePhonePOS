namespace Empire.Application.DTOs.Device;

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
}
