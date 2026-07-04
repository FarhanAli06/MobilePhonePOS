using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class Repair : BaseEntity
{
    [Required]
    public int ShopId { get; set; }
    
    [Required]
    public int CustomerId { get; set; }
    
    // Brand information for repair
    public int? BrandId { get; set; }
    
    // Device categorization
    public int? DeviceCategoryId { get; set; }
    public int? DeviceModelId { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string RepairNumber { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(1000)]
    public string? Comments { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "InProgress"; // From LookupValue: Pending, InProgress, Completed, Cancelled
    
    [Required]
    [MaxLength(20)]
    public string PaymentStatus { get; set; } = "Unpaid"; // From LookupValue: Unpaid, Partial, Paid, Refunded
    
    [Required]
    public decimal Cost { get; set; }

    /// <summary>Total amount paid so far (used for partial payment tracking).</summary>
    public decimal AmountPaid { get; set; } = 0;
    
    public DateTime? CompletedDate { get; set; }
    
    // User tracking fields
    public int? CreatedBy { get; set; }
    public int? ModifiedBy { get; set; }
    
    // Navigation properties
    public virtual Shop Shop { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
    public virtual Brand? Brand { get; set; }
    public virtual DeviceCategory? DeviceCategory { get; set; }
    public virtual DeviceModel? DeviceModel { get; set; }
    
    public int? CompanyId { get; set; }
    public virtual Company? Company { get; set; }

    public virtual User? CreatedByUser { get; set; }
    public virtual User? ModifiedByUser { get; set; }
    
    // Repair parts relationship
    public virtual ICollection<RepairPart> RepairParts { get; set; } = new List<RepairPart>();

    // Repair payment ledger
    public virtual ICollection<RepairPayment> RepairPayments { get; set; } = new List<RepairPayment>();
}

