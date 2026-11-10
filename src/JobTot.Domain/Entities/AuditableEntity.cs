using JobTot.Domain.Enums;

namespace JobTot.Domain.Entities;

public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedAccountId { get; set; }
    public DateTimeOffset? UpdatedDate { get; set; }
    public Guid? UpdatedAccountId { get; set; }
    public Account? CreatedAccount { get; set; }
    public Account? UpdatedAccount { get; set; }
}
