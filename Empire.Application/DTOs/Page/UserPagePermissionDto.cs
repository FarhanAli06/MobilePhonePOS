namespace Empire.Application.DTOs.Page;

public class UserPagePermissionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int PageId { get; set; }
    public string PageName { get; set; } = string.Empty;
    public string PageKey { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
    public DateTime GrantedDate { get; set; }
}

public class AssignUserPagesRequest
{
    public int UserId { get; set; }
    public int ShopId { get; set; }
    /// <summary>
    /// Full list of page IDs to grant. Any existing permissions not in this
    /// list will be revoked (IsGranted = false).
    /// </summary>
    public List<int> PageIds { get; set; } = new();
}

public class UserPermissionSummaryDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<PageDto> GrantedPages { get; set; } = new();
    /// <summary>Flat list of granted page keys for quick session storage.</summary>
    public List<string> GrantedPageKeys { get; set; } = new();
}
