namespace Empire.Application.DTOs.Sale;

public class AddSalePaymentRequest
{
    public string PaymentMethod { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
}
