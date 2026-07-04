using System.ComponentModel.DataAnnotations;

namespace Empire.Domain.Entities
{
    public class Styling
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string Icon { get; set; } = "circle";

        [MaxLength(20)]
        public string Color { get; set; } = "#6c757d";

        /// <summary>Foreground/text colour rendered on top of Color background.</summary>
        [MaxLength(20)]
        public string TextColor { get; set; } = "#ffffff";

        /// <summary>Bootstrap badge variant: primary, secondary, success, danger, warning, info, dark, light</summary>
        [MaxLength(30)]
        public string BadgeVariant { get; set; } = "secondary";

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation: entities that use this styling
        public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();
        public virtual ICollection<DeviceCategory> DeviceCategories { get; set; } = new List<DeviceCategory>();
        public virtual ICollection<DeviceModel> DeviceModels { get; set; } = new List<DeviceModel>();
    }
}
