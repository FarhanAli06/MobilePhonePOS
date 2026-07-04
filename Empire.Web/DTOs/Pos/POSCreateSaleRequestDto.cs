namespace Empire.Web.DTOs.Pos
{
    public class POSCreateSaleRequestDto
    {
        public int ShopId { get; set; }
        public int? CustomerId { get; set; }
        public List<POSSaleItemRequestDto> Items { get; set; } = new();
        public List<POSPaymentRequestDto> Payments { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public int CreatedByUserId { get; set; }
    }
}
