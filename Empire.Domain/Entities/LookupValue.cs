using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class LookupValue : BaseEntity
{
    /// <summary>Shop that owns this lookup value. 0 means global/system lookup.</summary>
    public int ShopId { get; set; } = 0;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty; // NetworkStatus, ScratchesCondition, RepairStatus, PaymentStatus
    
    [Required]
    [MaxLength(50)]
    public string Value { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    
    public int DisplayOrder { get; set; } = 0;
    
    [MaxLength(20)]
    public string ColorCode { get; set; } = string.Empty; // For status badges
    
    // Foreign keys
    public int? StylingId { get; set; }
    
    // Navigation properties
    public virtual Styling? Styling { get; set; }
    
    // Optional relationship with Category entity
    public int? CategoryId { get; set; }
    public virtual Category? CategoryEntity { get; set; }
}
