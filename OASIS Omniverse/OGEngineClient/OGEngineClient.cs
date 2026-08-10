using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Contracts;

namespace NextGenSoftware.OASIS.STARAPI.Client;
public sealed partial class OGEngineClient : IDisposable
{
    /// <summary>Serializes WEB4/WEB5 beam-in so overlapping calls (e.g. init + console) do not stack multiple full logins and duplicate quest fetches.</summary>
    private static readonly SemaphoreSlim AuthenticateSingleFlight = new(1, 1);

    private readonly object _stateLock = new();
    private readonly object _inventoryCacheLock = new();
    private readonly object _questsCacheLock = new();
    /// <summary>Only one refresh-token POST at a time; parallel RestoreSession + RefreshAvatarProfile GETs caused double-refresh, bad parse on 2nd body, and ClearSessionToken wiping a valid session.</summary>
    private readonly SemaphoreSlim _tokenRefreshSemaphore = new(1, 1);
    /// <summary>Non-null while <see cref="RestoreSessionAsync"/> is queued/running (proactive refresh + GET). Profile refresh awaits this so it does not hit 401 with a stale JWT in parallel.</summary>
    private Task? _restoreSessionInFlight;

    /// <summary>Cached serialized full quest list (game format: "Q\tid\tname\t...\n"). Returned as-is by TryGetQuestsCache for ogengine_get_quests_string so the game thread never blocks or re-serializes.</summary>
    private string? _questsCacheString;
    /// <summary>Cached structured quest list from last successful load. Used to filter and re-serialize: top-level only (TryGetTopLevelQuestsCache), objectives (TryGetQuestObjectivesCache), sub-quests (TryGetQuestSubQuestsCache), prereqs (TryGetQuestPrereqsCache). We need both: the string for fast full-list return; the list for filtering by ParentQuestId/Objectives without re-parsing.</summary>
    private List<StarQuestInfo>? _cachedQuestList;
    /// <summary>True while a background quest refresh is running; prevents multiple concurrent refreshes.</summary>
    private bool _questsRefreshInProgress;
    /// <summary>When true, another <see cref="RequestQuestCacheRefreshInBackground"/> was requested while a refresh was in flight; run one more fetch after the current one completes (coalesces rapid progress POSTs).</summary>
    private bool _questsRefreshPending;
    /// <summary>Last (total, top) logged for TryGetTopLevelQuestsCache; log only when changed to avoid spam.</summary>
    private (int total, int top) _questsFilterLastLogTop = (0, 0);
    /// <summary>Last (parentId, count) logged for objectives; log only when changed.</summary>
    private (string id, int count) _questsFilterLastLogObjectives = ("", -1);
    /// <summary>Last (parentId, count) logged for sub-quests; log only when changed.</summary>
    private (string id, int count) _questsFilterLastLogSubQuests = ("", -1);
    /// <summary>Last (questId, count) logged for prereqs; log only when changed.</summary>
    private (string id, int count) _questsFilterLastLogPrereqs = ("", -1);

    /// <summary>1 while native quest list popup is open. No quest progress POST, no client merge, and no replacing the quest cache from GET all-for-avatar while set (game cannot meaningfully earn progress with the list open).</summary>
    private int _questUiPopupOpen;

    /// <summary>Quest ids we're currently fetching for on-demand objectives; avoids duplicate concurrent fetches. Result is merged into _cachedQuestList.</summary>
    private readonly HashSet<string> _questObjectivesHydrating = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Incremented when objectives are merged into the cache (on-demand fetch). UI can poll this and re-call get_quest_objectives_string when it changes to refresh the list.</summary>
    private int _questObjectivesCacheVersion;
    /// <summary>Local cache of last loaded inventory. GetInventory/HasItem/UseItem use this first and only hit the API when cache is empty or item not found (for has_item).</summary>
    private List<StarItem>? _cachedInventory;
    /// <summary>Single-flight fetch: when cache is null, only one HTTP get_inventory runs; other callers wait on this task.</summary>
    private Task<OASISResult<List<StarItem>>>? _inventoryFetchTask;

    /// <summary>Pickup delta array: one entry per item type (name -> pending qty to add). Games call QueueAddItem; we merge here and return API + pending in GetInventory. Worker flushes to API in background.</summary>
    private readonly Dictionary<string, LocalPendingEntry> _localPending = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _localPendingLock = new();

