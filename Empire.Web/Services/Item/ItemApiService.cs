using Empire.Web.DTOs.Item;
using Empire.Web.Services.Http;

namespace Empire.Web.Services.Item
{
    public class ItemApiService : IItemApiService
    {
        private readonly IHttpClientService _httpClient;
        private readonly ILogger<ItemApiService> _logger;
        private const string BaseEndpoint = "/api/items";

        public ItemApiService(IHttpClientService httpClient, ILogger<ItemApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<ItemDto>> GetAllAsync()
        {
            try
            {
                return await _httpClient.GetAsync<List<ItemDto>>(BaseEndpoint) ?? new List<ItemDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all items from API");
                return new List<ItemDto>();
            }
        }

        public async Task<ItemDto?> GetByIdAsync(int id)
        {
            try
            {
                return await _httpClient.GetAsync<ItemDto>($"{BaseEndpoint}/{id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting item {Id} from API", id);
                return null;
            }
        }

        public async Task<ItemDto?> CreateAsync(CreateItemRequest request)
        {
            try
            {
                return await _httpClient.PostAsync<CreateItemRequest, ItemDto>(BaseEndpoint, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item via API");
                return null;
            }
        }

        public async Task<ItemDto?> UpdateAsync(int id, UpdateItemRequest request)
        {
            try
            {
                return await _httpClient.PutAsync<UpdateItemRequest, ItemDto>($"{BaseEndpoint}/{id}", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {Id} via API", id);
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
                _logger.LogError(ex, "Error deleting item {Id} via API", id);
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
                _logger.LogError(ex, "Error checking duplicate item name via API");
                return false;
            }
        }

        public async Task<List<ItemDto>> GetActiveItemsAsync()
        {
            try
            {
                return await _httpClient.GetAsync<List<ItemDto>>($"{BaseEndpoint}/active") ?? new List<ItemDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active items from API");
                return new List<ItemDto>();
            }
        }
    }
}
