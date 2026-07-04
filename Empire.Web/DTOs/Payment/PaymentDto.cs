using Empire.Web.DTOs.Sale;
using Empire.Web.DTOs.User;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Empire.Web.DTOs.Payment
{
    public class PaymentDto
    {
        public int SaleId { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Zelle, Venmo, etc.

        public string? TransactionId { get; set; }

        public int UserId { get; set; }

        public SaleDto Sale { get; set; } = null!;
        public UserDto User { get; set; } = null!;
    }
}