    private HttpClient? _httpClient;
    private bool _initialized;
    // WEB5 STAR API base URI.
    private string _baseApiUrl = string.Empty;
    // WEB4 OASIS API base URI.
    private string _oasisBaseUrl = string.Empty;

    /// <summary>WEB4 endpoint STAR WebAPI mapped from GET /api/avatar/current (<c>AvatarController.GetCurrentAvatar</c>).</summary>
    private const string Web4GetLoggedInAvatarWithXpPath = "/api/avatar/get-logged-in-avatar-with-xp";

    /// <summary>Avatar profile, inventory, XP, active quest, and send-item calls go to WEB4 directly so client <c>oasis_api_url</c> matches server behavior (no separate Web4OasisApiBaseUrl on WEB5).</summary>
    private bool TryGetWeb4BaseTrimmed(out string web4Base, out string missingMessage)
    {
        lock (_stateLock)
        {
            if (string.IsNullOrWhiteSpace(_oasisBaseUrl))
            {
                web4Base = string.Empty;
                missingMessage = "WEB4 OASIS API base URL is not set. Set oasis_api_url in oasisstar.json, Web4OasisApiBaseUrl in STAR init, or OASIS_WEB4_API_BASE_URL (required for avatar profile and inventory).";
                return false;
            }

            web4Base = _oasisBaseUrl.TrimEnd('/');
            missingMessage = string.Empty;
            return true;
        }
    }
    private string? _jwtToken;
    private string? _refreshToken;
    private string? _avatarId;
    /// <summary>Username of the currently logged-in avatar (set on auth and from GET avatar/current). Used for session persistence and display.</summary>
    private string? _loggedInUsername;
    private Guid? _cachedActiveQuestId;
    private Guid? _cachedActiveObjectiveId;
    /// <summary>Last <c>gameSource</c> on a successful quest progress POST (tie-breaker after client + objective + quest metadata).</summary>
    private string? _questLastProgressGameSource;
    /// <summary>Runtime client id from <see cref="OGEngineConfig.ClientGameSource"/> (e.g. ODOOM, OQUAKE) — which game binary is running.</summary>
    private string? _questClientGameSource;
    /// <summary>When true, a save happened after the current GET avatar/current was started; do not let the GET response overwrite quest/objective cache.</summary>
    private bool _questTrackerSavedSinceLastGet;
    /// <summary>Set true when we clear session due to 401 (expired JWT and refresh failed). Prevents hammering the API with auth-required requests until user re-logs in.</summary>
    private bool _sessionExpiredCleared;
    private string _lastError = string.Empty;
    private StarApiCallback? _callback;
    private object? _callbackUserData;
    private readonly ConcurrentQueue<PendingAddItemJob> _pendingAddItemJobs = new();
    private readonly ConcurrentQueue<PendingPickupWithMintJob> _pendingPickupWithMint = new();
    private readonly ConcurrentQueue<PendingMonsterKillJob> _pendingMonsterKill = new();
    private readonly ConcurrentQueue<PendingUseItemJob> _pendingUseItemJobs = new();
    private readonly ConcurrentQueue<PendingQuestObjectiveJob> _pendingQuestObjectiveJobs = new();
    private readonly SemaphoreSlim _addItemSignal = new(0);
    private readonly SemaphoreSlim _useItemSignal = new(0);
    private readonly SemaphoreSlim _questObjectiveSignal = new(0);
    private readonly object _jobLock = new();
    private int _activeAddItemJobs;
    private readonly object _lastMintLock = new();
    private string? _lastMintItemName;
    private string? _lastMintNftId;
    private string? _lastMintHash;
    private int _activeUseItemJobs;
    private int _activeQuestObjectiveJobs;
    /// <summary>Last known avatar XP (from get-current-avatar or add-xp response). Used by ogengine_get_avatar_xp.</summary>
    private int _cachedAvatarXp;
    /// <summary>Last known avatar karma score (from get-current-avatar). Used by ogengine_get_avatar_karma.</summary>
    private long _cachedAvatarKarma;
    /// <summary>Pending XP to add (queued by ogengine_queue_add_xp). Flushed with add-item worker.</summary>
    private int _pendingXp;
    private CancellationTokenSource? _jobCts;
    private Task? _jobWorker;
    private CancellationTokenSource? _useItemJobCts;
    private Task? _useItemJobWorker;
    private CancellationTokenSource? _questObjectiveJobCts;
    private Task? _questObjectiveJobWorker;
    /// <summary>Generic background queue for any async API call so UI/game thread never blocks. One worker processes jobs in order.</summary>
    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _genericBackgroundQueue = new();
    private readonly SemaphoreSlim _genericBackgroundSignal = new(0);
    private CancellationTokenSource? _genericBackgroundCts;
    private Task? _genericBackgroundWorker;
    private readonly object _genericBackgroundLock = new();

