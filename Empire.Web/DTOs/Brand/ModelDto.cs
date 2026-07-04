namespace Empire.Web.DTOs.Brand
{
    public class ModelDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? BrandId { get; set; }
        public int? CategoryId { get; set; }
        public string Icon { get; set; } = "devices";
        public string Color { get; set; } = "#000000";
    }
}
