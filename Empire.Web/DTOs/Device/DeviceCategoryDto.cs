namespace Empire.Web.DTOs;

public class DeviceCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "devices";
    public string Color { get; set; } = "#000000";
}