    /// <summary>Dedicated workers so auth, profile, inventory, and quests don't block each other (audit: avoid single bottleneck).</summary>
    private enum DedicatedWorker { AuthSession, Profile, Inventory, Quests }

    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _authSessionQueue = new();
    private readonly SemaphoreSlim _authSessionSignal = new(0);
    private CancellationTokenSource? _authSessionCts;
    private Task? _authSessionWorker;
    private readonly object _authSessionLock = new();

    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _profileQueue = new();
    private readonly SemaphoreSlim _profileSignal = new(0);
    private CancellationTokenSource? _profileCts;
    private Task? _profileWorker;
    private readonly object _profileLock = new();

    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _inventoryQueue = new();
    private readonly SemaphoreSlim _inventorySignal = new(0);
    private CancellationTokenSource? _inventoryCts;
    private Task? _inventoryWorker;
    private readonly object _inventoryLock = new();

    private readonly ConcurrentQueue<Func<CancellationToken, Task>> _questsQueue = new();
    private readonly SemaphoreSlim _questsSignal = new(0);
    private CancellationTokenSource? _questsCts;
    private Task? _questsWorker;
    private readonly object _questsLock = new();

    /// <summary>After successful /progress POST: merge locally or GET full quest list.</summary>
    private QuestProgressCacheRefreshMode _questProgressCacheRefresh = QuestProgressCacheRefreshMode.ClientCacheMerge;

    /// <summary>Max retries for transient network errors (audit: bounded retry with backoff).</summary>
    private const int HttpRetryMaxAttempts = 3;
    /// <summary>Backoff delays in ms for retries (200, 400, 800).</summary>
    private static readonly int[] HttpRetryDelayMs = { 200, 400, 800 };

    /// <summary>Default JsonDocument max depth is 64; STAR quest/holon JSON (starnetdna, metaData, etc.) exceeds that and throws, which broke Linux 406 success detection and envelope parsing.</summary>
    private OGEngineConfig? _config;
    private static readonly JsonDocumentOptions DeepJsonDocumentOptions = new() { MaxDepth = 1024 };

    public int AddItemBatchSize { get; set; } = 32;
    public TimeSpan AddItemBatchWindow { get; set; } = TimeSpan.FromMilliseconds(75);
    public int UseItemBatchSize { get; set; } = 32;
    public TimeSpan UseItemBatchWindow { get; set; } = TimeSpan.FromMilliseconds(50);
    public int QuestObjectiveBatchSize { get; set; } = 32;
    public TimeSpan QuestObjectiveBatchWindow { get; set; } = TimeSpan.FromMilliseconds(50);


    private sealed class AvatarAuthResponse
    {
        public Guid Id { get; set; }
        public string? JwtToken { get; set; }
        public string? RefreshToken { get; set; }
    }

    private sealed class AvatarInfo
    {
        public Guid Id { get; set; }
    }

    private sealed class InventoryItemResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public Dictionary<string, JsonElement>? MetaData { get; set; }
        public int Quantity { get; set; } = 1;
        /// <summary>From API / InventoryItem holon.</summary>
        public string? GameSource { get; set; }
        /// <summary>From API / InventoryItem holon.</summary>
        public string? ItemType { get; set; }
        /// <summary>NFT ID when item is linked to NFTHolon (from MetaData or root). Persists so [NFT] prefix shows in Quake/Doom after reload.</summary>
        public string? NftId { get; set; }
    }

    /// <summary>One row per item type: accumulated delta until flushed to API. Used by GetInventory merge and background flush.</summary>
    private sealed class LocalPendingEntry
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string GameSource { get; set; } = string.Empty;
        public string ItemType { get; set; } = "KeyItem";
        public int Quantity { get; set; }
        /// <summary>Set when mint completes (pickup-with-mint) so merge shows [NFT] prefix in Quake/Doom overlay.</summary>
        public string? NftId { get; set; }
    }
}
