using System.ComponentModel.DataAnnotations;
using Empire.Domain.Enums;

namespace Empire.Application.DTOs.User;

public class UpdateUserRequest
{
    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [StringLength(15)]
    public string? Phone { get; set; }

    [Required]
    public int ShopId { get; set; }

    [Required]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(100, MinimumLength = 6)]
    public string? NewPassword { get; set; }
}

