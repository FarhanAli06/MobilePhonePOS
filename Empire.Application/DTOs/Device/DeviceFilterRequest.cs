using Empire.Domain.Enums;

namespace Empire.Application.DTOs.Device;

public class DeviceFilterRequest
{
    public int? ShopId { get; set; }
    public int? BrandId { get; set; }
    public int? DeviceCategoryId { get; set; }
    public int? DeviceModelId { get; set; }
    public DeviceType? DeviceType { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Model { get; set; }
    public string? IMEISerialNumber { get; set; }
    public bool? IsAvailableForSale { get; set; }
    public bool? IsSold { get; set; }
    public string? NetworkStatus { get; set; }
    public string? ScratchesCondition { get; set; }
    public decimal? MinBuyingPrice { get; set; }
    public decimal? MaxBuyingPrice { get; set; }
    public decimal? MinSellingPrice { get; set; }
    public decimal? MaxSellingPrice { get; set; }
    public DateTime? CreatedFromDate { get; set; }
    public DateTime? CreatedToDate { get; set; }
    public DateTime? SoldFromDate { get; set; }
    public DateTime? SoldToDate { get; set; }
    public string? SearchTerm { get; set; }
}

