using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class Payment : AuditableEntity
{
    [Required]
    public int SaleId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Zelle, Venmo, etc.

    [MaxLength(255)]
    public string? TransactionId { get; set; }

    [Required]
    public int UserId { get; set; }

    // Navigation properties
    public virtual Sale Sale { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
