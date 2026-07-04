namespace Empire.Web.DTOs.DeviceCategory
{
    public class CreateDeviceCategoryRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
