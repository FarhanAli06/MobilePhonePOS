using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

/// <summary>
/// Represents a single inventory transaction (stock movement)
/// Every stock IN or OUT is recorded as a transaction
/// Current stock = SUM(IN) - SUM(OUT)
/// </summary>
public class InventoryTransaction : BaseEntity
{
    /// <summary>
    /// Which inventory item this transaction is for
    /// </summary>
    [Required]
    public int InventoryItemId { get; set; }
    
    /// <summary>
    /// When this transaction occurred
    /// </summary>
    [Required]
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Type of transaction: IN, OUT, ADJUSTMENT, RETURN
    /// IN = Stock received (purchase, return from customer)
    /// OUT = Stock used (repair, sale, damaged)
    /// ADJUSTMENT = Manual correction
    /// RETURN = Customer return
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;
    
    /// <summary>
    /// Quantity moved (always positive, type determines IN or OUT)
    /// </summary>
    [Required]
    public int Quantity { get; set; }
    
    /// <summary>
    /// Description of why this transaction occurred
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of reference: REPAIR, SALE, PURCHASE, ADJUSTMENT
    /// </summary>
    [MaxLength(50)]
    public string? ReferenceType { get; set; }
    
    /// <summary>
    /// ID of the related record (RepairId, SaleId, etc.)
    /// </summary>
    public int? ReferenceId { get; set; }
    
    /// <summary>
    /// Human-readable reference number (R-2025-0001, S-2025-0050, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? ReferenceNumber { get; set; }
    
    /// <summary>
    /// Cost per unit (for IN transactions, used for COGS calculation)
    /// </summary>
    public decimal? UnitCost { get; set; }
    
    /// <summary>
    /// Total cost (Quantity × UnitCost)
    /// </summary>
    public decimal? TotalCost { get; set; }
    
    /// <summary>
    /// Which user performed this transaction
    /// </summary>
    [Required]
    public int CreatedByUserId { get; set; }
    
    /// <summary>
    /// Additional notes about this transaction
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual InventoryItem InventoryItem { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
}
