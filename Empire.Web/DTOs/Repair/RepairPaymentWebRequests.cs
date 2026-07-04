namespace Empire.Web.DTOs.Repair;

public class AddRepairPaymentWebRequest
{
    public int RepairId { get; set; }
    public string Type { get; set; } = "Payment";
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class UpdateRepairPaymentWebRequest
{
    public int PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class DeleteRepairPaymentWebRequest
{
    public int PaymentId { get; set; }
}
