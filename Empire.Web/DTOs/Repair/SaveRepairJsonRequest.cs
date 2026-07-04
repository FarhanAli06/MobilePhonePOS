namespace Empire.Web.DTOs.Repair;

/// <summary>
/// Request DTO for the AJAX-based repair save endpoint (POST /Repairs/SaveRepairJson).
/// Submitted as JSON from the Create.cshtml JavaScript form.
/// </summary>
public class SaveRepairJsonRequest
{
    public int CustomerId { get; set; }
    public int BrandId { get; set; }
    public int DeviceCategoryId { get; set; }
    public int DeviceModelId { get; set; }
    public string? Description { get; set; }
    public string? Comments { get; set; }
    public decimal Cost { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public decimal AmountPaid { get; set; } = 0m;
    public int CompanyId { get; set; }
    public List<RepairPartRequest> Parts { get; set; } = new();
    /// <summary>Individual payment/refund entries to persist in RepairPayments table at create time.</summary>
    public List<InitialRepairPaymentRequest> Payments { get; set; } = new();
}

/// <summary>
/// A single part line in a repair save request.
/// </summary>
public class RepairPartRequest
{
    public int InventoryItemId { get; set; }
    public int Quantity { get; set; }
    public string? PartName { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// A single payment or refund entry submitted at repair create time.
/// </summary>
public class InitialRepairPaymentRequest
{
    public string Type { get; set; } = "Payment";         // "Payment" | "Refund"
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    /// <summary>ISO date string, e.g. "2025-06-07T00:00:00". Null = now.</summary>
    public string? PaidAt { get; set; }
}
