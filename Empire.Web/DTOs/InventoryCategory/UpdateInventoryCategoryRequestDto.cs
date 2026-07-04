namespace Empire.Web.DTOs.InventoryCategory
{
    public class UpdateInventoryCategoryRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
