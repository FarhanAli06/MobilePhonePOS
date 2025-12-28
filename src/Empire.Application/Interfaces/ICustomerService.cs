using Empire.Application.DTOs.Customer;

namespace Empire.Application.Interfaces;

public interface ICustomerService
{
    Task<IEnumerable<CustomerDto>> GetCustomersAsync(CustomerFilterRequest filter);
    Task<IEnumerable<CustomerDto>> GetCustomersByShopAsync(int shopId);
    Task<CustomerDto?> GetCustomerByIdAsync(int customerId, int shopId);
    Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request);
    Task<CustomerDto> UpdateCustomerAsync(int customerId, UpdateCustomerRequest request);
    Task<IEnumerable<CustomerDto>> SearchCustomersAsync(int shopId, string term);
    Task<bool> DeleteCustomerAsync(int customerId, int shopId);
}

