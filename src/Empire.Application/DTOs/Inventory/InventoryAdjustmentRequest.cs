using System.ComponentModel.DataAnnotations;

namespace Empire.Application.DTOs.Inventory;

public class InventoryAdjustmentRequest
{
    [Required]
    public int InventoryId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AdjustmentType { get; set; } = string.Empty; // Received, Sold, Damaged, Lost, etc.
    
    [Required]
    public int Quantity { get; set; } // Can be positive or negative
    
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;
}

