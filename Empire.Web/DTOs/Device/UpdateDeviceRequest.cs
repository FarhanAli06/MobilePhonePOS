using System.ComponentModel.DataAnnotations;

namespace Empire.Web.DTOs.Device;

public class UpdateDeviceRequest
{
    // Lookup IDs (from EmpireDeviceSelector / edit modal)
    public int? BrandId { get; set; }
    public int? DeviceCategoryId { get; set; }
    public int? DeviceModelId { get; set; }
    public int? CompanyId { get; set; }

    // String names (required by API UpdateDeviceRequest)
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public int? Year { get; set; }

    /// <summary>
    /// Integer representation of DeviceType enum: 1=Phone, 2=Laptop, 3=Part, 4=Accessories.
    /// JS sends an integer (e.g. 0 or 1); we map it to the enum in the controller before forwarding.
    /// </summary>
    public int DeviceType { get; set; } = 1;

    public string IMEISerialNumber { get; set; } = string.Empty;
    public string? GB { get; set; }
    public int? BatteryHealthPercentage { get; set; }
    public string NetworkStatus { get; set; } = "Unlocked";
    public string ScratchesCondition { get; set; } = "None";
    public decimal? BuyingPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsAvailableForSale { get; set; } = true;
    public bool IsSold { get; set; } = false;
    public int? SoldToCustomerId { get; set; }
    public string? Condition { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? PurchasePrice { get; set; }

    public string? Status { get; set; }
}
