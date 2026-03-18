namespace SITAG.Domain.Common;

/// <summary>
/// Base class for all domain entities. Every entity has a UUID primary key
/// and standard audit timestamps as described in DATABASE_MODEL.md §2.6.
/// Setters are public so EF Core can materialise entities and the DbContext
/// interceptor can stamp UpdatedAt automatically.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
