namespace NextGenSoftware.OASIS.STARAPI.Client;

public sealed class StarAvatarProfile
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    /// <summary>Experience points (from avatar detail). Updated by get-current-avatar and add-xp responses.</summary>
    public int XP { get; init; }
    /// <summary>Quest currently tracked (from AvatarDetail). Restored after beam-in.</summary>
    public Guid? ActiveQuestId { get; init; }
    /// <summary>Objective currently active within the tracked quest (from AvatarDetail). Restored after beam-in.</summary>
    public Guid? ActiveObjectiveId { get; init; }
}
