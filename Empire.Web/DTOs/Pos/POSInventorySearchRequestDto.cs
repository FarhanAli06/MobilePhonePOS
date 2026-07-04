namespace Empire.Web.DTOs.Pos
{
    public class POSInventorySearchRequestDto
    {
        public int ShopId { get; set; }
        public string? SearchTerm { get; set; }
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public string? Status { get; set; }
    }
}
