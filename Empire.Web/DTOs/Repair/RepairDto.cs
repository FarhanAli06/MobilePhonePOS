using System.Text.Json.Serialization;

namespace Empire.Web.DTOs.Repair;

public class RepairDto
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string DeviceBrand { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string? IMEI { get; set; }
    public string IssueDescription { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public decimal AdvancePayment { get; set; }
    public decimal RemainingDues { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public int? ShopId { get; set; }
    public string? ShopName { get; set; }
    public string? RepairNumber { get; set; }
    public int? BrandId { get; set; }
    public int? DeviceCategoryId { get; set; }
    public int? DeviceModelId { get; set; }
    public string? Description { get; set; }
    public string? Comments { get; set; }
    public decimal Cost { get; set; }
    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? DeviceCategory { get; set; }
    public string? CreatedByUser { get; set; }

    /// <summary>
    /// List of part names used in this repair.
    /// The API RepairDto returns a JSON array; this must match to avoid deserialization errors.
    /// </summary>
    public List<string> RepairPartNames { get; set; } = new List<string>();

    /// <summary>
    /// Convenience property: comma-joined part names for display in views/tables.
    /// Ignored during JSON serialization/deserialization.
    /// </summary>
    [JsonIgnore]
    public string RepairPartNamesDisplay =>
        RepairPartNames.Count > 0 ? string.Join(", ", RepairPartNames) : string.Empty;
}
