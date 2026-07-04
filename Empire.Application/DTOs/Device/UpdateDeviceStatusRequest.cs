namespace Empire.Application.DTOs.Device;

public class UpdateDeviceStatusRequest
{
    public bool IsAvailableForSale { get; set; }
    
    public bool IsSold { get; set; }
    
    public int? SoldToCustomerId { get; set; }
}
