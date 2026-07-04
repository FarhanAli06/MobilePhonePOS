using Empire.Application.DTOs.Repair;

namespace Empire.Application.Interfaces;

public interface IRepairService
{
    Task<RepairDto> CreateRepairAsync(CreateRepairRequest request, int createdByUserId);
    Task<RepairDto?> GetRepairByIdAsync(int repairId, int shopId);
    Task<IEnumerable<RepairDto>> GetRepairsAsync(RepairFilterRequest filter);
    Task<RepairDto?> UpdateRepairAsync(int repairId, UpdateRepairRequest request, int modifiedByUserId);
    Task<bool> DeleteRepairAsync(int repairId, int shopId);
    Task<IEnumerable<RepairDto>> GetRepairsByCustomerAsync(int customerId, int shopId);
    Task<bool> AddPartsToRepairAsync(int repairId, AddRepairPartsRequest request, int shopId);
    Task<bool> ReplacePartsAsync(int repairId, AddRepairPartsRequest request, int shopId);
    Task<IEnumerable<RepairPartDto>> GetRepairPartsAsync(int repairId, int shopId);
    Task<IEnumerable<RepairActivityDto>> GetRepairActivitiesAsync(int repairId, int shopId);

    // ── Payment Ledger ────────────────────────────────────────────────────────
    Task<IEnumerable<RepairPaymentDto>> GetRepairPaymentsAsync(int repairId);
    Task<RepairPaymentDto> AddRepairPaymentAsync(AddRepairPaymentRequest request);
    Task<RepairPaymentDto?> UpdateRepairPaymentAsync(int paymentId, UpdateRepairPaymentRequest request);
    Task<bool> DeleteRepairPaymentAsync(int paymentId);
    Task RecalculateRepairPaymentStatusAsync(int repairId);
}
