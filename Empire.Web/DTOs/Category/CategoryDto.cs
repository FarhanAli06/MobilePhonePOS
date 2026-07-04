
namespace Empire.Web.DTOs.Category
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string CategoryType { get; set; } = string.Empty; // e.g., "Inventory", "Device", "Service", etc.

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        // Optional parent category for hierarchical structure
        public int? ParentCategoryId { get; set; }
    }
}
