
namespace Empire.Web.DTOs.Pos
{ 
    public class UpdatePaymentRequestDto
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string TransactionId { get; set; }
        public string? ReferenceNumber { get; set; }
    }
}
