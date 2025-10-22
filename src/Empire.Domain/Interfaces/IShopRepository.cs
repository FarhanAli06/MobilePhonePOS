using Empire.Domain.Entities;

namespace Empire.Domain.Interfaces;

public interface IShopRepository : IRepository<Shop>
{
    Task<IEnumerable<Shop>> GetActiveShopsAsync();
    Task<IEnumerable<Shop>> GetShopsByUserIdAsync(int userId);
    Task<IEnumerable<Shop>> GetShopsByUserAsync(int userId);
}

