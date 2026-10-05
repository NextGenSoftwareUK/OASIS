namespace Domain.Common;

/// <summary>
/// Represents a base entity that provides common properties for all entities.
/// This class is typically used as a base class for other domain entities.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Unique identifier of the entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The timestamp when the entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The timestamp when the entity was last updated.
    /// This is nullable because the entity may not have been updated yet.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// The timestamp when the entity was deleted.
    /// This is nullable because the entity may not have been deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Version of the entity for optimistic concurrency control.
    /// </summary>
    public long Version { get; set; } = 1;

    /// <summary>
    /// Indicates whether the entity has been marked as deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// The current status of the entity (e.g., Active, Inactive).
    /// </summary>
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    /// <summary>
    /// The user who created the entity. This is nullable because the creator may not always be tracked.
    /// </summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// The user who last updated the entity. This is nullable because the entity may not have been updated yet.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    /// The user who deleted the entity, if applicable. This is nullable.
    /// </summary>
    public Guid? DeletedBy { get; set; }

    /// <summary>
    /// The IP address from which the entity was created. This is nullable.
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// The list of IP addresses from which the entity was last updated. This is nullable and may contain multiple entries.
    /// </summary>
    public List<string>? UpdatedByIp { get; set; } = new List<string>();

    /// <summary>
    /// The IP address from which the entity was deleted. This is nullable.
    /// </summary>
    public string? DeletedByIp { get; set; }

    /// <summary>
    /// Updates the entity's properties, including the timestamp and the user who made the update.
    /// Increments the version for optimistic concurrency control.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user performing the update.
    /// </param>
    public void Update(Guid? userId)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = userId;
        Version++;
    }

    /// <summary>
    /// Marks the entity as deleted, setting the deletion timestamp and user.
    /// </summary>
    /// <param name="userId">
    /// The unique identifier of the user performing the deletion.
    /// </param>
    public void Delete(Guid? userId)
    {
        if (!IsDeleted)
        {
            DeletedAt = DateTimeOffset.UtcNow;
            DeletedBy = userId;
            IsDeleted = true;
        }
    }
}