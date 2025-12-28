using Empire.Application.DTOs.Company;

namespace Empire.Application.Interfaces;

public interface ICompanyService
{
    Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync();
    Task<CompanyDto> GetCompanyByIdAsync(int id);
    Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request);
    Task UpdateCompanyAsync(UpdateCompanyRequest request);
    Task DeleteCompanyAsync(int id);
    Task<IEnumerable<CompanySelectionDto>> GetCompanySelectionListAsync();
}
