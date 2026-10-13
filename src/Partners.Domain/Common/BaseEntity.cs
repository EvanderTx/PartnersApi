namespace Partners.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
    public byte[] RowVersion { get; protected set; } = [];

    protected void MarkAsCreated(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        CreatedAt = utcNow;
        UpdatedAt = utcNow;
    }

    protected void MarkAsUpdated(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        UpdatedAt = utcNow;
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Dates must be in UTC.", nameof(value));
    }
}