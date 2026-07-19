namespace NextGenSoftware.OASIS.STARAPI.Client;

/// <summary>How to refresh the in-memory quest cache after a successful POST /quests/{id}/progress.</summary>
public enum QuestProgressCacheRefreshMode
{
    /// <summary>Apply the same deltas to cached objective progress dictionaries; re-serialize; notify native (no GET).</summary>
    ClientCacheMerge = 0,
    /// <summary>GET all-for-avatar/game and replace cache (authoritative; more network load).</summary>
    FullServerRefresh = 1
}
