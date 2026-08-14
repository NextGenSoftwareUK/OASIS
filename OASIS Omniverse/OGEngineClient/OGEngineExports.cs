using System.Buffers;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STARAPI.Client;
public static unsafe partial class OGEngineExports
{
    private static readonly object Sync = new();
    private static readonly object NativeStateLock = new();
    private static readonly object BackgroundErrorLock = new();
    private static string? _lastBackgroundError;
    private static readonly ConcurrentQueue<string> _consoleLogQueue = new();
    private const int MaxConsoleLogMessages = 64;
    /// <summary>Pending cross-game events from quest progress/start responses (ShowNarration, PlayAudio, PlayVideo, OpenWebsite, UnlockPortal). Consumed by ogengine_poll_cross_game_event.</summary>
    private static readonly ConcurrentQueue<string> _pendingCrossGameEvents = new();
    private const int MaxPendingCrossGameEvents = 64;
    /// <summary>Pending inventory item GUIDs to grant the avatar on objective/quest completion. Consumed by ogengine_poll_inventory_grant.</summary>
    private static readonly ConcurrentQueue<string> _pendingInventoryGrants = new();
    /// <summary>Cap UTF-8 size for queued console lines so ogengine_consume_console_log never hits GetBytes buffer-too-small (native crash).</summary>
    private const int MaxConsoleLogUtf8Bytes = 1536;
    /// <summary>Per-line cap for ogengine.log (file only). Large enough for API bodies; avoids multi-megabyte single-line OOM.</summary>
    private const int MaxStarApiFileLineUtf8Bytes = 1_048_576;
    private static OGEngineClient? _client;
    private static byte* _lastError;
    private static delegate* unmanaged[Cdecl]<int, void*, void> _callback;
    private static void* _callbackUserData;
    /// <summary>Optional: (result, operation_type, user_data). When set, profile refresh uses this instead of _callback so the game can filter by operation_type.</summary>
    private static delegate* unmanaged[Cdecl]<int, int, void*, void> _operationCallback;
    private static void* _operationCallbackUserData;
    private static volatile int _starDebug;
    private static int StarApiGetQuestsStringLastLoggedToCopy = -1;
    private static bool _topLevelQuestsLastLoggedLoading;

    /// <summary>Whether STAR debug logging is on (games set via ogengine_set_debug). When true, quest API and other requests log URI and response to file and console.</summary>
    internal static bool GetStarDebug() => _starDebug != 0;
}
