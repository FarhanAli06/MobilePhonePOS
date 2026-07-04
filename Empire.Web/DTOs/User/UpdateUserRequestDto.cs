using System.ComponentModel.DataAnnotations;
namespace Empire.Web.DTOs.User;

public class UpdateUserRequestDto
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
    [Range(1, int.MaxValue, ErrorMessage = "Please select a role")]
    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(100, MinimumLength = 6)]
    public string? NewPassword { get; set; }
}

