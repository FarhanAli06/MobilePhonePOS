using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

/// <summary>
/// Represents a single payment or refund transaction against a repair.
/// Type = "Payment" for money received, "Refund" for money returned to customer.
/// The net balance is: Repair.Cost - SUM(Payments) + SUM(Refunds).
/// </summary>
public class RepairPayment : BaseEntity
{
    [Required]
    public int RepairId { get; set; }

    /// <summary>"Payment" or "Refund"</summary>
    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "Payment";

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Cash, Card, Zelle, Venmo, Check, Other</summary>
    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash";

    [MaxLength(255)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    public int? CreatedBy { get; set; }

    // Navigation
    public virtual Repair Repair { get; set; } = null!;
    public virtual User? CreatedByUser { get; set; }
}
