

namespace Empire.Web.DTOs.Company;

public class CreateCompanyRequestDto
{
    public string Name { get; set; } = string.Empty;
    
    public int Rank { get; set; }

    public bool IsDefault { get; set; } = false;
}
