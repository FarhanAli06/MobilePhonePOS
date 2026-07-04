using System.ComponentModel.DataAnnotations;
using Empire.Web.Enums;

namespace Empire.Web.DTOs.Repair;

public class CreateRepairRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public string DeviceBrand { get; set; } = string.Empty;

    [Required]
    public string DeviceModel { get; set; } = string.Empty;

    public string? IMEI { get; set; }

    [Required]
    public string IssueDescription { get; set; } = string.Empty;

    [Required]
    public decimal EstimatedCost { get; set; }

    public decimal AdvancePayment { get; set; }

    public string Status { get; set; } = "Pending";

    public int? ShopId { get; set; }
    public int? BrandId { get; set; }
    public int? DeviceCategoryId { get; set; }
    public int? DeviceModelId { get; set; }
    public string? Description { get; set; }
    public string? Comments { get; set; }
    public decimal Cost { get; set; }

    /// <summary>
    /// Typed as enum so BaseApiService serialises it as "unpaid"/"paid" (camelCase string)
    /// matching the JsonStringEnumConverter on the API side.
    /// </summary>
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    public decimal AmountPaid { get; set; } = 0m;

    public int? CompanyId { get; set; }
}
