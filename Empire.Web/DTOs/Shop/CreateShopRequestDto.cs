using System.ComponentModel.DataAnnotations;

namespace Empire.Web.DTOs.Shop;

public class CreateShopRequestDto
{
    [Required(ErrorMessage = "Shop name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Shop name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
    public string? Address { get; set; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "State cannot exceed 100 characters")]
    public string? State { get; set; }

    [StringLength(20, ErrorMessage = "Zip code cannot exceed 20 characters")]
    public string? ZipCode { get; set; }

    [Phone(ErrorMessage = "Invalid phone number format")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string? Email { get; set; }

    public string? LogoPath { get; set; }
}
