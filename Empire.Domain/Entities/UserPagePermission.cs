using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

/// <summary>
/// Grants a specific user access to a specific page within a specific shop.
/// ShopId = 0 means the permission applies globally to all shops for that user.
/// When a user logs in the set of granted page keys is stored in session
/// so the sidebar can render only the permitted items without an extra
/// database round-trip on every request.
/// </summary>
public class UserPagePermission : BaseEntity
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int PageId { get; set; }

    /// <summary>The shop this permission applies to. 0 means global (all shops).</summary>
    public int ShopId { get; set; } = 0;

    /// <summary>Whether this permission is currently active.</summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>Who granted this permission (admin user ID).</summary>
    public int? GrantedByUserId { get; set; }

    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;

    // ── Navigation properties ──────────────────────────────────────────────
    public virtual User User { get; set; } = null!;
    public virtual Page Page { get; set; } = null!;
    public virtual Shop? Shop { get; set; }
}
