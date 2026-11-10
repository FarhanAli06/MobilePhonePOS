namespace Empire.Web.DTOs.Repair;

public sealed class UpdateRepairStatusRequest
{
    public int RepairId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class UpdateRepairPaymentStatusRequest
{
    public int RepairId { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
}
