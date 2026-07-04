namespace Empire.Application.DTOs.Repair;

public class RepairPaymentDto
{
    public int Id { get; set; }
    public int RepairId { get; set; }
    public string Type { get; set; } = "Payment";       // "Payment" | "Refund"
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime PaidAt { get; set; }
    public string CreatedByUser { get; set; } = string.Empty;
}

public class AddRepairPaymentRequest
{
    public int RepairId { get; set; }
    public string Type { get; set; } = "Payment";
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
    public int? UserId { get; set; }
}

public class UpdateRepairPaymentRequest
{
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
}
