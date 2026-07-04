namespace Empire.Web.DTOs.Page;

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
    /// <summary>
    /// When returned from GetPagesForUserAsync, IsActive is repurposed as
    /// "IsGranted" — true means the user has been granted this page.
    /// </summary>
    public bool IsActive { get; set; }
    public List<PageDto> ChildPages { get; set; } = new();
}

public class UserPermissionSummaryDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<PageDto> GrantedPages { get; set; } = new();
    public List<string> GrantedPageKeys { get; set; } = new();
}

public class AssignUserPagesRequest
{
    public int UserId { get; set; }
    public int ShopId { get; set; }
    public List<int> PageIds { get; set; } = new();
}
