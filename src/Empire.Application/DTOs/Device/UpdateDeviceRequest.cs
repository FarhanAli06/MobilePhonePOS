using System.ComponentModel.DataAnnotations;
using Empire.Domain.Enums;

namespace Empire.Application.DTOs.Device;

public class UpdateDeviceRequest
{
    [Required]
    [MaxLength(50)]
    public string Brand { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ModelNumber { get; set; } = string.Empty;
    
    public int? Year { get; set; }
    
    [Required]
    public DeviceType DeviceType { get; set; }
    
    [MaxLength(50)]
    public string IMEISerialNumber { get; set; } = string.Empty;
    
    [Range(0, 100)]
    public int? BatteryHealthPercentage { get; set; }
    
    [MaxLength(20)]
    public string NetworkStatus { get; set; } = "Unlocked";
    
    [MaxLength(20)]
    public string ScratchesCondition { get; set; } = "None";
    
    [Range(0, double.MaxValue)]
    public decimal? BuyingPrice { get; set; }
    
    [Range(0, double.MaxValue)]
    public decimal? SellingPrice { get; set; }
    
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
    
    public bool IsAvailableForSale { get; set; } = true;
    
    public bool IsSold { get; set; } = false;
    
    public int? SoldToCustomerId { get; set; }
}

