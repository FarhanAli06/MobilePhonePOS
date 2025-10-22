using System.ComponentModel.DataAnnotations;
using Empire.Domain.Enums;

namespace Empire.Web.Models;

public class CreateRepairViewModel
{
    [Required(ErrorMessage = "Customer is required")]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Display(Name = "Brand")]
    public int? BrandId { get; set; }

    [Display(Name = "Device Category")]
    public int? DeviceCategoryId { get; set; }

    [Display(Name = "Device Model")]
    public int? DeviceModelId { get; set; }

    [Required(ErrorMessage = "Issue is required")]
    [StringLength(200, ErrorMessage = "Issue cannot exceed 200 characters")]
    [Display(Name = "Issue")]
    public string Issue { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters")]
    [Display(Name = "Comments (Optional)")]
    public string? Comments { get; set; }

    [Required(ErrorMessage = "Cost is required")]
    [Range(0, 99999.99, ErrorMessage = "Cost must be between 0 and 99,999.99")]
    [Display(Name = "Cost")]
    public decimal Cost { get; set; }

    [Required(ErrorMessage = "Payment status is required")]
    [Display(Name = "Payment Status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;
}

