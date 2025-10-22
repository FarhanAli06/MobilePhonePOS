using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class LookupValue : BaseEntity
{
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
    
    // Optional relationship with Category entity
    public int? CategoryId { get; set; }
    public virtual Category? CategoryEntity { get; set; }
}

