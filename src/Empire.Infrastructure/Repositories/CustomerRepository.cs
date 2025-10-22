using Microsoft.EntityFrameworkCore;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(EmpireDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Customer>> GetCustomersByShopIdAsync(int shopId)
    {
        return await _dbSet
            .Where(c => c.ShopId == shopId && !c.IsDeleted)
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Customer>> GetCustomersByShopAsync(int shopId, string? searchTerm = null)
    {
        var query = _dbSet.Where(c => c.ShopId == shopId && !c.IsDeleted);

        if (!string.IsNullOrEmpty(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(c => 
                c.FirstName.ToLower().Contains(lowerSearchTerm) ||
                c.LastName.ToLower().Contains(lowerSearchTerm) ||
                c.Phone.Contains(searchTerm) ||
                c.Email.ToLower().Contains(lowerSearchTerm));
        }

        return await query
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToListAsync();
    }

    public async Task<Customer?> GetByPhoneAsync(int shopId, string phone)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.ShopId == shopId && c.Phone == phone && !c.IsDeleted);
    }

    public async Task<IEnumerable<Customer>> SearchCustomersAsync(int shopId, string searchTerm)
    {
        var lowerSearchTerm = searchTerm.ToLower();
        
        return await _dbSet
            .Where(c => c.ShopId == shopId && !c.IsDeleted &&
                       (c.FirstName.ToLower().Contains(lowerSearchTerm) ||
                        c.LastName.ToLower().Contains(lowerSearchTerm) ||
                        c.Phone.Contains(searchTerm) ||
                        c.Email.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.LastName)
            .ToListAsync();
    }
}

