using System.ComponentModel.DataAnnotations;

namespace Empire.Web.DTOs.Customer;

public class CreateCustomerRequestDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    /// <summary>Email — optional, matches API CreateCustomerRequest.Email</summary>
    public string? Email { get; set; }

    /// <summary>Phone number — optional, matches API CreateCustomerRequest.Phone</summary>
    public string? Phone { get; set; }


    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public int? ShopId { get; set; }
}
