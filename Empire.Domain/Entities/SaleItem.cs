using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class SaleItem : AuditableEntity
{


    [Required]
    public int SaleId { get; set; }

    public virtual Sale Sale { get; set; } = null!;

    // Nullable for custom items
    public int? InventoryItemId { get; set; }

    [Required]
    [MaxLength(255)]
    public string ItemName { get; set; } = string.Empty; // For custom items

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    // Cost price at time of sale (for profit calculation)
    [Column(TypeName = "decimal(18,2)")]
    public decimal CostPrice { get; set; } = 0;

    [MaxLength(1000)]
    public string? Note { get; set; }

    public bool IsCustomItem { get; set; } = false;
    public bool IsTaxable { get; set; } = true;

    // Optional link to a repair (when selling a completed repair)
    public int? RepairId { get; set; }
    public virtual Repair? Repair { get; set; }

    // Navigation properties
    public virtual InventoryItem? InventoryItem { get; set; }
}

