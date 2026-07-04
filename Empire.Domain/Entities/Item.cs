using Empire.Domain.Common;

namespace Empire.Domain.Entities;

/// <summary>
/// Master list of item types (e.g., Screen, Battery, Charging Port, etc.)
/// Used as a lookup for inventory items. Each shop manages its own item list.
/// </summary>
public class Item : BaseEntity
{
    /// <summary>Shop that owns this item entry.</summary>
    public int ShopId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public virtual Shop Shop { get; set; } = null!;
    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
