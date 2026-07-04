using Empire.Application.DTOs.Customer;

namespace Empire.Application.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetAsync(CustomerFilterRequestDto filter);
    Task<IEnumerable<CustomerDto>> GetByShopAsync(int shopId);
    Task<CustomerDto?> GetByIdAsync(int customerId, int shopId);
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request);
    Task<CustomerDto> UpdateAsync(int customerId, UpdateCustomerRequest request);
    Task<CustomerDto> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request);
    Task<IEnumerable<CustomerDto>> SearchAsync(int shopId, string term);
    Task<bool> DeleteAsync(int customerId, int shopId);
    Task<IEnumerable<CustomerDto>> SearchCustomersAsync(int shopId, string searchTerm);
    Task<IEnumerable<CustomerDto>> GetCustomersAsync(int shopId);
    Task<CustomerDto?> GetCustomerByIdAsync(int customerId);
    Task DeleteCustomerAsync(int customerId);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request);
}

