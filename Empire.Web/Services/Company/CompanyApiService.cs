using Empire.Web.DTOs.Company;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.Company
{
    public class CompanyApiService : ICompanyApiService
    {
        private readonly IHttpClientService _httpClient;
        private readonly ILogger<CompanyApiService> _logger;
        private const string BaseEndpoint = "/api/companies";

        public CompanyApiService(IHttpClientService httpClient, ILogger<CompanyApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<CompanyDto>> GetAllAsync()
        {
            try
            {
                return await _httpClient.GetAsync<List<CompanyDto>>(BaseEndpoint) ?? new List<CompanyDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all companies from API");
                return new List<CompanyDto>();
            }
        }

        public async Task<CompanyDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetAsync<CompanyDto>($"{BaseEndpoint}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company {Id} from API", id);
                return null;
            }
        }

        public async Task<CompanyDto?> CreateAsync(CreateCompanyRequestDto request)
        {
            try
            {
                return await _httpClient.PostAsync<CreateCompanyRequestDto, CompanyDto>(BaseEndpoint, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company via API");
                return null;
            }
        }

        public async Task<CompanyDto?> UpdateAsync(UpdateCompanyRequestDto request)
        {
            try
            {
                return await _httpClient.PutAsync<UpdateCompanyRequestDto, CompanyDto>($"{BaseEndpoint}/{request.Id}", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company {Id} via API", request.Id);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                return await _httpClient.DeleteAsync($"{BaseEndpoint}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company {Id} via API", id);
                return false;
            }
        }

        public async Task<bool> CheckDuplicateNameAsync(string name, int? excludeId = null)
        {
            try
            {
                var url = $"{BaseEndpoint}/check-duplicate?name={Uri.EscapeDataString(name)}";
                if (excludeId.HasValue)
                    url += $"&excludeId={excludeId.Value}";
                return await _httpClient.GetAsync<bool>(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking duplicate company name via API");
                return false;
            }
        }

        public async Task<List<CompanySelectionDto>> GetSelectionListAsync()
        {
            try
            {
                return await _httpClient.GetAsync<List<CompanySelectionDto>>($"{BaseEndpoint}/selections") ?? new List<CompanySelectionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company selections from API");
                return new List<CompanySelectionDto>();
            }
        }

        public async Task<List<CompanyDto>> GetActiveCompaniesAsync()
        {
            try
            {
                return await _httpClient.GetAsync<List<CompanyDto>>($"{BaseEndpoint}/active") ?? new List<CompanyDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active companies from API");
                return new List<CompanyDto>();
            }
        }
    }
}
