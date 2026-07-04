using System.ComponentModel.DataAnnotations;

namespace Empire.Web.DTOs.Device;

public class DeviceModelDto
{
    public int Id { get; set; }
    public string? Name { get; set; } = string.Empty;
    public string ModelNumber { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string Description { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public int DeviceCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Icon { get; set; } = "phone_iphone";
    public string Color { get; set; } = "#000000";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}
