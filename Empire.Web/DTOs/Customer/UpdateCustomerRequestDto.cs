using System.ComponentModel.DataAnnotations;

namespace Empire.Web.DTOs.Customer;

public class UpdateCustomerRequestDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? LastName { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    /// <summary>Maps to the API's "Phone" field. Optional.</summary>
    [StringLength(20)]
    public string? Phone { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
}
