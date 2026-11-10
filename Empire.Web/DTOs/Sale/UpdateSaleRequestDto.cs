using Empire.Web.DTOs.Pos;
using Empire.Web.DTOs.SaleItem;

namespace Empire.Web.DTOs.Sale
{
    public class UpdateSaleRequestDto
    {
        public int SaleId { get; set; }
        public int? CustomerId { get; set; }
        public List<UpdateSaleItemRequestDto> Items { get; set; } = new();
        public decimal? SubTotal { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<UpdatePaymentRequestDto> Payments { get; set; } = new();
        public string? ChangeReason { get; set; }
    }
}
