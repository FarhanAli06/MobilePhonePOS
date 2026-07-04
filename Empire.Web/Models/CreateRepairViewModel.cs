using System.ComponentModel.DataAnnotations;
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

    [StringLength(100, ErrorMessage = "IMEI cannot exceed 100 characters")]
    [Display(Name = "IMEI / Serial Number")]
    public string? IMEI { get; set; }

    [Required(ErrorMessage = "Issue description is required")]
    [StringLength(2000, ErrorMessage = "Issue description cannot exceed 2000 characters")]
    [Display(Name = "Issue Description")]
    public string IssueDescription { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Diagnosis cannot exceed 2000 characters")]
    [Display(Name = "Technician Diagnosis")]
    public string? Diagnosis { get; set; }

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Comments cannot exceed 500 characters")]
    [Display(Name = "Comments (Optional)")]
    public string? Comments { get; set; }

    [Display(Name = "Repair Status")]
    public string Status { get; set; } = "Pending";

    [Required(ErrorMessage = "Labor cost is required")]
    [Range(0, 99999.99, ErrorMessage = "Labor cost must be between 0 and 99,999.99")]
    [Display(Name = "Labor Cost")]
    public decimal LaborCost { get; set; }

    [Range(0, 99999.99, ErrorMessage = "Parts cost must be between 0 and 99,999.99")]
    [Display(Name = "Parts Cost")]
    public decimal PartsCost { get; set; }

    [Range(0, 99999.99, ErrorMessage = "Discount must be between 0 and 99,999.99")]
    [Display(Name = "Discount")]
    public decimal Discount { get; set; }

    [Required(ErrorMessage = "Cost is required")]
    [Range(0, 99999.99, ErrorMessage = "Cost must be between 0 and 99,999.99")]
    [Display(Name = "Total Cost")]
    public decimal Cost { get; set; }

    [Required(ErrorMessage = "Payment status is required")]
    [Display(Name = "Payment Status")]
    public string PaymentStatus { get; set; } = "Unpaid";

    [Range(0, 99999.99, ErrorMessage = "Amount paid must be between 0 and 99,999.99")]
    [Display(Name = "Amount Paid")]
    public decimal AmountPaid { get; set; }

    [Display(Name = "Estimated Completion Date")]
    public DateTime? EstimatedCompletionDate { get; set; }
    
    [Display(Name = "Company")]
    public int? CompanyId { get; set; }
    
    [Display(Name = "Parts Required")]
    public List<int> InventoryParts { get; set; } = new List<int>();
    
    // Additional properties for exact replica design
    [Display(Name = "Problem Description")]
    public string? ProblemDescription { get; set; }
    
    [Display(Name = "Technician Diagnosis")]
    public string? TechnicianDiagnosis { get; set; }
    
    [Display(Name = "Tax Rate")]
    public decimal TaxRate { get; set; }
    
    [Display(Name = "Selected Parts")]
    public List<SelectedPartDto> SelectedParts { get; set; } = new List<SelectedPartDto>();
}

public class SelectedPartDto
{
    public int InventoryItemId { get; set; }
    public int Quantity { get; set; }
}
