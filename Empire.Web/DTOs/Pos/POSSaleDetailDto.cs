using Empire.Web.Services.API;

namespace Empire.Web.DTOs.Pos
{
    public class POSSaleDetailDto
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string? CustomerName { get; set; }
        public string? Notes { get; set; }
        public List<POSSaleItemDto> Items { get; set; } = new();
        public List<POSPaymentDto> Payments { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
