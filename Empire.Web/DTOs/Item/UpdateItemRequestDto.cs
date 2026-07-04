namespace Empire.Web.DTOs.Item
{
    public class UpdateItemRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
