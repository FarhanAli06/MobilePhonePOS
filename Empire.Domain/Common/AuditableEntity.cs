using System.ComponentModel.DataAnnotations;

namespace Empire.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;
    
    public DateTime? ModifiedDateUtc { get; set; }
}
