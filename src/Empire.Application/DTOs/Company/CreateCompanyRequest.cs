using System.ComponentModel.DataAnnotations;

namespace Empire.Application.DTOs.Company;

public class CreateCompanyRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int Rank { get; set; }

    public bool IsDefault { get; set; } = false;
}
