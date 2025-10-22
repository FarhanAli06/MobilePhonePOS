using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class DeviceModel : BaseEntity
{
    [Required]
    public int BrandId { get; set; }
    
    [Required]
    public int DeviceCategoryId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ModelNumber { get; set; } = string.Empty;
    
    public int? Year { get; set; }
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public int DisplayOrder { get; set; } = 0;
    
    // Navigation properties
    public virtual Brand Brand { get; set; } = null!;
    public virtual DeviceCategory DeviceCategory { get; set; } = null!;
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}

