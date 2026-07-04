namespace Empire.Web.DTOs.Device
{
    public class UpdateDeviceStatusRequestDto
    {
        public bool IsAvailableForSale { get; set; }
        public bool IsSold { get; set; }
        public int? SoldToCustomerId { get; set; }
    }
}
