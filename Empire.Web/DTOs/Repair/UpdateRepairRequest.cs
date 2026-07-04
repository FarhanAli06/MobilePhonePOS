using System.ComponentModel.DataAnnotations;
namespace Empire.Web.DTOs.Repair;

public class UpdateRepairRequestDto
{
    public string? IssueDescription { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public decimal? AdvancePayment { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int? BrandId { get; set; }
    public int? DeviceCategoryId { get; set; }
    public int? DeviceModelId { get; set; }
    public string? Description { get; set; }
    public string? Comments { get; set; }
    public decimal? Cost { get; set; }
    public decimal? AmountPaid { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
}
