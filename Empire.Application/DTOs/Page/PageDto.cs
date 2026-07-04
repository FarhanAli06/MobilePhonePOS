namespace Empire.Application.DTOs.Page;

public class PageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PageKey { get; set; } = string.Empty;
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    public string? Icon { get; set; }
    public int? ParentPageId { get; set; }
    public int DisplayOrder { get; set; }
    public string? GroupName { get; set; }
    public bool IsActive { get; set; }
    public List<PageDto> ChildPages { get; set; } = new();
}
