using System.ComponentModel.DataAnnotations;

namespace Empire.Web.Models;

public class EditRepairViewModel
{
    public int Id { get; set; }

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
    [Display(Name = "Comments")]
    public string? Comments { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [Display(Name = "Status")]
    public string Status { get; set; } = "InProgress";

    [Required(ErrorMessage = "Payment status is required")]
    [Display(Name = "Payment Status")]
    public string PaymentStatus { get; set; } = "Unpaid";

    [Required(ErrorMessage = "Cost is required")]
    [Range(0, 99999.99, ErrorMessage = "Cost must be between 0 and 99,999.99")]
    [Display(Name = "Cost")]
    public decimal Cost { get; set; }

    // Read-only display properties
    public string CustomerName { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
}

