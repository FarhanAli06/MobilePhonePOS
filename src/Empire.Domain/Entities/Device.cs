using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class Device : BaseEntity
{
    [Required]
    public int ShopId { get; set; }
    
    [Required]
    public int BrandId { get; set; }
    
    [Required]
    public int DeviceCategoryId { get; set; }
    
    [Required]
    public int DeviceModelId { get; set; }
    
    // Physical device tracking and sales
    [MaxLength(50)]
    public string IMEISerialNumber { get; set; } = string.Empty;
    
    public int? BatteryHealthPercentage { get; set; }
    
    [MaxLength(20)]
    public string NetworkStatus { get; set; } = "Unlocked"; // From LookupValue
    
    [MaxLength(20)]
    public string ScratchesCondition { get; set; } = "Excellent"; // From LookupValue
    
    public decimal? BuyingPrice { get; set; }
    
    public decimal? SellingPrice { get; set; }
    
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty; // Where did you buy this device
    
    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
    
    public bool IsAvailableForSale { get; set; } = true;
    
    public bool IsSold { get; set; } = false;
    
    public DateTime? SoldDate { get; set; }
    
    public int? SoldToCustomerId { get; set; }
    
    // Navigation properties
    public virtual Shop Shop { get; set; } = null!;
    public virtual Brand Brand { get; set; } = null!;
    public virtual DeviceCategory DeviceCategory { get; set; } = null!;
    public virtual DeviceModel DeviceModel { get; set; } = null!;
    public virtual Customer? SoldToCustomer { get; set; }
    public virtual ICollection<Repair> Repairs { get; set; } = new List<Repair>();
}

