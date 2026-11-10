namespace Empire.Web.DTOs.Device
{
    public class DeviceFilterRequestDto
    {
        public int ShopId { get; set; }
        public string? SearchTerm { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public bool? IsAvailableForSale { get; set; }
        public bool? IsSold { get; set; }
    }
}
