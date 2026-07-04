namespace Empire.Web.DTOs.DeviceModel
{
    public class CreateDeviceModelRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int BrandId { get; set; }
        public int DeviceCategoryId { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
