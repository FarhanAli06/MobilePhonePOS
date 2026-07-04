namespace Empire.Web.DTOs.Pos
{
    public class POSPaymentRequestDto
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
