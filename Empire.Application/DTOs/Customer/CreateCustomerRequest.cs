using System.ComponentModel.DataAnnotations;
using Empire.Application.Validation;

namespace Empire.Application.DTOs.Customer;

public class CreateCustomerRequest
{
    public int ShopId { get; set; }
    
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string? LastName { get; set; }
    
    [StringLength(20)]
    public string? Phone { get; set; }
    
    [OptionalEmailAddress]
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

