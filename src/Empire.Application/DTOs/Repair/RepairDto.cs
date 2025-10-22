namespace Empire.Application.DTOs.Repair;

public class RepairDto
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public int? BrandId { get; set; }
    public string DeviceBrand { get; set; } = string.Empty;
    public int? DeviceCategoryId { get; set; }
    public string DeviceCategory { get; set; } = string.Empty;
    public int? DeviceModelId { get; set; }
    public string DeviceModel { get; set; } = string.Empty;
    public string RepairNumber { get; set; } = string.Empty;
    public string Issue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string CreatedByUser { get; set; } = string.Empty;
    public string? ModifiedByUser { get; set; }
}

