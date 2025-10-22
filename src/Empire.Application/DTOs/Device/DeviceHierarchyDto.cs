namespace Empire.Application.DTOs.Device;

public class DeviceHierarchyDto
{
    public List<string> Brands { get; set; } = new();
    public Dictionary<string, List<string>> CategoriesByBrand { get; set; } = new();
    public Dictionary<string, List<DeviceSelectionDto>> ModelsByBrandCategory { get; set; } = new();
}

