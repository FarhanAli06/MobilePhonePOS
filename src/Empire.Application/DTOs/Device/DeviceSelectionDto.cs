using Empire.Domain.Enums;
using Empire.Application.DTOs.Customer;

namespace Empire.Application.DTOs.Device;

public class DeviceSelectionDto
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public int BrandId { get; set; }
    public int DeviceCategoryId { get; set; }
    public int DeviceModelId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public int? Year { get; set; }
    public DeviceType DeviceType { get; set; }
    public string IMEISerialNumber { get; set; } = string.Empty;
    public int? BatteryHealthPercentage { get; set; }
    public string NetworkStatus { get; set; } = string.Empty;
    public string ScratchesCondition { get; set; } = string.Empty;
    public decimal? BuyingPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsAvailableForSale { get; set; }
    public bool IsSold { get; set; }
    public DateTime? SoldDate { get; set; }
    public int? SoldToCustomerId { get; set; }
    public string SoldToCustomerName { get; set; } = string.Empty;
    public CustomerDto? SoldToCustomer { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    public string DisplayName => $"{Brand} {Model} {(Year.HasValue ? $"({Year})" : "")}".Trim();
    public string StatusDisplay => IsSold ? "Sold" : (IsAvailableForSale ? "Available" : "Not for Sale");
    public string ConditionSummary => $"Battery: {BatteryHealthPercentage ?? 0}%, Scratches: {ScratchesCondition}, Network: {NetworkStatus}";
}

