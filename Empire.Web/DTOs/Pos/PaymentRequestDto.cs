namespace Empire.Web.DTOs.Pos
{
    public class PaymentRequestDto
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
