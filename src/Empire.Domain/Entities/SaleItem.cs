using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Empire.Domain.Entities;

public class SaleItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SaleId { get; set; }

    [ForeignKey(nameof(SaleId))]
    public Sale? Sale { get; set; }

    [Required]
    [MaxLength(50)]
    public string ItemType { get; set; } = string.Empty; // "Device", "Inventory", "Repair"

    public int? ItemReferenceId { get; set; } // ID of the Device, Inventory, or Repair

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int Quantity { get; set; } = 1;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OriginalPrice { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

