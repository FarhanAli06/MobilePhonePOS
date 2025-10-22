using System.ComponentModel.DataAnnotations;
using Empire.Domain.Enums;

namespace Empire.Application.DTOs.Repair;

public class CreateRepairRequest
{
    [Required]
    public int ShopId { get; set; }
    
    [Required]
    public int CustomerId { get; set; }
    
    public int? BrandId { get; set; }
    
    public int? DeviceCategoryId { get; set; }
    
    public int? DeviceModelId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Issue { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Comments { get; set; } = string.Empty; // New field
    
    [Required]
    public decimal Cost { get; set; }
    
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid; // New field
}

