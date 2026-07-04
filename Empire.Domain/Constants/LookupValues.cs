namespace Empire.Domain.Constants;

/// <summary>
/// Constants for lookup values - use these for string comparisons in code
/// </summary>
public static class LookupValues
{
    /// <summary>
    /// Repair Status Values
    /// </summary>
    public static class RepairStatus
    {
        public const string Pending = "Pending";
        public const string InProgress = "In Progress";
        public const string WaitingForParts = "Waiting for Parts";
        public const string ReadyForPickup = "Ready for Pickup";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string OnHold = "On Hold";
        public const string Unrepairable = "Unrepairable";
        public const string Abandoned = "Abandoned";
    }

    /// <summary>
    /// Payment Status Values
    /// </summary>
    public static class PaymentStatus
    {
        public const string Unpaid = "Unpaid";
        public const string PartiallyPaid = "Partially Paid";
        public const string Paid = "Paid";
        public const string Refunded = "Refunded";
        public const string Overdue = "Overdue";
    }
    
    /// <summary>
    /// Payment Method Values
    /// </summary>
    public static class PaymentMethod
    {
        public const string Cash = "Cash";
        public const string Card = "Card";
        public const string CreditCard = "Credit Card";
        public const string DebitCard = "Debit Card";
        public const string BankTransfer = "Bank Transfer";
        public const string Check = "Check";
        public const string MobilePayment = "Mobile Payment";
        public const string PayPal = "PayPal";
        public const string Venmo = "Venmo";
        public const string CashApp = "Cash App";
        public const string Zelle = "Zelle";
        public const string Other = "Other";
    }
    
    /// <summary>
    /// Device Status Values
    /// </summary>
    public static class DeviceStatus
    {
        public const string Available = "Available";
        public const string Sold = "Sold";
        public const string NotForSale = "Not For Sale";
        public const string ForParts = "For Parts";
        public const string Reserved = "Reserved";
        public const string InRepair = "In Repair";
        public const string Damaged = "Damaged";
        public const string Lost = "Lost";
        public const string Returned = "Returned";
    }

    /// <summary>
    /// Network Status Values
    /// </summary>
    public static class NetworkStatus
    {
        public const string Unlocked = "Unlocked";
        public const string Locked = "Locked";
        public const string Blacklisted = "Blacklisted";
        public const string Unknown = "Unknown";
        public const string FactoryUnlocked = "Factory Unlocked";
        public const string CarrierLocked = "Carrier Locked";
    }

    /// <summary>
    /// Device Condition Values
    /// </summary>
    public static class DeviceCondition
    {
        public const string New = "New";
        public const string LikeNew = "Like New";
        public const string Excellent = "Excellent";
        public const string Good = "Good";
        public const string Fair = "Fair";
        public const string Poor = "Poor";
        public const string ForParts = "For Parts";
        public const string Refurbished = "Refurbished";
        public const string OpenBox = "Open Box";
    }
    
    /// <summary>
    /// Scratches Condition Values
    /// </summary>
    public static class ScratchesCondition
    {
        public const string NoScratches = "No Scratches";
        public const string MinorScratches = "Minor Scratches";
        public const string ModerateScratches = "Moderate Scratches";
        public const string HeavyScratches = "Heavy Scratches";
        public const string DeepScratches = "Deep Scratches";
    }
    
    /// <summary>
    /// Damage Location Values
    /// </summary>
    public static class DamageLocation
    {
        public const string Screen = "Screen";
        public const string BackPanel = "Back Panel";
        public const string Frame = "Frame";
        public const string Edges = "Edges";
        public const string Corners = "Corners";
        public const string Camera = "Camera";
        public const string CameraLens = "Camera Lens";
        public const string Buttons = "Buttons";
        public const string PowerButton = "Power Button";
        public const string VolumeButtons = "Volume Buttons";
        public const string HomeButton = "Home Button";
        public const string Ports = "Ports";
        public const string ChargingPort = "Charging Port";
        public const string HeadphoneJack = "Headphone Jack";
        public const string SpeakerGrill = "Speaker Grill";
        public const string MicrophoneGrill = "Microphone Grill";
        public const string SIMTray = "SIM Tray";
        public const string Battery = "Battery";
        public const string Internal = "Internal";
        public const string Other = "Other";
    }

