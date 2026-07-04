using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class Brand : BaseEntity
{
    /// <summary>Shop that owns this brand entry. Each shop manages its own brand list.</summary>
    public int ShopId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public int DisplayOrder { get; set; } = 0;
    
    // Foreign keys
    public int? StylingId { get; set; }
    
    // Navigation properties
    public virtual Shop Shop { get; set; } = null!;
    public virtual Styling? Styling { get; set; }
    public virtual ICollection<DeviceModel> DeviceModels { get; set; } = new List<DeviceModel>();
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}

