using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;
using Empire.Domain.Enums;

namespace Empire.Domain.Entities;

public class Company : AuditableEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    public int Rank { get; set; }

    public bool IsDefault { get; set; }

    // Navigation properties
    public virtual ICollection<Repair> Repairs { get; set; } = new List<Repair>();
    public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
}