    /// <summary>
    /// Storage Capacity Values (GB)
    /// </summary>
    public static class StorageCapacity
    {
        public const string GB8 = "8GB";
        public const string GB16 = "16GB";
        public const string GB32 = "32GB";
        public const string GB64 = "64GB";
        public const string GB128 = "128GB";
        public const string GB256 = "256GB";
        public const string GB512 = "512GB";
        public const string GB1TB = "1TB";
        public const string GB2TB = "2TB";
        public const string GB4TB = "4TB";
    }

    /// <summary>
    /// RAM Capacity Values (GB)
    /// </summary>
    public static class RAMCapacity
    {
        public const string GB1 = "1GB";
        public const string GB2 = "2GB";
        public const string GB3 = "3GB";
        public const string GB4 = "4GB";
        public const string GB6 = "6GB";
        public const string GB8 = "8GB";
        public const string GB12 = "12GB";
        public const string GB16 = "16GB";
        public const string GB32 = "32GB";
        public const string GB64 = "64GB";
    }

    /// <summary>
    /// Device Color Values
    /// </summary>
    public static class DeviceColor
    {
        public const string Black = "Black";
        public const string White = "White";
        public const string Silver = "Silver";
        public const string Gold = "Gold";
        public const string RoseGold = "Rose Gold";
        public const string SpaceGray = "Space Gray";
        public const string MidnightGreen = "Midnight Green";
        public const string PacificBlue = "Pacific Blue";
        public const string Blue = "Blue";
        public const string Red = "Red";
        public const string ProductRed = "(PRODUCT)RED";
        public const string Green = "Green";
        public const string Purple = "Purple";
        public const string Yellow = "Yellow";
        public const string Pink = "Pink";
        public const string Coral = "Coral";
        public const string Graphite = "Graphite";
        public const string SierraBlu = "Sierra Blue";
        public const string AlpineGreen = "Alpine Green";
        public const string Starlight = "Starlight";
        public const string Midnight = "Midnight";
    }

    /// <summary>
    /// Device Source Values
    /// </summary>
    public static class DeviceSource
    {
        public const string Supplier = "Supplier";
        public const string TradeIn = "Trade-In";
        public const string Wholesale = "Wholesale";
        public const string Retail = "Retail";
        public const string Auction = "Auction";
        public const string Direct = "Direct";
        public const string Customer = "Customer";
        public const string Donation = "Donation";
        public const string Other = "Other";
    }

    /// <summary>
    /// Device Type Values
    /// </summary>
    public static class DeviceType
    {
        public const string Smartphone = "Smartphone";
        public const string Tablet = "Tablet";
        public const string Laptop = "Laptop";
        public const string Desktop = "Desktop";
        public const string Smartwatch = "Smartwatch";
        public const string Headphones = "Headphones";
        public const string Earbuds = "Earbuds";
        public const string Speaker = "Speaker";
        public const string GameConsole = "Game Console";
        public const string Camera = "Camera";
        public const string Accessory = "Accessory";
        public const string Other = "Other";
    }

    /// <summary>
    /// Inventory Unit Values
    /// </summary>
    public static class InventoryUnit
    {
        public const string Piece = "Piece";
        public const string Box = "Box";
        public const string Pack = "Pack";
        public const string Set = "Set";
        public const string Roll = "Roll";
        public const string Meter = "Meter";
        public const string Kilogram = "Kilogram";
        public const string Liter = "Liter";
        public const string Pair = "Pair";
        public const string Dozen = "Dozen";
    }

    /// <summary>
    /// Transaction Type Values
    /// </summary>
    public static class TransactionType
    {
        public const string Purchase = "Purchase";
        public const string Sale = "Sale";
        public const string Return = "Return";
        public const string Adjustment = "Adjustment";
        public const string Transfer = "Transfer";
        public const string Damaged = "Damaged";
        public const string Lost = "Lost";
        public const string Found = "Found";
        public const string Repaired = "Repaired";
    }
    
    /// <summary>
    /// Priority Values
    /// </summary>
    public static class Priority
    {
        public const string Low = "Low";
        public const string Normal = "Normal";
        public const string High = "High";
        public const string Urgent = "Urgent";
        public const string Critical = "Critical";
    }
    
    /// <summary>
    /// Customer Type Values
    /// </summary>
    public static class CustomerType
    {
        public const string Individual = "Individual";
        public const string Business = "Business";
        public const string Corporate = "Corporate";
        public const string Government = "Government";
        public const string Reseller = "Reseller";
        public const string VIP = "VIP";
    }
}
