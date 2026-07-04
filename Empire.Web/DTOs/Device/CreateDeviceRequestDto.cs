using System.ComponentModel.DataAnnotations;

namespace Empire.Web.DTOs.Device;

/// <summary>
/// Request DTO for creating a new device — matches Empire.Application.DTOs.Device.CreateDeviceRequest
/// </summary>
public class CreateDeviceRequestDto
{
    public int? ShopId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Brand is required")]
    public int BrandId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Device category is required")]
    public int DeviceCategoryId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Device model is required")]
    public int DeviceModelId { get; set; }

    [MaxLength(50)]
    public string IMEISerialNumber { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? GB { get; set; }

    [Range(0, 100)]
    public int? BatteryHealthPercentage { get; set; }

    [MaxLength(20)]
    public string NetworkStatus { get; set; } = "Unlocked";

    [MaxLength(20)]
    public string ScratchesCondition { get; set; } = "Excellent";

    [Range(0, double.MaxValue)]
    public decimal? BuyingPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SellingPrice { get; set; }

    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;

    public bool IsAvailableForSale { get; set; } = true;
}
