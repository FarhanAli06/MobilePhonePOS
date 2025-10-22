using System.ComponentModel.DataAnnotations;

namespace Empire.Application.DTOs.Shop;

public class CreateShopRequest
{
    [Required(ErrorMessage = "Shop name is required")]
    [StringLength(100, ErrorMessage = "Shop name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "City cannot exceed 50 characters")]
    public string? City { get; set; }

    [StringLength(50, ErrorMessage = "State cannot exceed 50 characters")]
    public string? State { get; set; }

    [StringLength(10, ErrorMessage = "Zip code cannot exceed 10 characters")]
    public string? ZipCode { get; set; }

    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string? Email { get; set; }
}

