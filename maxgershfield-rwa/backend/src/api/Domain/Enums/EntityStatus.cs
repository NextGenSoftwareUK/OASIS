namespace Domain.Enums;

/// <summary>
/// Represents the possible statuses of an entity in the system.
/// This enum is used to track the state of an entity, such as whether it is active, inactive, or deleted.
/// </summary>
public enum EntityStatus
{
    /// <summary>
    /// The entity is currently active and in use within the system.
    /// </summary>
    Active,

    /// <summary>
    /// The entity is currently inactive and not in use.
    /// It may be temporarily disabled but not deleted.
    /// </summary>
    Inactive,

    /// <summary>
    /// The entity has been deleted and is no longer available in the system.
    /// </summary>
    Deleted
}
