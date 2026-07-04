namespace Empire.Web.DTOs.Sale;

public class AddSalePaymentWebRequest
{
    public int SaleId { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
}

public class UpdateSalePaymentWebRequest
{
    public int PaymentId { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
}
