using Empire.Web.DTOs.Company;

namespace Empire.Web.Services.Company
{
    public interface ICompanyApiService
    {
        Task<List<CompanyDto>> GetAllAsync();
        Task<CompanyDto?> GetByIdAsync(int id);
        Task<CompanyDto?> CreateAsync(CreateCompanyRequestDto request);
        Task<CompanyDto?> UpdateAsync(UpdateCompanyRequestDto request);
        Task<bool> DeleteAsync(int id);
        Task<bool> CheckDuplicateNameAsync(string name, int? excludeId = null);
        Task<List<CompanySelectionDto>> GetSelectionListAsync();
        Task<List<CompanyDto>> GetActiveCompaniesAsync();
    }
}
