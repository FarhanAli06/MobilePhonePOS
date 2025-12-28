namespace Empire.Application.DTOs.Company;

public class CompanySelectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
