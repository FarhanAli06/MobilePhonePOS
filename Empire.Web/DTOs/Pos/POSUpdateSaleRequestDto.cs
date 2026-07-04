using Empire.Web.Services.API;

namespace Empire.Web.DTOs.Pos
{
    public class POSUpdateSaleRequestDto
    {
        public int? CustomerId { get; set; }
        public List<POSSaleItemRequestDto> Items { get; set; } = new();
        public List<POSPaymentRequestDto> Payments { get; set; } = new();
        public decimal? SubTotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
