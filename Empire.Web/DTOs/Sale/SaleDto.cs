using Empire.Web.DTOs.Customer;
using Empire.Web.DTOs.Payment;
using Empire.Web.DTOs.SaleItem;
using Empire.Web.DTOs.Shop;
using Empire.Web.DTOs.User;
namespace Empire.Web.DTOs.Sale
{
    public class SaleDto
    {
        public int ShopId { get; set; }

        
        public ShopDto? Shop { get; set; }

        public int? CustomerId { get; set; }

       
        public CustomerDto? Customer { get; set; }

       public string SaleNumber { get; set; } = string.Empty;

       public decimal SubTotal { get; set; }

      
        public decimal TaxAmount { get; set; }

      
        public decimal DiscountAmount { get; set; }

       public decimal TotalAmount { get; set; }

      
        public decimal AdvancePayment { get; set; } = 0;

      
        public decimal RemainingDues { get; set; } = 0;

      
        public string PaymentMethod { get; set; } = "Cash"; // Will be deprecated by Payments collection

      
        public string PaymentStatus { get; set; } = "Unpaid"; // Unpaid, Partial, Paid, Refunded

      
        public string? Notes { get; set; }

      
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;

      
        public int CreatedByUserId { get; set; }

      
        public UserDto? CreatedByUser { get; set; }

        public List<SaleItemDto> SaleItems { get; set; } = new List<SaleItemDto>();
        public List<PaymentDto> Payments { get; set; } = new List<PaymentDto>();
    }
}
