/**
 * OASIS STAR API - star_sync_* implementation in C# (alternative to star_sync.c).
 * Export names match star_sync.h so games can link against the client DLL when OASIS_STAR_SYNC_IN_CLIENT is defined.
 */

using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NextGenSoftware.OASIS.STARAPI.Client;

internal enum PumpActionKind { Auth, Inventory, SendItem, UseItem, AddItemLog }

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void StarSyncDoneCallback(IntPtr userData);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void StarSyncAddItemLogCallback(IntPtr itemName, int success, IntPtr errorMessage, IntPtr userData);

internal sealed class PumpAction
{
    public PumpActionKind Kind;
    public IntPtr DoneCallback;
    public IntPtr DoneUserData;
    public IntPtr AddItemLogCallback;
    public IntPtr AddItemLogUserData;
    public string? ItemName;
    public int Success;
    public string? ErrorMessage;
    public IntPtr LocalItemsPtr;
    public int LocalCount;
    public int[]? SyncedOut;
}

public static unsafe class StarSyncExports
{
    private static readonly object AuthLock = new();
    private static readonly object InvLock = new();
    private static readonly object SendLock = new();
    private static readonly object UseLock = new();

    private static bool _initialized;
    private static int _authInProgress;
    private static int _authHasResult;
    private static int _authSuccess;
    private static string _authUsernameOut = string.Empty;
    private static string _authAvatarIdOut = string.Empty;
    private static string _authErrorMsg = string.Empty;
    private static string _authJwtOut = string.Empty;
    private static IntPtr _authOnDone;
    private static IntPtr _authOnDoneUser;

    private static int _invInProgress;
    private static int _invHasResult;
    private static star_item_list_t* _invList;
    private static int _invResult;
    private static string _invErrorMsg = string.Empty;
    private static string _invAddItemError = string.Empty;
    private static List<string> _invAddItemLogNames = new();
    private static int _invAddItemLogSuccess;
    private static IntPtr _invOnDone;
    private static IntPtr _invOnDoneUser;
    private static IntPtr _invLocalItemsPtr;
    private static int _invLocalCount;
    private static int[]? _invSyncedOut;

    private static int _sendInProgress;
    private static int _sendHasResult;
    private static int _sendSuccess;
    private static string _sendErrorMsg = string.Empty;
    private static IntPtr _sendOnDone;
    private static IntPtr _sendOnDoneUser;

    private static int _useInProgress;
    private static int _useHasResult;
    private static int _useSuccess;
    private static string _useErrorMsg = string.Empty;
    private static IntPtr _useOnDone;
    private static IntPtr _useOnDoneUser;

    private static IntPtr _addItemLogCb;
    private static IntPtr _addItemLogUser;

    private static readonly ConcurrentQueue<PumpAction> PumpQueue = new();

    private const int AddItemLogNamesMax = 32;

