using System.ComponentModel.DataAnnotations;

namespace Empire.Application.DTOs.Repair;

public class UpdateRepairRequest
{
    public int? BrandId { get; set; }
    
    public int? DeviceCategoryId { get; set; }
    
    public int? DeviceModelId { get; set; }
    
    [MaxLength(100)]
    public string? Issue { get; set; }
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(1000)]
    public string? Comments { get; set; }
    
    public string? Status { get; set; }
    
    public string? PaymentStatus { get; set; }
    
    public decimal? Cost { get; set; }
}

