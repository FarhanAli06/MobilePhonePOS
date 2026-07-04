namespace Empire.Web.DTOs.Device
{
    public class MarkAsSoldRequestDto
    {
        public int CustomerId { get; set; }
        public decimal? SalePrice { get; set; }
    }
}
