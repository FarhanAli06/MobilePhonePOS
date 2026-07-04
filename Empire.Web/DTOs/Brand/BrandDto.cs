namespace Empire.Web.DTOs.Brand;

public class BrandDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "devices";
    public string Color { get; set; } = "#000000";
    public bool IsActive { get; set; } = true;
    public int? DisplayOrder { get; set; }
    public DateTime? CreatedDate { get; set; }
}
