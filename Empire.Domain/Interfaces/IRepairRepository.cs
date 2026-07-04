using Empire.Domain.Entities;
using Empire.Domain.Enums;

namespace Empire.Domain.Interfaces;

public interface IRepairRepository : IRepository<Repair>
{
    Task<IEnumerable<Repair>> GetRepairsByShopIdAsync(int shopId);
    Task<IEnumerable<Repair>> GetRepairsByDateRangeAsync(int shopId, DateTime startDate, DateTime endDate);
    Task<IEnumerable<Repair>> GetRepairsByStatusAsync(int shopId, RepairStatus status);
    Task<IEnumerable<Repair>> GetRepairsByDateRangeAndStatusAsync(int shopId, DateTime startDate, DateTime endDate, RepairStatus status);
    Task<IEnumerable<Repair>> GetRepairsByCustomerIdAsync(int customerId);
    Task<string> GenerateRepairNumberAsync(int shopId);
}

