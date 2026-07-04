using Empire.Web.DTOs.Repair;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empire.Web.Services.Repair
{
    /// <summary>
    /// Interface for Repair API service operations
    /// </summary>
    public interface IRepairApiService
    {
        Task<IEnumerable<RepairDto>> GetByShopAsync(int shopId);
        Task<RepairDto?> GetByIdAsync(int repairId);
        Task<RepairDto?> CreateAsync(CreateRepairRequest request);
        Task<RepairDto?> UpdateAsync(int id, UpdateRepairRequestDto request);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<RepairDto>> SearchAsync(string searchTerm);
        Task<IEnumerable<RepairDto>> GetByCustomerIdAsync(int customerId);
        Task<IEnumerable<RepairDto>> GetByStatusAsync(string status);
        Task<SelectList> GetRepairStatusesAsync();
        Task<SelectList> GetPaymentStatusesAsync();
        Task<IEnumerable<RepairDto>> GetRepairsAsync(int shopId);
        Task<bool> AddPartsToRepairAsync(int repairId, object request);
        Task<bool> ReplacePartsAsync(int repairId, object request);
        Task<IEnumerable<RepairPartDto>> GetRepairPartsAsync(int repairId);
        Task<IEnumerable<RepairActivityDto>> GetRepairActivitiesAsync(int repairId);

        // Payment Ledger
        Task<IEnumerable<RepairPaymentDto>> GetRepairPaymentsAsync(int repairId);
        Task<RepairPaymentDto?> AddRepairPaymentAsync(int repairId, AddRepairPaymentRequestDto request);
        Task<RepairPaymentDto?> UpdateRepairPaymentAsync(int paymentId, UpdateRepairPaymentRequestDto request);
        Task<bool> DeleteRepairPaymentAsync(int paymentId);
    }
}
