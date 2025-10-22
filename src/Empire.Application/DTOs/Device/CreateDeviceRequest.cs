using System.ComponentModel.DataAnnotations;

namespace Empire.Application.DTOs.Device;

public class CreateDeviceRequest
{
    [Required]
    public int ShopId { get; set; }
    
    [Required]
    public int BrandId { get; set; }
    
    [Required]
    public int DeviceCategoryId { get; set; }
    
    [Required]
    public int DeviceModelId { get; set; }
    
    [MaxLength(50)]
    public string IMEISerialNumber { get; set; } = string.Empty;
    
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