    [UnmanagedCallersOnly(EntryPoint = "star_sync_init", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncInit()
    {
        _initialized = true;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_cleanup", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncCleanup()
    {
        if (!_initialized) return;
        lock (InvLock)
        {
            if (_invList != null)
            {
                StarApiExports.FreeItemListInternal(_invList);
                _invList = null;
            }
            _invHasResult = 0;
        }
        while (PumpQueue.TryDequeue(out _)) { }
        _initialized = false;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_set_add_item_log_cb", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncSetAddItemLogCb(delegate* unmanaged[Cdecl]<byte*, int, byte*, void*, void> cb, void* userData)
    {
        _addItemLogCb = (IntPtr)cb;
        _addItemLogUser = (IntPtr)userData;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_pump", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncPump()
    {
        while (PumpQueue.TryDequeue(out var action))
        {
            try
            {
                switch (action.Kind)
                {
                    case PumpActionKind.Auth:
                    case PumpActionKind.Inventory:
                    case PumpActionKind.SendItem:
                    case PumpActionKind.UseItem:
                        if (action.Kind == PumpActionKind.Inventory && action.LocalItemsPtr != IntPtr.Zero && action.SyncedOut != null)
                            WriteBackSynced(action.LocalItemsPtr, action.LocalCount, action.SyncedOut);
                        if (action.DoneCallback != IntPtr.Zero)
                            ((StarSyncDoneCallback)Marshal.GetDelegateForFunctionPointer(action.DoneCallback, typeof(StarSyncDoneCallback))).Invoke(action.DoneUserData);
                        break;
                    case PumpActionKind.AddItemLog:
                        if (action.AddItemLogCallback != IntPtr.Zero)
                        {
                            nint itemNamePtr = 0, errorPtr = 0;
                            try
                            {
                                itemNamePtr = Marshal.StringToCoTaskMemUTF8(action.ItemName ?? string.Empty);
                                errorPtr = Marshal.StringToCoTaskMemUTF8(action.ErrorMessage ?? string.Empty);
                                ((StarSyncAddItemLogCallback)Marshal.GetDelegateForFunctionPointer(action.AddItemLogCallback, typeof(StarSyncAddItemLogCallback))).Invoke(itemNamePtr, action.Success, errorPtr, action.AddItemLogUserData);
                            }
                            finally
                            {
                                if (itemNamePtr != 0) Marshal.FreeCoTaskMem(itemNamePtr);
                                if (errorPtr != 0) Marshal.FreeCoTaskMem(errorPtr);
                            }
                        }
                        break;
                }
            }
            catch { /* avoid crashing native caller */ }
        }
    }

    private static void WriteBackSynced(IntPtr localItemsPtr, int count, int[] syncedOut)
    {
        const int sizeOfItem = 256 + 512 + 64 + 64 + 128 + 4; // star_sync_local_item_t
        const int syncedOffset = 256 + 512 + 64 + 64 + 128;
        for (int i = 0; i < count && i < syncedOut.Length; i++)
        {
            var p = (byte*)localItemsPtr + (i * sizeOfItem) + syncedOffset;
            *(int*)p = syncedOut[i];
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_auth_start", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncAuthStart(sbyte* username, sbyte* password, delegate* unmanaged[Cdecl]<void*, void> onDone, void* userData)
    {
        var user = StarApiExports.PtrToString(username) ?? string.Empty;
        var pass = StarApiExports.PtrToString(password) ?? string.Empty;
        lock (AuthLock)
        {
            if (_authInProgress != 0) return;
            _authHasResult = 0;
            _authOnDone = (IntPtr)onDone;
            _authOnDoneUser = (IntPtr)userData;
            _authInProgress = 1;
        }
        _ = Task.Run(() =>
        {
            int success = 0;
            string usernameOut = user;
            string avatarIdOut = string.Empty;
            string errorMsg = string.Empty;
            string jwtOut = string.Empty;
            var client = StarApiExports.GetClient();
            if (client != null)
            {
                var authResult = client.AuthenticateAsync(user, pass).GetAwaiter().GetResult();
                if (!authResult.IsError)
                {
                    jwtOut = client.GetCurrentJwt() ?? string.Empty;
                    var avatarResult = client.GetCurrentAvatarAsync().GetAwaiter().GetResult();
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        avatarIdOut = avatarResult.Result.Id.ToString();
                        success = 1;
                    }
                    else
                        errorMsg = avatarResult.Message ?? "Failed to get avatar ID.";
                }
                else
                    errorMsg = authResult.Message ?? "Authentication failed.";
            }
            else
                errorMsg = "Client is not initialized.";
            lock (AuthLock)
            {
                _authInProgress = 0;
                _authHasResult = 1;
                _authSuccess = success;
                _authUsernameOut = usernameOut;
                _authAvatarIdOut = avatarIdOut;
                _authErrorMsg = errorMsg;
                _authJwtOut = jwtOut;
                var cb = _authOnDone;
                var ud = _authOnDoneUser;
                _authOnDone = IntPtr.Zero;
                _authOnDoneUser = IntPtr.Zero;
                if (cb != IntPtr.Zero)
                    PumpQueue.Enqueue(new PumpAction { Kind = PumpActionKind.Auth, DoneCallback = cb, DoneUserData = ud });
            }
        });
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_auth_poll", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncAuthPoll()
    {
        lock (AuthLock)
        {
            if (_authInProgress != 0) return 0;
            if (_authHasResult != 0) return 1;
        }
        return -1;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_auth_get_result", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncAuthGetResult(int* successOut, sbyte* usernameBuf, nuint usernameSize, sbyte* avatarIdBuf, nuint avatarIdSize, sbyte* errorMsgBuf, nuint errorMsgSize)
    {
        lock (AuthLock)
        {
            if (_authHasResult == 0) return 0;
            if (successOut != null) *successOut = _authSuccess;
            WriteUtf8(usernameBuf, usernameSize, _authUsernameOut);
            WriteUtf8(avatarIdBuf, avatarIdSize, _authAvatarIdOut);
            WriteUtf8(errorMsgBuf, errorMsgSize, _authErrorMsg);
            _authHasResult = 0;
            return 1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_auth_get_result_jwt", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncAuthGetResultJwt(sbyte* jwtBuf, nuint jwtSize)
    {
        if (jwtBuf == null || jwtSize == 0) return;
        lock (AuthLock)
            WriteUtf8(jwtBuf, jwtSize, _authJwtOut);
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_auth_in_progress", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncAuthInProgress()
    {
        lock (AuthLock) return _authInProgress;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_inventory_start", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncInventoryStart(star_sync_local_item_t* localItems, int localCount, sbyte* defaultGameSource, delegate* unmanaged[Cdecl]<void*, void> onDone, void* onDoneUser)
    {
        var defaultSrc = StarApiExports.PtrToString(defaultGameSource) ?? string.Empty;
        lock (InvLock)
        {
            if (_invInProgress != 0) return;
            if (_invList != null)
            {
                StarApiExports.FreeItemListInternal(_invList);
                _invList = null;
            }
            _invHasResult = 0;
            _invLocalItemsPtr = (IntPtr)localItems;
            _invLocalCount = localCount < 0 ? 0 : localCount;
            _invOnDone = (IntPtr)onDone;
            _invOnDoneUser = (IntPtr)onDoneUser;
            _invInProgress = 1;
        }
        var localCopy = CopyLocalItems(localItems, localCount);
        _ = Task.Run(() =>
        {
            var client = StarApiExports.GetClient();
            string err = string.Empty;
            string addItemError = string.Empty;
            var loggedNames = new List<string>();
            star_item_list_t* list = null;
            int resultCode = (int)StarApiResultCode.ApiError;

            if (client != null && localCopy != null && localCopy.Count > 0 && !string.IsNullOrEmpty(defaultSrc))
            {
                foreach (var item in localCopy)
                {
                    if (item.Synced) continue;
                    string name = item.Name;
                    bool isStackEvent = name.Length >= 8 && name[^7] == '_' && name.Length >= 6 &&
                        name[^6..].All(c => c >= '0' && c <= '9');
                    string baseName = name;
                    if (isStackEvent && name.Length > 7)
                        baseName = name[..^7];
                    bool hasIt = client.HasItemAsync(isStackEvent ? baseName : name).GetAwaiter().GetResult().Result;
                    if (isStackEvent || !hasIt)
                    {
                        client.EnqueueAddItemJobOnly(
                            baseName,
                            item.Description,
                            item.GameSource.Length > 0 ? item.GameSource : defaultSrc,
                            item.ItemType.Length > 0 ? item.ItemType : "KeyItem",
                            string.IsNullOrEmpty(item.NftId) ? null : item.NftId,
                            1,
                            true);
                        if (loggedNames.Count < AddItemLogNamesMax)
                            loggedNames.Add(baseName);
                    }
                    item.Synced = true;
                }
                var flushResult = client.FlushAddItemJobsAsync(CancellationToken.None).GetAwaiter().GetResult();
                if (flushResult.IsError && string.IsNullOrEmpty(addItemError))
                    addItemError = flushResult.Message ?? "flush add_item jobs failed";
            }

            if (client != null)
            {
                var invResult = client.GetInventoryAsync(CancellationToken.None).GetAwaiter().GetResult();
                if (!invResult.IsError && invResult.Result != null)
                {
                    list = StarApiExports.BuildItemListFromInventory(invResult.Result);
                    resultCode = (int)StarApiResultCode.Success;
                }
                else
                {
                    err = invResult.Message ?? "Unknown error";
                    if (string.IsNullOrEmpty(err)) err = "Unknown error";
                }
            }
            else
                err = "Client is not initialized.";

            if (list == null && resultCode == (int)StarApiResultCode.Success)
            {
                resultCode = (int)StarApiResultCode.ApiError;
                err = "Inventory API returned success but no data";
            }

            int[]? syncedOut = localCopy?.Select(_ => 1).ToArray();
            lock (InvLock)
            {
                _invInProgress = 0;
                _invHasResult = 1;
                if (_invList != null)
                    StarApiExports.FreeItemListInternal(_invList);
                _invList = list;
                _invResult = resultCode;
                _invErrorMsg = string.IsNullOrEmpty(addItemError) ? err : addItemError;
                if (!string.IsNullOrEmpty(addItemError) && resultCode == (int)StarApiResultCode.Success)
                    _invResult = (int)StarApiResultCode.NotInitialized;
                _invAddItemLogNames = new List<string>(loggedNames);
                _invAddItemLogSuccess = string.IsNullOrEmpty(addItemError) ? 1 : 0;
                _invSyncedOut = syncedOut;
                var cb = _invOnDone;
                var ud = _invOnDoneUser;
                _invOnDone = IntPtr.Zero;
                _invOnDoneUser = IntPtr.Zero;
                if (cb != IntPtr.Zero)
                    PumpQueue.Enqueue(new PumpAction
                    {
                        Kind = PumpActionKind.Inventory,
                        DoneCallback = cb,
                        DoneUserData = ud,
                        LocalItemsPtr = _invLocalItemsPtr,
                        LocalCount = _invLocalCount,
                        SyncedOut = syncedOut
                    });
                _invLocalItemsPtr = IntPtr.Zero;
                _invLocalCount = 0;
                _invSyncedOut = null;
            }
            if (_addItemLogCb != IntPtr.Zero && loggedNames.Count > 0)
            {
                foreach (var n in loggedNames)
                    PumpQueue.Enqueue(new PumpAction
                    {
                        Kind = PumpActionKind.AddItemLog,
                        AddItemLogCallback = _addItemLogCb,
                        AddItemLogUserData = _addItemLogUser,
                        ItemName = n,
                        Success = string.IsNullOrEmpty(addItemError) ? 1 : 0,
                        ErrorMessage = addItemError
                    });
            }
        });
    }

    private static List<LocalItemCopy>? CopyLocalItems(star_sync_local_item_t* localItems, int localCount)
    {
        if (localItems == null || localCount <= 0) return null;
        var list = new List<LocalItemCopy>(localCount);
        for (int i = 0; i < localCount; i++)
        {
            ref var item = ref localItems[i];
            string name, desc, gs, it, nft;
            fixed (byte* np = item.name) { name = FixedByteToString(np, 256); }
            fixed (byte* dp = item.description) { desc = FixedByteToString(dp, 512); }
            fixed (byte* gp = item.game_source) { gs = FixedByteToString(gp, 64); }
            fixed (byte* ip = item.item_type) { it = FixedByteToString(ip, 64); }
            fixed (byte* np = item.nft_id) { nft = FixedByteToString(np, 128); }
            list.Add(new LocalItemCopy
            {
                Name = name,
                Description = desc,
                GameSource = gs,
                ItemType = it,
                NftId = nft,
                Synced = item.synced != 0
            });
        }
        return list;
    }

    private static string FixedByteToString(byte* ptr, int maxLen)
    {
        if (ptr == null) return string.Empty;
        int len = 0;
        while (len < maxLen && ptr[len] != 0) len++;
        return len == 0 ? string.Empty : Marshal.PtrToStringUTF8((IntPtr)ptr, len) ?? string.Empty;
    }

    private sealed class LocalItemCopy
    {
        public string Name = string.Empty;
        public string Description = string.Empty;
        public string GameSource = string.Empty;
        public string ItemType = string.Empty;
        public string NftId = string.Empty;
        public bool Synced;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_inventory_poll", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncInventoryPoll()
    {
        lock (InvLock)
        {
            if (_invInProgress != 0) return 0;
            if (_invHasResult != 0) return 1;
        }
        return -1;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_inventory_get_result", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncInventoryGetResult(star_item_list_t** listOut, int* resultOut, sbyte* errorMsgBuf, nuint errorMsgSize)
    {
        lock (InvLock)
        {
            if (_invHasResult == 0) return 0;
            if (listOut != null) *listOut = _invList;
            if (resultOut != null) *resultOut = _invResult;
            WriteUtf8(errorMsgBuf, errorMsgSize, _invErrorMsg);
            _invHasResult = 0;
            _invList = null;
            return 1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_inventory_clear_result", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncInventoryClearResult()
    {
        lock (InvLock)
        {
            if (_invList != null)
            {
                StarApiExports.FreeItemListInternal(_invList);
                _invList = null;
            }
            _invHasResult = 0;
            _invAddItemError = string.Empty;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_inventory_deliver_result", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncInventoryDeliverResult(star_item_list_t* list, int result, sbyte* errorMsg)
    {
        var err = StarApiExports.PtrToString(errorMsg) ?? string.Empty;
        lock (InvLock)
        {
            _invInProgress = 0;
            _invHasResult = 1;
            if (_invList != null)
                StarApiExports.FreeItemListInternal(_invList);
            _invList = list;
            _invResult = result;
            _invErrorMsg = err;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_inventory_in_progress", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncInventoryInProgress()
    {
        lock (InvLock) return _invInProgress;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_single_item", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncSingleItem(sbyte* name, sbyte* description, sbyte* gameSource, sbyte* itemType, sbyte* nftId)
    {
        var nameStr = StarApiExports.PtrToString(name);
        if (string.IsNullOrEmpty(nameStr)) return (int)StarApiResultCode.InvalidParam;
        var client = StarApiExports.GetClient();
        if (client == null) return (int)StarApiResultCode.NotInitialized;
        if (client.HasItemAsync(nameStr).GetAwaiter().GetResult().Result)
            return (int)StarApiResultCode.Success;
        client.EnqueueAddItemJobOnly(
            nameStr,
            StarApiExports.PtrToString(description) ?? string.Empty,
            StarApiExports.PtrToString(gameSource) ?? string.Empty,
            StarApiExports.PtrToString(itemType) ?? "KeyItem",
            StarApiExports.PtrToString(nftId),
            1,
            true);
        var result = client.FlushAddItemJobsAsync(CancellationToken.None).GetAwaiter().GetResult();
        int success = result.IsError ? 0 : 1;
        string err = result.IsError ? (result.Message ?? string.Empty) : string.Empty;
        if (_addItemLogCb != IntPtr.Zero)
            PumpQueue.Enqueue(new PumpAction
            {
                Kind = PumpActionKind.AddItemLog,
                AddItemLogCallback = _addItemLogCb,
                AddItemLogUserData = _addItemLogUser,
                ItemName = nameStr,
                Success = success,
                ErrorMessage = err
            });
        return result.IsError ? (int)StarApiResultCode.ApiError : (int)StarApiResultCode.Success;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_send_item_start", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncSendItemStart(sbyte* target, sbyte* itemName, int quantity, int toClan, sbyte* itemId, delegate* unmanaged[Cdecl]<void*, void> onDone, void* userData)
    {
        var targetStr = StarApiExports.PtrToString(target) ?? string.Empty;
        var itemNameStr = StarApiExports.PtrToString(itemName) ?? string.Empty;
        var itemIdStr = StarApiExports.PtrToString(itemId);
        Guid? guid = !string.IsNullOrWhiteSpace(itemIdStr) && Guid.TryParse(itemIdStr, out var g) && g != Guid.Empty ? g : null;
        int qty = quantity < 1 ? 1 : quantity;
        bool toClanBool = toClan != 0;
        lock (SendLock)
        {
            if (_sendInProgress != 0) return;
            _sendHasResult = 0;
            _sendOnDone = (IntPtr)onDone;
            _sendOnDoneUser = (IntPtr)userData;
            _sendInProgress = 1;
        }
        _ = Task.Run(() =>
        {
            int success = 0;
            string errorMsg = string.Empty;
            var client = StarApiExports.GetClient();
            if (client != null)
            {
                var result = toClanBool
                    ? client.SendItemToClanAsync(targetStr, itemNameStr, qty, guid, CancellationToken.None).GetAwaiter().GetResult()
                    : client.SendItemToAvatarAsync(targetStr, itemNameStr, qty, guid, CancellationToken.None).GetAwaiter().GetResult();
                success = result.IsError ? 0 : 1;
                errorMsg = result.Message ?? string.Empty;
            }
            else
                errorMsg = "Client is not initialized.";
            lock (SendLock)
            {
                _sendInProgress = 0;
                _sendHasResult = 1;
                _sendSuccess = success;
                _sendErrorMsg = errorMsg;
                var cb = _sendOnDone;
                var ud = _sendOnDoneUser;
                _sendOnDone = IntPtr.Zero;
                _sendOnDoneUser = IntPtr.Zero;
                if (cb != IntPtr.Zero)
                    PumpQueue.Enqueue(new PumpAction { Kind = PumpActionKind.SendItem, DoneCallback = cb, DoneUserData = ud });
            }
        });
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_send_item_poll", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncSendItemPoll()
    {
        lock (SendLock)
        {
            if (_sendInProgress != 0) return 0;
            if (_sendHasResult != 0) return 1;
        }
        return -1;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_send_item_get_result", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncSendItemGetResult(int* successOut, sbyte* errorMsgBuf, nuint errorMsgSize)
    {
        lock (SendLock)
        {
            if (_sendHasResult == 0) return 0;
            if (successOut != null) *successOut = _sendSuccess;
            WriteUtf8(errorMsgBuf, errorMsgSize, _sendErrorMsg);
            _sendHasResult = 0;
            return 1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_send_item_in_progress", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncSendItemInProgress()
    {
        lock (SendLock) return _sendInProgress;
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_use_item_start", CallConvs = [typeof(CallConvCdecl)])]
    public static void StarSyncUseItemStart(sbyte* itemName, sbyte* context, delegate* unmanaged[Cdecl]<void*, void> onDone, void* userData)
    {
        var itemNameStr = StarApiExports.PtrToString(itemName) ?? string.Empty;
        var contextStr = StarApiExports.PtrToString(context);
        if (string.IsNullOrEmpty(contextStr)) contextStr = "unknown";
        lock (UseLock)
        {
            if (_useInProgress != 0) return;
            _useHasResult = 0;
            _useOnDone = (IntPtr)onDone;
            _useOnDoneUser = (IntPtr)userData;
            _useInProgress = 1;
        }
        _ = Task.Run(() =>
        {
            int success = 0;
            string errorMsg = string.Empty;
            var client = StarApiExports.GetClient();
            if (client != null)
            {
                client.EnqueueUseItemJobOnly(itemNameStr, contextStr, 1);
                var result = client.FlushUseItemJobsAsync(CancellationToken.None).GetAwaiter().GetResult();
                success = result.IsError ? 0 : 1;
                errorMsg = result.Message ?? string.Empty;
            }
            else
                errorMsg = "Client is not initialized.";
            lock (UseLock)
            {
                _useInProgress = 0;
                _useHasResult = 1;
                _useSuccess = success;
                _useErrorMsg = errorMsg;
                var cb = _useOnDone;
                var ud = _useOnDoneUser;
                _useOnDone = IntPtr.Zero;
                _useOnDoneUser = IntPtr.Zero;
                if (cb != IntPtr.Zero)
                    PumpQueue.Enqueue(new PumpAction { Kind = PumpActionKind.UseItem, DoneCallback = cb, DoneUserData = ud });
            }
        });
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_use_item_get_result", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncUseItemGetResult(int* successOut, sbyte* errorMsgBuf, nuint errorMsgSize)
    {
        lock (UseLock)
        {
            if (_useHasResult == 0) return 0;
            if (successOut != null) *successOut = _useSuccess;
            WriteUtf8(errorMsgBuf, errorMsgSize, _useErrorMsg);
            _useHasResult = 0;
            return 1;
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "star_sync_use_item_in_progress", CallConvs = [typeof(CallConvCdecl)])]
    public static int StarSyncUseItemInProgress()
    {
        lock (UseLock) return _useInProgress;
    }

    private static void WriteUtf8(sbyte* dest, nuint size, string value)
    {
        if (dest == null || size == 0) return;
        var bytes = System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty);
        int copyLen = (int)Math.Min((nuint)bytes.Length, size - 1);
        if (copyLen > 0)
            new ReadOnlySpan<byte>(bytes, 0, copyLen).CopyTo(new Span<byte>((byte*)dest, copyLen));
        ((byte*)dest)[copyLen] = 0;
    }
}
