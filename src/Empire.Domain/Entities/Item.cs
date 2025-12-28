using Empire.Domain.Common;

namespace Empire.Domain.Entities;

/// <summary>
/// Master list of item types (e.g., Screen, Battery, Charging Port, etc.)
/// Used as a lookup for inventory items
/// </summary>
public class Item : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation property
    public virtual ICollection<InventoryItem> InventoryItems { get; set; } = new List<InventoryItem>();
}
