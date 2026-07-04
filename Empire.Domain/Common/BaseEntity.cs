using System.ComponentModel.DataAnnotations;

namespace Empire.Domain.Common;

public abstract class BaseEntity
{
    [Key]
    public int Id { get; set; }
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public DateTime? ModifiedDate { get; set; }
    
    public bool IsDeleted { get; set; } = false;
}

