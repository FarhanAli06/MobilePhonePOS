using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class Customer : BaseEntity
{
    [Required]
    public int ShopId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string City { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string State { get; set; } = string.Empty;
    
    [MaxLength(10)]
    public string ZipCode { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual Shop Shop { get; set; } = null!;
    public virtual ICollection<Repair> Repairs { get; set; } = new List<Repair>();
}

