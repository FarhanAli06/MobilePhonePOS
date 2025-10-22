using Empire.Application.DTOs.Repair;

namespace Empire.Web.Models;

public class RepairIndexViewModel
{
    public IEnumerable<RepairDto> Repairs { get; set; } = new List<RepairDto>();
    public string SearchTerm { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

