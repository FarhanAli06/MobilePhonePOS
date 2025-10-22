using System.ComponentModel.DataAnnotations;

namespace Empire.Application.DTOs.Customer;

public class UpdateCustomerRequest
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }
    
    [StringLength(200)]
    public string? Address { get; set; }
    
    [StringLength(50)]
    public string? City { get; set; }
    
    [StringLength(50)]
    public string? State { get; set; }
    
    [StringLength(10)]
    public string? ZipCode { get; set; }
}

