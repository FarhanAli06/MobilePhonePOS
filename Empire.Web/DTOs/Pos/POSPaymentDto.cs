namespace Empire.Web.DTOs.Pos
{
    public class POSPaymentDto
    {
        public int Id { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? TransactionId { get; set; }
    }
}
