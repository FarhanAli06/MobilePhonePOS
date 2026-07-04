using System.ComponentModel.DataAnnotations;

namespace Empire.Domain.Entities;

/// <summary>
/// Role entity for managing user roles
/// </summary>
public class Role
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<UserShopRole> UserShopRoles { get; set; } = new List<UserShopRole>();
}
