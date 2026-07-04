using System.ComponentModel.DataAnnotations;

namespace Empire.Web.DTOs.DeviceCategory
{
    public class DeviceCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        public string? Icon { get; set; }

        public string? Color { get; set; }
    }
}
