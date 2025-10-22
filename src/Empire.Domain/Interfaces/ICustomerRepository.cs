using Empire.Domain.Entities;

namespace Empire.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<IEnumerable<Customer>> GetCustomersByShopIdAsync(int shopId);
    Task<IEnumerable<Customer>> GetCustomersByShopAsync(int shopId, string? searchTerm = null);
    Task<Customer?> GetByPhoneAsync(int shopId, string phone);
    Task<IEnumerable<Customer>> SearchCustomersAsync(int shopId, string searchTerm);
}

