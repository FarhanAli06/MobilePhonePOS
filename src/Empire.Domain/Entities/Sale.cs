using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Empire.Domain.Common;
using Empire.Domain.Enums;

namespace Empire.Domain.Entities;

public class Sale : AuditableEntity
{


    [Required]
    public int ShopId { get; set; }

    [ForeignKey(nameof(ShopId))]
    public Shop? Shop { get; set; }

    public int? CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    [Required]
    [MaxLength(50)]
    public string SaleNumber { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash"; // Will be deprecated by Payments collection

    [MaxLength(50)]
    public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Partial, Paid, Refunded

    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int CreatedByUserId { get; set; }

    [ForeignKey(nameof(CreatedByUserId))]
    public User? CreatedByUser { get; set; }




    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

