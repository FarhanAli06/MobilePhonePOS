namespace Empire.Web.DTOs.Pos
{
    public class POSSalesSearchRequestDto
    {
        public int ShopId { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
