using Empire.Web.DTOs.Shop;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.Shop
{
    public class ShopApiService : IShopApiService
    {
        private readonly IHttpClientService _httpClient;
        private readonly ILogger<ShopApiService> _logger;
        private const string BaseEndpoint = "api/shops";

        public ShopApiService(IHttpClientService httpClient, ILogger<ShopApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<ShopDto>> GetAllAsync()
        {
            try
            {
                return await _httpClient.GetAsync<List<ShopDto>>(BaseEndpoint) ?? new List<ShopDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all shops from API");
                return new List<ShopDto>();
            }
        }

        public async Task<ShopDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetAsync<ShopDto>($"{BaseEndpoint}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shop {Id} from API", id);
                return null;
            }
        }

        public async Task<ShopDto?> GetShopByIdAsync(int id)
            => await GetByIdAsync(id);

        public async Task<List<ShopDto>> GetShopsByUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Fetching shops for user {UserId} from API", userId);
                return await _httpClient.GetAsync<List<ShopDto>>($"{BaseEndpoint}/user/{userId}")
                       ?? new List<ShopDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shops for user {UserId}", userId);
                return new List<ShopDto>();
            }
        }

        public async Task<ShopDto?> GetCurrentShopAsync()
        {
            try
            {
                return await _httpClient.GetAsync<ShopDto>($"{BaseEndpoint}/current");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current shop from API");
                return null;
            }
        }

        public async Task<ShopDto?> CreateAsync(CreateShopRequestDto request)
        {
            try
            {
                return await _httpClient.PostAsync<CreateShopRequestDto, ShopDto>(BaseEndpoint, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shop via API");
                return null;
            }
        }

        public async Task<ShopDto> CreateShopAsync(CreateShopRequestDto request, int userId)
        {
            try
            {
                return await _httpClient.PostAsync<CreateShopRequestDto, ShopDto>(BaseEndpoint, request)
                       ?? throw new Exception("Failed to create shop — API returned null");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shop for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ShopDto?> UpdateAsync(int id, UpdateShopRequestDto request)
        {
            try
            {
                return await _httpClient.PutAsync<UpdateShopRequestDto, ShopDto>($"{BaseEndpoint}/{id}", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shop {Id} via API", id);
                return null;
            }
        }

        public async Task UpdateShopAsync(int id, UpdateShopRequestDto request, int userId)
        {
            try
            {
                await _httpClient.PutAsync<UpdateShopRequestDto, ShopDto>($"{BaseEndpoint}/{id}", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating shop {Id} for user {UserId}", id, userId);
                throw;
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
                _logger.LogError(ex, "Error deleting shop {Id} via API", id);
                return false;
            }
        }

        public async Task DeleteShopAsync(int id)
        {
            try
            {
                await _httpClient.DeleteAsync($"{BaseEndpoint}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting shop {Id}", id);
                throw;
            }
        }
    }
}
