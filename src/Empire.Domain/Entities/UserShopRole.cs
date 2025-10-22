using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;
using Empire.Domain.Enums;

namespace Empire.Domain.Entities;

public class UserShopRole : BaseEntity
{
    [Required]
    public int UserId { get; set; }
    
    [Required]
    public int ShopId { get; set; }
    
    [Required]
    public UserRole Role { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Shop Shop { get; set; } = null!;
}

