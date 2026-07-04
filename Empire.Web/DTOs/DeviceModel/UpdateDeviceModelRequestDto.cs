namespace Empire.Web.DTOs.DeviceModel
{
    public class UpdateDeviceModelRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int BrandId { get; set; }
        public int DeviceCategoryId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
