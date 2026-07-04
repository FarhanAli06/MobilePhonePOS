using Empire.Web.DTOs.SaleItem;

namespace Empire.Web.DTOs.Pos
{
    public class CreateSaleRequestDto
    {
        public int? CustomerId { get; set; } // Made nullable for custom sales
        public List<SaleItemRequestDto> Items { get; set; } = new();
        public List<PaymentRequestDto> Payments { get; set; } = new(); // Added Payments property
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? PaymentStatus { get; set; } // Added for partial payment support
        public string? Notes { get; set; }
    }
}
