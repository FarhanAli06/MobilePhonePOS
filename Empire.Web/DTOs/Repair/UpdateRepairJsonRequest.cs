namespace Empire.Web.DTOs.Repair;

/// <summary>
/// Request DTO for the AJAX-based repair update endpoint (POST /Repairs/UpdateRepairJson).
/// Submitted as JSON from the Edit.cshtml JavaScript form.
/// </summary>
public class UpdateRepairJsonRequest
{
    public int RepairId { get; set; }
    public int BrandId { get; set; }
    public int DeviceCategoryId { get; set; }
    public int DeviceModelId { get; set; }
    public string? Description { get; set; }
    public string? Comments { get; set; }
    public decimal Cost { get; set; }
    public decimal AmountPaid { get; set; }
    public string? Status { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public List<RepairPartRequest> Parts { get; set; } = new();
    public List<PaymentLineRequest> Payments { get; set; } = new();
}

/// <summary>A single payment line added on the Edit page.</summary>
public class PaymentLineRequest
{
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reference { get; set; }
    public string? Date { get; set; }
}
