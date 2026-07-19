namespace NextGenSoftware.OASIS.STARAPI.Client;

public sealed class StarApiConfig
{
    // WEB5 STAR API base URI (quests, STAR status/ignite, optional WEB5 auth sync, NFT activate, etc.).
    public string Web5StarApiBaseUrl { get; init; } = string.Empty;
    // WEB4 OASIS API base URI (avatar auth, profile, inventory, XP, active quest, send-item, NFT mint). Required for those features when using the native client.
    public string? Web4OasisApiBaseUrl { get; init; }
    public string? ApiKey { get; init; }
    public string? AvatarId { get; init; }
    /// <summary>HTTP request timeout in seconds. Default 60 so slow endpoints (e.g. quests all-for-avatar with many quests) can complete. Increase if you see timeout errors.</summary>
    public int TimeoutSeconds { get; init; } = 60;
    /// <summary>After successful progress POST: merge into local cache (default) or refetch all quests. Native: star_api_set_quest_progress_cache_refresh; ODOOM: oasisstar.json quest_progress_refresh.</summary>
    public QuestProgressCacheRefreshMode QuestProgressCacheRefresh { get; init; } = QuestProgressCacheRefreshMode.ClientCacheMerge;
    /// <summary>Which executable is running (e.g. ODOOM vs OQUAKE). Used with quest/objective <c>GameSource</c> to pick the correct requirement row for the tracker. Set from native <c>client_game_source</c>; do not hardcode in shared client logic.</summary>
    public string? ClientGameSource { get; init; }
    /// <summary><see cref="StarApiTransport.Remote"/> = HTTP client (default). <see cref="StarApiTransport.Native"/> is only supported when star_api is built/loaded with the full OASIS stack (not the shipped NativeAOT DLL).</summary>
    public StarApiTransport Transport { get; init; } = StarApiTransport.Remote;
    /// <summary>Optional path to OASIS_DNA.json for native transport (consumed by a native host when implemented).</summary>
    public string? OasisDnaPath { get; init; }
}
