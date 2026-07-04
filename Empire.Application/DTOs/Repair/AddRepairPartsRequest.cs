using System.ComponentModel.DataAnnotations;

namespace Empire.Application.DTOs.Repair;

/// <summary>
/// Request DTO for adding parts to an existing repair.
/// Used by POST /api/repairs/{id}/parts
/// </summary>
public class AddRepairPartsRequest
{
    /// <summary>
    /// List of inventory item IDs to add (legacy flat list — quantity defaults to 1 each).
    /// </summary>
    public List<int> InventoryPartIds { get; set; } = new();

    /// <summary>
    /// Optional: parts with explicit quantity and unit price per item.
    /// When provided, takes precedence over InventoryPartIds.
    /// </summary>
    public List<RepairPartLineRequest>? Parts { get; set; }

    public int ShopId { get; set; }
    public int UserId { get; set; }
}

/// <summary>
/// A single part line with quantity and price.
/// </summary>
public class RepairPartLineRequest
{
    [Required]
    public int InventoryItemId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public string? Notes { get; set; }
}
