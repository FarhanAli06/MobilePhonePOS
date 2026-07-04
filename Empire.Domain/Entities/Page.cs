using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

/// <summary>
/// Represents a navigable page / menu item in the application.
/// Each record maps to one sidebar entry (or sub-entry) that can be
/// assigned to users individually.
/// </summary>
public class Page : BaseEntity
{
    /// <summary>Display name shown in the sidebar, e.g. "Repairs".</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique system key used in code comparisons, e.g. "repairs".
    /// Lower-case, no spaces.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string PageKey { get; set; } = string.Empty;

    /// <summary>ASP.NET Core controller name, e.g. "Repairs".</summary>
    [MaxLength(100)]
    public string? ControllerName { get; set; }

    /// <summary>ASP.NET Core action name, e.g. "Index".</summary>
    [MaxLength(100)]
    public string? ActionName { get; set; }

    /// <summary>Font-Awesome icon class, e.g. "fas fa-tools".</summary>
    [MaxLength(100)]
    public string? Icon { get; set; }

    /// <summary>
    /// Optional parent page ID for sub-menu grouping.
    /// NULL means this is a top-level menu item.
    /// </summary>
    public int? ParentPageId { get; set; }

    /// <summary>Controls the order items appear in the sidebar.</summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Logical group label shown as a sidebar section header,
    /// e.g. "Main Menu" or "Administration".
    /// </summary>
    [MaxLength(100)]
    public string? GroupName { get; set; }

    /// <summary>Whether this page is currently active / visible.</summary>
    public bool IsActive { get; set; } = true;

    // ── Navigation properties ──────────────────────────────────────────────
    public virtual Page? ParentPage { get; set; }
    public virtual ICollection<Page> ChildPages { get; set; } = new List<Page>();
    public virtual ICollection<UserPagePermission> UserPagePermissions { get; set; } = new List<UserPagePermission>();
}
