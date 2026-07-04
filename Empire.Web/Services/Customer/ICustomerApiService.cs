using Empire.Web.DTOs.Customer;

namespace Empire.Web.Services.Customer
{
    /// <summary>
    /// Interface for Customer API service operations
    /// </summary>
    public interface ICustomerApiService
    {
        Task<CustomerDto?> GetByIdAsync(int id);
        Task<IEnumerable<CustomerDto>> GetByShopAsync(int shopId);
        Task<CustomerDto?> CreateAsync(CreateCustomerRequestDto request);
        Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerRequestDto request);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<CustomerDto>> SearchAsync(string searchTerm, int shopId);
        Task<IEnumerable<CustomerDto>> GetCustomersAsync(int shopId);
    }
}
