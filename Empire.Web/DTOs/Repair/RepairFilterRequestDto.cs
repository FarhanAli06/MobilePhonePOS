namespace Empire.Web.DTOs.Repair
{
    public class RepairFilterRequestDto
    {
        public int ShopId { get; set; }

        public DateTime? StartDate { get; set; } // Optional date filter

        public DateTime? EndDate { get; set; } // Optional date filter

        public string? Status { get; set; } // Optional status filter

        public string? PaymentStatus { get; set; } // Optional payment status filter

        public int? CustomerId { get; set; } // Optional customer filter

        public string? SearchTerm { get; set; } // Optional search term for repair number, customer name, etc.
    }
}
