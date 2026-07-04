using Empire.Domain.Entities;

namespace Empire.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<IEnumerable<Customer>> GetByShopIdAsync(int shopId);
    Task<IEnumerable<Customer>> GetByShopAsync(int shopId, string? searchTerm = null);
    Task<Customer?> GetByPhoneAsync(int shopId, string phone);
    Task<IEnumerable<Customer>> SearchAsync(int shopId, string searchTerm);
}

