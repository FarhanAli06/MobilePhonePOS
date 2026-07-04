namespace Empire.Domain.Constants;

/// <summary>
/// Constants for lookup value categories
/// </summary>
public static class LookupCategories
{
    // Repair-related lookups
    public const string RepairStatus = "RepairStatus";
    public const string PaymentStatus = "PaymentStatus";
    public const string PaymentMethod = "PaymentMethod";
    
    // Device-related lookups
    public const string DeviceStatus = "DeviceStatus";
    public const string NetworkStatus = "NetworkStatus";
    public const string DeviceCondition = "DeviceCondition";
    public const string ScratchesCondition = "ScratchesCondition";
    public const string DamageLocation = "DamageLocation";
    public const string DeviceType = "DeviceType";
    public const string DeviceSource = "DeviceSource";
    public const string DeviceColor = "DeviceColor";
    
    // Storage and capacity lookups
    public const string StorageCapacity = "StorageCapacity";
    public const string RAMCapacity = "RAMCapacity";
    
    // Inventory-related lookups
    public const string InventoryUnit = "InventoryUnit";
    public const string TransactionType = "TransactionType";
    
    // General lookups
    public const string Priority = "Priority";
    public const string CustomerType = "CustomerType";
}
