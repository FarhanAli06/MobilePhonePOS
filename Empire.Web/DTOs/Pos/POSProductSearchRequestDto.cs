namespace Empire.Web.DTOs.Pos
{
    public class POSProductSearchRequestDto
    {
        public int ShopId { get; set; }
        public string SearchTerm { get; set; } = string.Empty;
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
    }
}
