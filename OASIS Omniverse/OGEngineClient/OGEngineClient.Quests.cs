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
public sealed partial class OGEngineClient
{
    private static string EscapeForQuestLine(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c == '\t' || c == '\n' || c == '\r') sb.Append(' ');
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Mint an NFT for a monster kill (any monster, including bosses) via WEB4 OASIS API. Returns NFT ID and optional tx hash. provider: same as nft_provider in oasisstar.json (e.g. SolanaOASIS); null/empty = SolanaOASIS. SPL used when provider is SolanaOASIS, else ERC1155.</summary>
    public async Task<OASISResult<(string NftId, string? Hash)>> CreateMonsterNftAsync(string monsterName, string? description, string? gameSource, string? monsterStatsJson, string? provider = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<(string NftId, string? Hash)>("Client is not initialized.", StarApiResultCode.NotInitialized);

        string oasisUrl;
        lock (_stateLock) { oasisUrl = _oasisBaseUrl ?? string.Empty; }
        if (string.IsNullOrWhiteSpace(oasisUrl))
            return FailAndCallback<(string NftId, string? Hash)>("WEB4 OASIS API base URL is not set. Set OASIS_WEB4_API_BASE_URL or Web4OasisApiBaseUrl (e.g. http://localhost:5555).", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(monsterName))
            return FailAndCallback<(string NftId, string? Hash)>("Monster name is required.", StarApiResultCode.InvalidParam);

        JsonElement monsterStatsElement;
        try
        {
            var statsJson = string.IsNullOrWhiteSpace(monsterStatsJson) ? "{}" : monsterStatsJson;
            using var statsDoc = JsonDocument.Parse(statsJson);
            monsterStatsElement = statsDoc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            return FailAndCallback<(string NftId, string? Hash)>($"monsterStatsJson is not valid JSON: {ex.Message}", StarApiResultCode.InvalidParam, ex);
        }

        string? sendToAvatarAfterMintingId = null;
        lock (_stateLock)
        {
            if (Guid.TryParse(_avatarId, out var avatarGuid) && avatarGuid != Guid.Empty)
                sendToAvatarAfterMintingId = avatarGuid.ToString();
        }

        var onChainProvider = string.IsNullOrWhiteSpace(provider) ? "SolanaOASIS" : provider;
        var nftStandardType = string.Equals(onChainProvider, "SolanaOASIS", StringComparison.OrdinalIgnoreCase) ? "SPL" : "ERC1155";

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("title", monsterName);
            writer.WriteString("description", string.IsNullOrWhiteSpace(description) ? "Monster from game" : description);
            writer.WriteString("symbol", "BOSS");
            writer.WriteString("image", "AQ==");
            writer.WriteString("imageUrl", "https://oasisweb4.one/images/star/default-boss.png");
            writer.WriteString("thumbnail", "AQ==");
            writer.WriteString("thumbnailUrl", "https://oasisweb4.one/images/star/default-boss-thumb.png");
            writer.WriteString("memoText", "Minted by WEB4 OASIS API");
            writer.WriteNumber("numberToMint", 1);
            writer.WriteBoolean("storeNFTMetaDataOnChain", false);
            writer.WriteString("offChainProvider", "MongoDBOASIS");
            writer.WriteString("onChainProvider", onChainProvider);
            writer.WriteString("nftOffChainMetaType", "ExternalJSONURL");
            writer.WriteString("JSONMetaDataURL", "https://oasisweb4.one/metadata/star/default-boss.json");
            writer.WriteString("nftStandardType", nftStandardType);
            if (!string.IsNullOrWhiteSpace(sendToAvatarAfterMintingId))
                writer.WriteString("sendToAvatarAfterMintingId", sendToAvatarAfterMintingId);
            writer.WritePropertyName("metaData");
            writer.WriteStartObject();
            writer.WriteString("GameSource", string.IsNullOrWhiteSpace(gameSource) ? "Unknown" : gameSource);
            writer.WritePropertyName("BossStats");
            monsterStatsElement.WriteTo(writer);
            writer.WriteString("DefeatedAt", DateTime.UtcNow.ToString("O"));
            writer.WriteBoolean("Deployable", true);
            writer.WriteEndObject();
            writer.WriteBoolean("waitTillNFTMinted", false);
            writer.WriteNumber("waitForNFTToMintInSeconds", 10);
            writer.WriteNumber("attemptToMintEveryXSeconds", 1);
            writer.WriteBoolean("waitTillNFTSent", false);
            writer.WriteNumber("waitForNFTToSendInSeconds", 30);
            writer.WriteNumber("attemptToSendEveryXSeconds", 1);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_oasisBaseUrl}/api/nft/mint-nft", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
            {
                InvokeCallback(StarApiResultCode.Success);
                return Success((warningMintId!, (string?)null), StarApiResultCode.Success, $"Boss NFT created with warnings: {response.Message}");
            }

            return FailAndCallback<(string NftId, string? Hash)>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
            {
                InvokeCallback(StarApiResultCode.Success);
                return Success((warningMintId!, (string?)null), StarApiResultCode.Success, $"Boss NFT created with warnings: {parseErrorMessage}");
            }

            return FailAndCallback<(string NftId, string? Hash)>(parseErrorMessage, parseErrorCode);
        }

        var nftId = ParseIdAsString(resultElement);
        if (string.IsNullOrWhiteSpace(nftId))
            return FailAndCallback<(string NftId, string? Hash)>("API did not return an NFT ID.", StarApiResultCode.ApiError);

        var hash = GetMintResponseHash(resultElement, response.Result);
        InvokeCallback(StarApiResultCode.Success);
        return Success((nftId, string.IsNullOrWhiteSpace(hash) ? null : hash), StarApiResultCode.Success, "Monster NFT created successfully.");
    }

    /// <summary>Run create-monster-NFT on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<(string NftId, string? Hash)>> QueueCreateMonsterNftAsync(string monsterName, string? description, string? gameSource, string? monsterStatsJson, string? provider = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => CreateMonsterNftAsync(monsterName, description, gameSource, monsterStatsJson, provider, ct), cancellationToken);

    /// <summary>Mint an NFT for an inventory item (creates NFTHolon on WEB4). Returns NFT ID and optional hash (tx/signature). Default provider: SolanaOASIS. Same as nft_provider in oasisstar.json. sendToAddressAfterMinting: optional wallet address to send the minted NFT to (from oasisstar.json SendToAddressAfterMinting).</summary>
    public async Task<OASISResult<(string NftId, string? Hash)>> MintInventoryItemNftAsync(string itemName, string? description, string gameSource, string itemType = "KeyItem", string? provider = null, string? sendToAddressAfterMinting = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<(string NftId, string? Hash)>("Client is not initialized.", StarApiResultCode.NotInitialized);

        string oasisUrl;
        lock (_stateLock) { oasisUrl = _oasisBaseUrl ?? string.Empty; }
        if (string.IsNullOrWhiteSpace(oasisUrl))
            return FailAndCallback<(string NftId, string? Hash)>("WEB4 OASIS API base URL is not set. Set OASIS_WEB4_API_BASE_URL or Web4OasisApiBaseUrl (e.g. http://localhost:5555).", StarApiResultCode.InvalidParam);

        if (string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<(string NftId, string? Hash)>("Item name is required.", StarApiResultCode.InvalidParam);

        var onChainProvider = string.IsNullOrWhiteSpace(provider) ? "SolanaOASIS" : provider;
        string? sendToAvatarAfterMintingId = null;
        lock (_stateLock)
        {
            if (Guid.TryParse(_avatarId, out var avatarGuid) && avatarGuid != Guid.Empty)
                sendToAvatarAfterMintingId = avatarGuid.ToString();
        }

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("title", itemName);
            writer.WriteString("description", string.IsNullOrWhiteSpace(description) ? $"Inventory item: {itemName}" : description);
            writer.WriteString("symbol", "STARITEM");
            writer.WriteString("image", "AQ==");
            writer.WriteString("imageUrl", "https://oasisweb4.one/images/star/default-item.png");
            writer.WriteString("thumbnail", "AQ==");
            writer.WriteString("thumbnailUrl", "https://oasisweb4.one/images/star/default-item-thumb.png");
            writer.WriteString("memoText", "Minted by WEB4 OASIS API (inventory item)");
            writer.WriteNumber("numberToMint", 1);
            writer.WriteBoolean("storeNFTMetaDataOnChain", false);
            writer.WriteString("offChainProvider", "MongoDBOASIS");
            writer.WriteString("onChainProvider", onChainProvider);
            writer.WriteString("nftOffChainMetaType", "ExternalJSONURL");
            writer.WriteString("JSONMetaDataURL", "https://oasisweb4.one/metadata/star/default-item.json");
            writer.WriteString("nftStandardType", string.Equals(onChainProvider, "SolanaOASIS", StringComparison.OrdinalIgnoreCase) ? "SPL" : "ERC1155");
            if (!string.IsNullOrWhiteSpace(sendToAvatarAfterMintingId))
                writer.WriteString("sendToAvatarAfterMintingId", sendToAvatarAfterMintingId);
            if (!string.IsNullOrWhiteSpace(sendToAddressAfterMinting))
                writer.WriteString("sendToAddressAfterMinting", sendToAddressAfterMinting);
            writer.WritePropertyName("metaData");
            writer.WriteStartObject();
            writer.WriteString("GameSource", string.IsNullOrWhiteSpace(gameSource) ? "Unknown" : gameSource);
            writer.WriteString("ItemType", string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType);
            writer.WriteString("ItemName", itemName);
            writer.WriteString("MintedAt", DateTime.UtcNow.ToString("O"));
            writer.WriteEndObject();
            writer.WriteBoolean("waitTillNFTMinted", false);
            writer.WriteNumber("waitForNFTToMintInSeconds", 10);
            writer.WriteBoolean("waitTillNFTSent", false);
            writer.WriteNumber("waitForNFTToSendInSeconds", 30);
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_oasisBaseUrl}/api/nft/mint-nft", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
                return Success((warningMintId!, (string?)null), StarApiResultCode.Success, $"Inventory item NFT created with warnings: {response.Message}");
            return FailAndCallback<(string NftId, string? Hash)>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            if (TryExtractTopLevelResultId(response.Result, out var warningMintId))
                return Success((warningMintId!, (string?)null), StarApiResultCode.Success, $"Inventory item NFT created with warnings: {parseErrorMessage}");
            return FailAndCallback<(string NftId, string? Hash)>(parseErrorMessage, parseErrorCode);
        }

        var nftId = ParseIdAsString(resultElement);
        if (string.IsNullOrWhiteSpace(nftId))
            return FailAndCallback<(string NftId, string? Hash)>("API did not return an NFT ID.", StarApiResultCode.ApiError);

        var hash = GetMintResponseHash(resultElement, response.Result);

        InvokeCallback(StarApiResultCode.Success);
        return Success((nftId, string.IsNullOrWhiteSpace(hash) ? null : hash), StarApiResultCode.Success, "Inventory item NFT minted successfully.");
    }

    /// <summary>Run mint-inventory-item-NFT on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<(string NftId, string? Hash)>> QueueMintInventoryItemNftAsync(string itemName, string? description, string gameSource, string itemType = "KeyItem", string? provider = null, string? sendToAddressAfterMinting = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => MintInventoryItemNftAsync(itemName, description, gameSource, itemType, provider, sendToAddressAfterMinting, ct), cancellationToken);

    public async Task<OASISResult<bool>> DeployBossNftAsync(string nftId, string targetGame, string? location = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);

        if (string.IsNullOrWhiteSpace(nftId) || string.IsNullOrWhiteSpace(targetGame))
            return FailAndCallback<bool>("NFT ID and target game are required.", StarApiResultCode.InvalidParam);

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("nftId", nftId);
            writer.WriteString("targetGame", targetGame);
            writer.WriteString("location", string.IsNullOrWhiteSpace(location) ? "default" : location);
            writer.WriteString("deployedAt", DateTime.UtcNow.ToString("O"));
            writer.WriteEndObject();
        });

        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/nfts/{nftId}/activate", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
                return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Boss NFT deployed successfully.");
    }

    /// <summary>Run deploy-boss-NFT on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueDeployBossNftAsync(string nftId, string targetGame, string? location = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => DeployBossNftAsync(nftId, targetGame, location, ct), cancellationToken);

    public async Task<OASISResult<List<StarNftInfo>>> GetNftCollectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<List<StarNftInfo>>("Client is not initialized.", StarApiResultCode.NotInitialized);

        var avatarIdResult = await EnsureAvatarIdAsync(cancellationToken).ConfigureAwait(false);
        if (avatarIdResult.IsError || string.IsNullOrWhiteSpace(avatarIdResult.Result))
            return FailAndCallback<List<StarNftInfo>>(avatarIdResult.Message, ParseCode(avatarIdResult.ErrorCode, StarApiResultCode.ApiError), avatarIdResult.Exception);

        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/nfts/load-all-for-avatar", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
            return FailAndCallback<List<StarNftInfo>>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<List<StarNftInfo>>(parseErrorMessage, parseErrorCode);

        var nfts = ParseNftInfos(resultElement);
        InvokeCallback(StarApiResultCode.Success);
        return Success(nfts, StarApiResultCode.Success, $"Loaded {nfts.Count} NFT(s).");
    }

    /// <summary>Run get-NFT-collection on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<List<StarNftInfo>>> QueueGetNftCollectionAsync(CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => GetNftCollectionAsync(ct), cancellationToken);

    /// <summary>Sends an item from the current avatar's inventory to another avatar. Target is username or avatar Id. Optionally pass itemId (Guid) to send that specific item. Works for all items (STAR and local).</summary>
    public async Task<OASISResult<bool>> SendItemToAvatarAsync(string targetUsernameOrAvatarId, string itemName, int quantity = 1, Guid? itemId = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(targetUsernameOrAvatarId) || string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<bool>("Target and item name are required.", StarApiResultCode.InvalidParam);
        itemName = StripNftDisplayPrefix(itemName);
        if (quantity < 1) quantity = 1;

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Target", targetUsernameOrAvatarId.Trim());
            writer.WriteString("ItemName", itemName.Trim());
            if (itemId.HasValue && itemId.Value != Guid.Empty)
                writer.WriteString("ItemId", itemId.Value.ToString());
            writer.WriteNumber("Quantity", quantity);
            writer.WriteEndObject();
        });

        /* Use 8s timeout so "avatar not found" returns quickly instead of waiting for full default timeout. */
        using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        sendCts.CancelAfter(TimeSpan.FromSeconds(8));
        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return FailAndCallback<bool>(missingWeb4, StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Post, $"{web4Base}/api/avatar/inventory/send-to-avatar", payload, sendCts.Token).ConfigureAwait(false);
        if (response.IsError)
        {
            if (response.Exception is OperationCanceledException)
                return FailAndCallback<bool>("Request timed out (8s). Avatar may not exist or server is slow.", StarApiResultCode.Network, response.Exception);
            return FailAndCallback<bool>(response.Message, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out _, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return FailAndCallback<bool>(parseErrorMessage, parseErrorCode);

        RemoveFromInventoryCache(itemName, quantity);
        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Item sent to avatar.");
    }

    /// <summary>Run send-item-to-avatar on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueSendItemToAvatarAsync(string targetUsernameOrAvatarId, string itemName, int quantity = 1, Guid? itemId = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => SendItemToAvatarAsync(targetUsernameOrAvatarId, itemName, quantity, itemId, ct), cancellationToken);

    /// <summary>Sends an item from the current avatar's inventory to a clan. Target is clan name (or username). Optionally pass itemId (Guid) to send that specific item. Works for all items (STAR and local).</summary>
    public async Task<OASISResult<bool>> SendItemToClanAsync(string clanNameOrTargetUsername, string itemName, int quantity = 1, Guid? itemId = null, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized())
            return FailAndCallback<bool>("Client is not initialized.", StarApiResultCode.NotInitialized);
        if (string.IsNullOrWhiteSpace(clanNameOrTargetUsername) || string.IsNullOrWhiteSpace(itemName))
            return FailAndCallback<bool>("Clan name (or target) and item name are required.", StarApiResultCode.InvalidParam);
        itemName = StripNftDisplayPrefix(itemName);
        if (quantity < 1) quantity = 1;

        var payload = BuildJson(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("Target", clanNameOrTargetUsername.Trim());
            writer.WriteString("ItemName", itemName.Trim());
            if (itemId.HasValue && itemId.Value != Guid.Empty)
                writer.WriteString("ItemId", itemId.Value.ToString());
            writer.WriteNumber("Quantity", quantity);
            writer.WriteEndObject();
        });

        /* Use 8s timeout so "clan not found" returns quickly instead of waiting for full default timeout. */
        using var sendCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        sendCts.CancelAfter(TimeSpan.FromSeconds(8));
        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return FailAndCallback<bool>(missingWeb4, StarApiResultCode.InvalidParam);

        var response = await SendRawAsync(HttpMethod.Post, $"{web4Base}/api/avatar/inventory/send-to-clan", payload, sendCts.Token).ConfigureAwait(false);
        if (response.IsError)
        {
            if (response.Exception is OperationCanceledException)
                return FailAndCallback<bool>("Request timed out (8s). Clan may not exist or server is slow.", StarApiResultCode.Network, response.Exception);
            var msg = response.Message ?? string.Empty;
            if (msg.IndexOf("avatar", StringComparison.OrdinalIgnoreCase) >= 0)
                msg = "Clan not found.";
            return FailAndCallback<bool>(msg, ParseCode(response.ErrorCode, StarApiResultCode.ApiError), response.Exception);
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out _, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
        {
            var msg = parseErrorMessage ?? string.Empty;
            if (msg.IndexOf("avatar", StringComparison.OrdinalIgnoreCase) >= 0)
                msg = "Clan not found.";
            return FailAndCallback<bool>(msg, parseErrorCode);
        }

        RemoveFromInventoryCache(itemName, quantity);
        InvokeCallback(StarApiResultCode.Success);
        return Success(true, StarApiResultCode.Success, "Item sent to clan.");
    }

    /// <summary>Run send-item-to-clan on the background worker so the calling thread does not block.</summary>
    public Task<OASISResult<bool>> QueueSendItemToClanAsync(string clanNameOrTargetUsername, string itemName, int quantity = 1, Guid? itemId = null, CancellationToken cancellationToken = default) =>
        RunOnBackgroundAsync(ct => SendItemToClanAsync(clanNameOrTargetUsername, itemName, quantity, itemId, ct), cancellationToken);

    /* ── Cross-game teleportation ─────────────────────────────────────────── */

    /// <summary>Write outgoing teleport request to %TEMP%\oasis_teleport_{avatarId}.json for OmniverseKernel pickup.</summary>
    public void RequestTeleport(string targetGame, string targetMap, float x, float y, float z)
    {
        try
        {
            var avatarId = GetCachedAvatarId() ?? "unknown";
            var path = Path.Combine(Path.GetTempPath(), $"oasis_teleport_{avatarId}.json");
            var json = $"{{\"targetGame\":{JsonSerializer.Serialize(targetGame)},\"targetMap\":{JsonSerializer.Serialize(targetMap)},\"x\":{x:R},\"y\":{y:R},\"z\":{z:R}}}";
            File.WriteAllText(path, json);
            OGEngineExports.StarApiLogFileOnly($"[Teleport] RequestTeleport: wrote {path} targetGame={targetGame} targetMap={targetMap} x={x} y={y} z={z}");
        }
        catch (Exception ex)
        {
            OGEngineExports.StarApiLogFileOnly($"[Teleport] RequestTeleport error: {ex.Message}");
        }
    }

    /// <summary>Write portal unlock signal to %TEMP%\oasis_portal_unlock_{portalId}.json for OGEditor/OmniverseKernel pickup.</summary>
    public void NotifyPortalUnlock(string portalId)
    {
        try
        {
            var avatarId = GetCachedAvatarId() ?? "unknown";
            var path = Path.Combine(Path.GetTempPath(), $"oasis_portal_unlock_{portalId}.json");
            var json = $"{{\"portalId\":{JsonSerializer.Serialize(portalId)},\"avatarId\":{JsonSerializer.Serialize(avatarId)},\"unlockedAt\":{JsonSerializer.Serialize(DateTimeOffset.UtcNow.ToString("O"))}}}";
            File.WriteAllText(path, json);
            OGEngineExports.StarApiLogFileOnly($"[Portal] NotifyPortalUnlock: wrote {path} portalId={portalId}");
        }
        catch (Exception ex)
        {
            OGEngineExports.StarApiLogFileOnly($"[Portal] NotifyPortalUnlock error: {ex.Message}");
        }
    }

    /// <summary>Read and delete %TEMP%\oasis_teleport_arrive_{avatarId}.json. Returns true and fills out params if a pending request exists.</summary>
    public bool PollTeleportRequest(out string map, out float x, out float y, out float z)
    {
        map = string.Empty;
        x = y = z = 0f;
        try
        {
            var avatarId = GetCachedAvatarId() ?? "unknown";
            var path = Path.Combine(Path.GetTempPath(), $"oasis_teleport_arrive_{avatarId}.json");
            if (!File.Exists(path)) return false;
            var json = File.ReadAllText(path);
            File.Delete(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            map = root.TryGetProperty("targetMap", out var mapProp) ? (mapProp.GetString() ?? string.Empty) : string.Empty;
            x = root.TryGetProperty("x", out var xProp) ? xProp.GetSingle() : 0f;
            y = root.TryGetProperty("y", out var yProp) ? yProp.GetSingle() : 0f;
            z = root.TryGetProperty("z", out var zProp) ? zProp.GetSingle() : 0f;
            OGEngineExports.StarApiLogFileOnly($"[Teleport] PollTeleportRequest: found arrive map={map} x={x} y={y} z={z}");
            return true;
        }
        catch (Exception ex)
        {
            OGEngineExports.StarApiLogFileOnly($"[Teleport] PollTeleportRequest error: {ex.Message}");
            return false;
        }
    }

    /// <summary>Notify STAR API that the avatar has arrived at the teleport destination (POST /api/teleport/confirm-arrival).</summary>
    public async Task<OASISResult<bool>> ConfirmTeleportArrivalAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseApiUrl))
            return Fail<bool>("STAR API base URL is not set.", StarApiResultCode.NotInitialized);
        var avatarId = GetCachedAvatarId() ?? string.Empty;
        var payload = $"{{\"avatarId\":{JsonSerializer.Serialize(avatarId)}}}";
        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/teleport/confirm-arrival", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            OGEngineExports.StarApiLogFileOnly($"[Teleport] ConfirmTeleportArrival error: {response.Message}");
            return Fail<bool>(response.Message ?? "ConfirmTeleportArrival failed.", StarApiResultCode.ApiError);
        }
        OGEngineExports.StarApiLogFileOnly("[Teleport] ConfirmTeleportArrival: OK");
        return Success(true, StarApiResultCode.Success, "Teleport arrival confirmed.");
    }

    /* ── Cross-game entity spawning ───────────────────────────────────────── */

    /// <summary>Read and delete %TEMP%\oasis_spawn_{avatarId}.json. Returns true and fills out params if a pending spawn event exists.</summary>
    public bool PollSpawnEvent(out string entityId, out float x, out float y, out float z)
    {
        entityId = string.Empty;
        x = y = z = 0f;
        try
        {
            var avatarId = GetCachedAvatarId() ?? "unknown";
            var path = Path.Combine(Path.GetTempPath(), $"oasis_spawn_{avatarId}.json");
            if (!File.Exists(path)) return false;
            var json = File.ReadAllText(path);
            File.Delete(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            entityId = root.TryGetProperty("entityId", out var eProp) ? (eProp.GetString() ?? string.Empty) : string.Empty;
            x = root.TryGetProperty("x", out var xProp) ? xProp.GetSingle() : 0f;
            y = root.TryGetProperty("y", out var yProp) ? yProp.GetSingle() : 0f;
            z = root.TryGetProperty("z", out var zProp) ? zProp.GetSingle() : 0f;
            OGEngineExports.StarApiLogFileOnly($"[Spawn] PollSpawnEvent: found entityId={entityId} x={x} y={y} z={z}");
            return true;
        }
        catch (Exception ex)
        {
            OGEngineExports.StarApiLogFileOnly($"[Spawn] PollSpawnEvent error: {ex.Message}");
            return false;
        }
    }

    /// <summary>Notify STAR API that the named entity has been spawned (POST /api/spawn-events/confirm).</summary>
    public async Task<OASISResult<bool>> ConfirmSpawnAsync(string entityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseApiUrl))
            return Fail<bool>("STAR API base URL is not set.", StarApiResultCode.NotInitialized);
        var avatarId = GetCachedAvatarId() ?? string.Empty;
        var payload = $"{{\"entityId\":{JsonSerializer.Serialize(entityId)},\"avatarId\":{JsonSerializer.Serialize(avatarId)}}}";
        var response = await SendRawAsync(HttpMethod.Post, $"{_baseApiUrl}/api/spawn-events/confirm", payload, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            OGEngineExports.StarApiLogFileOnly($"[Spawn] ConfirmSpawn error: {response.Message}");
            return Fail<bool>(response.Message ?? "ConfirmSpawn failed.", StarApiResultCode.ApiError);
        }
        OGEngineExports.StarApiLogFileOnly($"[Spawn] ConfirmSpawn: OK entityId={entityId}");
        return Success(true, StarApiResultCode.Success, "Spawn confirmed.");
    }

    /* ── Map entity list ──────────────────────────────────────────────────── */

    /// <summary>Fetch the cross-game entity list for a map from STAR API (GET /api/maps/{gameId}/{mapName}/entities). Returns raw JSON array.</summary>
    public async Task<OASISResult<string>> GetMapEntitiesAsync(string gameId, string mapName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseApiUrl))
            return Fail<string>("STAR API base URL is not set.", StarApiResultCode.NotInitialized);
        var gId = Uri.EscapeDataString(gameId);
        var mName = Uri.EscapeDataString(mapName);
        var response = await SendRawAsync(HttpMethod.Get, $"{_baseApiUrl}/api/maps/{gId}/{mName}/entities", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            OGEngineExports.StarApiLogFileOnly($"[MapEntities] GetMapEntities error: gameId={gameId} mapName={mapName} {response.Message}");
            return Fail<string>(response.Message ?? "GetMapEntities failed.", StarApiResultCode.ApiError);
        }
        OGEngineExports.StarApiLogFileOnly($"[MapEntities] GetMapEntities: OK gameId={gameId} mapName={mapName}");
        return Success(response.Result ?? "[]", StarApiResultCode.Success, "Map entities retrieved.");
    }

    public OASISResult<string> GetLastError()
    {
        lock (_stateLock)
            return Success(_lastError, StarApiResultCode.Success, "Last error retrieved.");
    }

    public OASISResult<bool> SetCallback(StarApiCallback? callback, object? userData = null)
    {
        lock (_stateLock)
        {
            _callback = callback;
            _callbackUserData = userData;
        }

        return Success(true, StarApiResultCode.Success, "Callback updated.");
    }

    public void Dispose()
    {
        Cleanup();
    }

    /// <summary>Try to refresh JWT using refresh token so play is not interrupted when token expires. Uses OASIS refresh-token endpoint (cookie or body). Tries _oasisBaseUrl first, then _baseApiUrl for STAR API–only setups.</summary>
    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenRefreshSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            /* Another caller may have refreshed while we waited on the semaphore. */
            string? jwtAfterWait;
            lock (_stateLock) { jwtAfterWait = _jwtToken; }
            if (!string.IsNullOrWhiteSpace(jwtAfterWait))
            {
                var exp = GetJwtExpirationUtc(jwtAfterWait);
                if (exp.HasValue && exp.Value > DateTime.UtcNow.AddSeconds(30))
                {
                    OGEngineExports.StarApiLogFileOnly("[Auth] Token refresh skipped: JWT already valid (another caller refreshed).");
                    return true;
                }
            }

            string? refreshToken;
            string oasisBase;
            string starBase;
            lock (_stateLock)
            {
                refreshToken = _refreshToken;
                oasisBase = _oasisBaseUrl ?? string.Empty;
                starBase = _baseApiUrl ?? string.Empty;
            }
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                OGEngineExports.StarApiLogFileOnly("[Auth] Token refresh skipped: no refresh token (save session after beam-in to persist it).");
                return false;
            }
            /* Prefer OASIS (Web4) URL; fall back to STAR API (Web5) so refresh works when only _baseApiUrl is set. */
            var baseUrl = !string.IsNullOrWhiteSpace(oasisBase) ? oasisBase.TrimEnd('/') : !string.IsNullOrWhiteSpace(starBase) ? starBase.TrimEnd('/') : null;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                OGEngineExports.StarApiLogFileOnly("[Auth] Token refresh skipped: no OASIS or STAR API base URL set.");
                return false;
            }
            if (_httpClient is null)
            {
                OGEngineExports.StarApiLogFileOnly("[Auth] Token refresh skipped: HTTP client is null.");
                return false;
            }

            try
            {
                var url = $"{baseUrl}/api/avatar/refresh-token";
                var usedOasis = !string.IsNullOrWhiteSpace(oasisBase);
                OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh: POST {(usedOasis ? "OASIS" : "STAR API")} url={url}");
                /* Do not send Authorization header: ONODE JwtMiddleware validates the JWT on every request. If we send the expired JWT, middleware returns 401 before the refresh-token controller runs. */
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                /* WEB4/ONODE reads refreshToken from cookie for browsers; HttpClient often does not send cookies the same way — send JSON body (ONODE RefreshTokenRequest) as primary. */
                var refreshBody = BuildJson(w =>
                {
                    w.WriteStartObject();
                    w.WriteString("refreshToken", refreshToken);
                    w.WriteEndObject();
                });
                request.Content = new StringContent(refreshBody, Encoding.UTF8, "application/json");

                using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                var responseBody = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : string.Empty;

                if (!response.IsSuccessStatusCode)
                {
                    OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh failed: HTTP {(int)response.StatusCode} url={url} responseBody={responseBody}");
                    OGEngineExports.StarApiLog($"[Auth] Token refresh failed: HTTP {(int)response.StatusCode} (full body in ogengine.log). Deploy ONODE with POST /api/avatar/refresh-token accepting JSON {{\"refreshToken\":\"...\"}} if renew always fails.");
                    return false;
                }

                var parseResult = ParseEnvelopeOrPayload(responseBody, out var resultElement, out _, out var parseErrMsg);
                if (!parseResult)
                {
                    var msg = string.IsNullOrWhiteSpace(parseErrMsg) ? "API returned error envelope." : parseErrMsg;
                    OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh parse failed: {msg} url={url} responseBody={responseBody}");
                    OGEngineExports.StarApiLog($"[Auth] Token refresh failed: {msg} (full body in ogengine.log)");
                    return false;
                }
                if (resultElement.ValueKind == JsonValueKind.Object && GetBoolProperty(resultElement, "IsError"))
                {
                    var msg = GetStringProperty(resultElement, "Message");
                    var em = string.IsNullOrWhiteSpace(msg) ? "API returned an error." : msg!;
                    OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh OASISResult IsError: {em} url={url} responseBody={responseBody}");
                    OGEngineExports.StarApiLog($"[Auth] Token refresh failed: {em} (details in ogengine.log)");
                    return false;
                }
                AvatarAuthResponse? auth = ParseAvatarAuthResponse(resultElement);
                if (auth is null || string.IsNullOrWhiteSpace(auth.JwtToken))
                {
                    try
                    {
                        using var rawDoc = JsonDocument.Parse(responseBody);
                        var rawJwt = FindStringRecursive(rawDoc.RootElement, "JwtToken") ?? FindStringRecursive(rawDoc.RootElement, "Token")
                            ?? FindStringRecursive(rawDoc.RootElement, "accessToken") ?? FindStringRecursive(rawDoc.RootElement, "access_token")
                            ?? FindStringRecursive(rawDoc.RootElement, "jwt");
                        var rawRefresh = FindStringRecursive(rawDoc.RootElement, "RefreshToken");
                        if (!string.IsNullOrWhiteSpace(rawJwt))
                            auth = new AvatarAuthResponse { JwtToken = rawJwt, RefreshToken = rawRefresh };
                    }
                    catch { /* ignore */ }
                }
                if (auth is null || string.IsNullOrWhiteSpace(auth.JwtToken))
                {
                    OGEngineExports.StarApiLogFileOnly($"[Auth] Token refresh: could not parse JwtToken from envelope. url={url} responseBody={responseBody}");
                    OGEngineExports.StarApiLog("[Auth] Token refresh failed: no JwtToken in response (full JSON in ogengine.log).");
                    return false;
                }

                lock (_stateLock)
                {
                    _jwtToken = auth.JwtToken;
                    if (!string.IsNullOrWhiteSpace(auth.RefreshToken))
                        _refreshToken = auth.RefreshToken;
                    if (_httpClient is not null)
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
                }
                OGEngineExports.StarApiLog("[Auth] JWT refreshed successfully.");
                ScheduleBackgroundTokenRefresh();
                return true;
            }
            catch (Exception ex)
            {
                OGEngineExports.StarApiLog($"[Auth] Token refresh exception: {ex.Message}");
                return false;
            }
        }
        finally
        {
            _tokenRefreshSemaphore.Release();
        }
    }

    private static readonly object _tokenRefreshScheduledLock = new();
    private static bool _tokenRefreshScheduled;

    /// <summary>Schedule a single background refresh shortly before JWT expiry so play is not interrupted.</summary>
    private void ScheduleBackgroundTokenRefresh()
    {
        lock (_tokenRefreshScheduledLock)
        {
            if (_tokenRefreshScheduled)
                return;
            _tokenRefreshScheduled = true;
        }
        _ = RunOnBackgroundAsync<bool>(async ct =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(10), ct).ConfigureAwait(false);
                for (int i = 0; i < 30; i++)
                {
                    if (ct.IsCancellationRequested) return Success(true, StarApiResultCode.Success, "Cancelled");
                    string? jwt;
                    lock (_stateLock) { jwt = _jwtToken; }
                    if (string.IsNullOrWhiteSpace(jwt)) return Success(true, StarApiResultCode.Success, "No token");
                    var exp = GetJwtExpirationUtc(jwt);
                    if (exp.HasValue && exp.Value > DateTime.UtcNow && (exp.Value - DateTime.UtcNow).TotalMinutes < 5)
                    {
                        var refreshed = await TryRefreshTokenAsync(ct).ConfigureAwait(false);
                        if (refreshed) OGEngineExports.StarApiLog("[Auth] Background token refresh completed.");
                        return Success(true, StarApiResultCode.Success, "Refreshed or skipped");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(20), ct).ConfigureAwait(false);
                }
                return Success(true, StarApiResultCode.Success, "Done");
            }
            finally
            {
                lock (_tokenRefreshScheduledLock) { _tokenRefreshScheduled = false; }
            }
        }, default);
    }

    private static DateTime? GetJwtExpirationUtc(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) return null;
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4) { case 2: payload += "=="; break; case 3: payload += "="; break; }
            var decoded = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(decoded);
            if (doc.RootElement.TryGetProperty("exp", out var expProp) && expProp.ValueKind == JsonValueKind.Number)
            {
                if (expProp.TryGetInt64(out var unix))
                    return DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private async Task<OASISResult<string>> SendRawAsync(HttpMethod method, string url, string? bodyJson, CancellationToken cancellationToken)
    {
        if (_httpClient is null)
            return Fail<string>("HTTP client is not initialized.", StarApiResultCode.NotInitialized);

        lock (_stateLock)
        {
            if (_sessionExpiredCleared && string.IsNullOrEmpty(_jwtToken))
                return Fail<string>("Session expired. Please beam in again.", StarApiResultCode.ApiError);
        }

        try
        {
            var result = await SendRawAsyncCore(method, url, bodyJson, cancellationToken).ConfigureAwait(false);
            // On 401, try refresh once and retry the request (minimal JWT timeout fix).
            if (result.IsError && result.Message != null && result.Message.Contains("401", StringComparison.Ordinal))
            {
                var refreshed = await TryRefreshTokenAsync(cancellationToken).ConfigureAwait(false);
                if (refreshed)
                    result = await SendRawAsyncCore(method, url, bodyJson, cancellationToken).ConfigureAwait(false);
                else
                {
                    /* Concurrent refresh may have succeeded on another worker; do not clear a good session. */
                    string? jwtCheck;
                    lock (_stateLock) { jwtCheck = _jwtToken; }
                    var exp = string.IsNullOrWhiteSpace(jwtCheck) ? null : GetJwtExpirationUtc(jwtCheck);
                    if (exp.HasValue && exp.Value > DateTime.UtcNow.AddSeconds(15))
                    {
                        OGEngineExports.StarApiLogFileOnly("[Auth] 401 retry: refresh returned false but JWT is valid (concurrent refresh); retrying request once.");
                        result = await SendRawAsyncCore(method, url, bodyJson, cancellationToken).ConfigureAwait(false);
                    }
                    if (result.IsError)
                    {
                        ClearSessionToken();
                        OGEngineExports.StarApiLog("[Auth] JWT expired and refresh failed or no refresh token; session cleared. Please beam in again.");
                    }
                }
            }
            return result;
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException != null && !string.IsNullOrWhiteSpace(ex.InnerException.Message)
                ? $"Network call failed: {ex.Message} ({ex.InnerException.Message})"
                : $"Network call failed: {ex.Message}";
            return Fail<string>(msg, StarApiResultCode.Network, ex);
        }
    }

    /// <summary>Send request with bounded retries on transient network errors (audit: retry with backoff).</summary>
    private async Task<OASISResult<string>> SendRawWithRetryAsync(HttpMethod method, string url, string? bodyJson, CancellationToken cancellationToken)
    {
        OASISResult<string> last = Fail<string>("No attempt.", StarApiResultCode.Network);
        for (var attempt = 0; attempt < HttpRetryMaxAttempts; attempt++)
        {
            if (attempt > 0)
            {
                var delayMs = attempt <= HttpRetryDelayMs.Length ? HttpRetryDelayMs[attempt - 1] : HttpRetryDelayMs[^1];
                try
                {
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return last;
                }
                OGEngineExports.StarApiLogFileOnly($"[HTTP] Retry attempt {attempt + 1}/{HttpRetryMaxAttempts} after {delayMs}ms: {method.Method} {url}");
            }
            last = await SendRawAsync(method, url, bodyJson, cancellationToken).ConfigureAwait(false);
            if (!last.IsError)
                return last;
            var code = ParseCode(last.ErrorCode, StarApiResultCode.ApiError);
            if (code != StarApiResultCode.Network)
                return last; /* Don't retry auth or API errors. */
        }
        return last;
    }

    /// <summary>WEB5 on Linux may return HTTP 406 with a valid OASIS envelope (isError false, result array/object). Root-level check avoids relying on full envelope unroll for multi-MB bodies.</summary>
    private static bool Is406ResponseWithOasisSuccessResult(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        try
        {
            using var doc = JsonDocument.Parse(body, DeepJsonDocumentOptions);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            if (GetBoolProperty(root, "IsError")) return false;
            if (!TryGetProperty(root, "Result", out var res)) return false;
            return res.ValueKind == JsonValueKind.Array || res.ValueKind == JsonValueKind.Object;
        }
        catch (Exception ex)
        {
            try { OGEngineExports.StarApiLogFileOnly($"[HTTP] 406 success-check: parse failed {ex.GetType().Name}: {ex.Message}"); } catch { /* ignore */ }
            return false;
        }
    }

    private async Task<OASISResult<string>> SendRawAsyncCore(HttpMethod method, string url, string? bodyJson, CancellationToken cancellationToken)
    {
        if (_httpClient is null)
            return Fail<string>("HTTP client is not initialized.", StarApiResultCode.NotInitialized);

        using var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(bodyJson))
            request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

        lock (_stateLock)
        {
            if (!string.IsNullOrWhiteSpace(_avatarId))
                request.Headers.TryAddWithoutValidation("X-Avatar-Id", _avatarId);

            var bearerToken = _jwtToken;
            if (string.IsNullOrWhiteSpace(bearerToken) && _httpClient.DefaultRequestHeaders.Authorization?.Scheme == "Bearer")
                bearerToken = _httpClient.DefaultRequestHeaders.Authorization.Parameter;
            if (!string.IsNullOrWhiteSpace(bearerToken))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var responseBody = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : string.Empty;

        if (!response.IsSuccessStatusCode)
        {
            // WEB5 on Linux sometimes returns 406 with a full success JSON body. Never attach response bodies to errors or StarApiLog — multi-MB strings crash native logging.
            if ((int)response.StatusCode == 406 && Is406ResponseWithOasisSuccessResult(responseBody))
            {
                OGEngineExports.StarApiLogFileOnly($"[HTTP] 406 {method.Method} treated as success (OASIS success JSON): {url}");
                return Success(responseBody ?? string.Empty, StarApiResultCode.Success, "Request completed (HTTP 406 with success JSON).");
            }

            var path = url;
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var u) && u.Segments?.Length > 0)
                    path = string.Concat(u.Segments);
            }
            catch { /* use full url if parse fails */ }
            OGEngineExports.StarApiLog($"[HTTP] {(int)response.StatusCode} {method.Method} {path}");
            OGEngineExports.StarApiLogFileOnly($"[HTTP] {(int)response.StatusCode} {method.Method} {path} url={url} bodyLen={responseBody.Length}");
            var failureMessage = $"HTTP {(int)response.StatusCode} ({response.StatusCode}) calling {url}.";
            return Fail<string>(failureMessage, StarApiResultCode.ApiError);
        }

        return Success(responseBody ?? string.Empty, StarApiResultCode.Success, "Request completed successfully.");
    }

    private async Task<OASISResult<string>> EnsureAvatarIdAsync(CancellationToken cancellationToken)
    {
        lock (_stateLock)
        {
            if (!string.IsNullOrWhiteSpace(_avatarId))
                return Success(_avatarId!, StarApiResultCode.Success, "Avatar ID already available.");
        }

        if (!TryGetWeb4BaseTrimmed(out var web4Base, out var missingWeb4))
            return Fail<string>(missingWeb4, StarApiResultCode.InvalidParam);

        var response = await SendRawWithRetryAsync(HttpMethod.Get, $"{web4Base}{Web4GetLoggedInAvatarWithXpPath}", null, cancellationToken).ConfigureAwait(false);
        if (response.IsError)
        {
            return new OASISResult<string>
            {
                IsError = true,
                Message = response.Message,
                ErrorCode = response.ErrorCode,
                Exception = response.Exception
            };
        }

        var parseResult = ParseEnvelopeOrPayload(response.Result, out var resultElement, out var parseErrorCode, out var parseErrorMessage);
        if (!parseResult)
            return Fail<string>(parseErrorMessage, parseErrorCode);

        var avatar = ParseAvatarInfo(resultElement);
        if (avatar is null || avatar.Id == Guid.Empty)
            return Fail<string>("Could not resolve current avatar ID.", StarApiResultCode.ApiError);

        lock (_stateLock)
            _avatarId = avatar.Id.ToString();

        return Success(_avatarId!, StarApiResultCode.Success, "Resolved current avatar ID.");
    }

    private static string BuildJson(Action<Utf8JsonWriter> writeAction)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writeAction(writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private bool ParseEnvelopeOrPayload(string? body, out JsonElement result, out StarApiResultCode errorCode, out string errorMessage)
    {
        result = default;
        errorCode = StarApiResultCode.ApiError;
        errorMessage = "Response body was empty.";

        if (string.IsNullOrWhiteSpace(body))
        {
            result = default;
            errorCode = StarApiResultCode.Success;
            errorMessage = string.Empty;
            return true;
        }

        try
        {
            using var doc = JsonDocument.Parse(body, DeepJsonDocumentOptions);
            var current = doc.RootElement.Clone();
            var depth = 0;

            while (depth < 4 && current.ValueKind == JsonValueKind.Object)
            {
                depth++;

                var isError = GetBoolProperty(current, "IsError");
                var message = GetStringProperty(current, "Message");
                var codeText = GetStringProperty(current, "ErrorCode");
                var parsedCode = ParseCode(codeText, StarApiResultCode.ApiError);

                if (isError)
                {
                    errorCode = parsedCode;
                    errorMessage = string.IsNullOrWhiteSpace(message) ? "API returned an error." : message!;
                    result = current.Clone();
                    return false;
                }

                if (TryGetProperty(current, "Result", out var nested))
                {
                    if (nested.ValueKind == JsonValueKind.Object &&
                        (TryGetProperty(nested, "Result", out _) || TryGetProperty(nested, "IsError", out _)))
                    {
                        current = nested.Clone();
                        continue;
                    }

                    /* OASISHttpResponseMessage shape: outer unwraps to an OASISResult object with isError/message but no further Result to descend into. */
                    if (nested.ValueKind == JsonValueKind.Object && GetBoolProperty(nested, "IsError"))
                    {
                        var msg = GetStringProperty(nested, "Message");
                        errorCode = ParseCode(GetStringProperty(nested, "ErrorCode"), StarApiResultCode.ApiError);
                        errorMessage = string.IsNullOrWhiteSpace(msg) ? "API returned an error." : msg!;
                        result = nested.Clone();
                        return false;
                    }

                    result = nested.Clone();
                    errorCode = StarApiResultCode.Success;
                    errorMessage = string.Empty;
                    return true;
                }

                break;
            }

            result = current.Clone();
            errorCode = StarApiResultCode.Success;
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorCode = StarApiResultCode.ApiError;
            errorMessage = $"Invalid JSON response: {ex.Message}";
            return false;
        }
    }

    private List<StarItem> ParseInventoryItems(JsonElement element)
    {
        var items = new List<StarItem>();
        var arraysToMerge = new List<JsonElement>();

        if (element.ValueKind == JsonValueKind.Array)
            arraysToMerge.Add(element);
        else if (element.ValueKind == JsonValueKind.Object)
        {
            // API may return payload as Result/result (array or object with array inside). Merge all arrays so ammo/armor/items appear.
            var arrayPropertyNames = new[] { "Result", "Results", "Items", "Inventory", "Data", "Holons", "InventoryItems", "value" };
            foreach (var name in arrayPropertyNames)
            {
                if (TryGetProperty(element, name, out var prop) && prop.ValueKind == JsonValueKind.Array)
                    arraysToMerge.Add(prop);
            }
        }

        foreach (var arrayElement in arraysToMerge)
        {
            foreach (var itemElement in arrayElement.EnumerateArray())
            {
                var item = ParseInventoryItemResponse(itemElement);
                if (item is null)
                    continue;

                var nftId = !string.IsNullOrWhiteSpace(item.NftId) ? item.NftId
                    : ExtractMeta(item.MetaData, "NFTId", string.Empty) ?? ExtractMeta(item.MetaData, "OASISNFTId", string.Empty) ?? string.Empty;
                items.Add(new StarItem
                {
                    Id = item.Id,
                    Name = item.Name ?? string.Empty,
                    Description = item.Description ?? string.Empty,
                    GameSource = !string.IsNullOrWhiteSpace(item.GameSource) ? item.GameSource : "n/a",
                    ItemType = !string.IsNullOrWhiteSpace(item.ItemType) ? item.ItemType : "Miscellaneous",
                    NftId = nftId,
                    Quantity = item.Quantity
                });
            }
        }

        return items;
    }

    /// <summary>WEB4 inventory holons often omit GameSource; add-item stores <c>"{desc} | Source: ODOOM"</c> in Description.</summary>
    private static string? TryExtractGameSourceFromDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        var span = description.AsSpan();
        ReadOnlySpan<char> key = "Source:";
        var idx = span.LastIndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        var tail = span[(idx + key.Length)..].TrimStart();
        if (tail.Length == 0) return null;
        var pipe = tail.IndexOf('|');
        if (pipe >= 0) tail = tail[..pipe].TrimEnd();
        return tail.Length > 0 ? tail.ToString() : null;
    }

    private InventoryItemResponse? ParseInventoryItemResponse(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        // API may return item wrapped in Holon/Item/Data (e.g. new items). Unwrap so we parse same shape as POST response.
        if (TryGetProperty(element, "Holon", out var inner) && inner.ValueKind == JsonValueKind.Object)
            element = inner;
        else if (TryGetProperty(element, "Item", out inner) && inner.ValueKind == JsonValueKind.Object)
            element = inner;
        else if (TryGetProperty(element, "Data", out inner) && inner.ValueKind == JsonValueKind.Object)
            element = inner;

        var idValue = GetStringProperty(element, "Id") ?? GetStringProperty(element, "id");
        Guid.TryParse(idValue, out var parsedGuid);

        Dictionary<string, JsonElement>? metadata = null;
        if (TryGetProperty(element, "MetaData", out var metaElement) && metaElement.ValueKind == JsonValueKind.Object)
            metadata = CloneMetaData(metaElement);
        else if (TryGetProperty(element, "Metadata", out metaElement) && metaElement.ValueKind == JsonValueKind.Object)
            metadata = CloneMetaData(metaElement);

        var name = GetStringProperty(element, "Name") ?? GetStringProperty(element, "name");
        var description = GetStringProperty(element, "Description") ?? GetStringProperty(element, "description");
        var gameSource = GetStringProperty(element, "GameSource") ?? GetStringProperty(element, "gameSource");
        var itemType = GetStringProperty(element, "ItemType") ?? GetStringProperty(element, "itemType");
        int quantity = 1;
        if (TryGetProperty(element, "Quantity", out var qtyEl))
        {
            if (qtyEl.ValueKind == JsonValueKind.Number && qtyEl.TryGetInt32(out var q))
                quantity = q;
            else if (qtyEl.ValueKind == JsonValueKind.String && int.TryParse(qtyEl.GetString(), out var qs))
                quantity = qs;
        }
        if (metadata != null)
        {
            if (string.IsNullOrWhiteSpace(name)) name = ExtractMeta(metadata, "Name", string.Empty) ?? ExtractMeta(metadata, "name", string.Empty) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemType)) itemType = ExtractMeta(metadata, "ItemType", string.Empty) ?? ExtractMeta(metadata, "itemType", string.Empty) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(gameSource)) gameSource = ExtractMeta(metadata, "GameSource", string.Empty) ?? ExtractMeta(metadata, "gameSource", string.Empty) ?? string.Empty;
            if (quantity <= 1)
            {
                var qtyStr = ExtractMeta(metadata, "Quantity", string.Empty) ?? ExtractMeta(metadata, "quantity", string.Empty);
                if (!string.IsNullOrWhiteSpace(qtyStr) && int.TryParse(qtyStr, out var qm) && qm > 0)
                    quantity = qm;
            }
        }
        if (quantity < 1) quantity = 1;
        if (string.IsNullOrWhiteSpace(name) && parsedGuid == Guid.Empty)
            return null;

        if (string.IsNullOrWhiteSpace(gameSource))
        {
            var extractedGs = TryExtractGameSourceFromDescription(description);
            if (!string.IsNullOrWhiteSpace(extractedGs))
                gameSource = extractedGs;
        }

        /* NftId: from root (API may use PascalCase or camelCase) or from MetaData so [NFT] prefix persists after reload / in Quake. */
        var nftId = GetStringProperty(element, "NftId") ?? GetStringProperty(element, "nftId") ?? GetStringProperty(element, "NFTId") ?? GetStringProperty(element, "OASISNFTId")
            ?? (metadata != null ? ExtractMeta(metadata, "NFTId", string.Empty) : null)
            ?? (metadata != null ? ExtractMeta(metadata, "OASISNFTId", string.Empty) : null);
        if (string.IsNullOrWhiteSpace(nftId)) nftId = null;

        return new InventoryItemResponse
        {
            Id = parsedGuid,
            Name = name,
            Description = description,
            GameSource = gameSource,
            ItemType = itemType,
            MetaData = metadata,
            Quantity = quantity,
            NftId = nftId
        };
    }

    private static Dictionary<string, JsonElement> CloneMetaData(JsonElement metaElement)
    {
        var metadata = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in metaElement.EnumerateObject())
            metadata[property.Name] = property.Value.Clone();
        return metadata;
    }

    private static AvatarAuthResponse? ParseAvatarAuthResponse(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var idText = GetStringProperty(element, "Id")
            ?? GetStringProperty(element, "AvatarId")
            ?? FindStringRecursive(element, "Id")
            ?? FindStringRecursive(element, "AvatarId");
        Guid.TryParse(idText, out var id);
        var jwt = GetStringProperty(element, "JwtToken") ?? FindStringRecursive(element, "JwtToken")
            ?? GetStringProperty(element, "Token") ?? FindStringRecursive(element, "Token")
            ?? GetStringProperty(element, "accessToken") ?? FindStringRecursive(element, "accessToken")
            ?? GetStringProperty(element, "access_token") ?? FindStringRecursive(element, "access_token")
            ?? GetStringProperty(element, "jwt") ?? FindStringRecursive(element, "jwt");
        var refresh = GetStringProperty(element, "RefreshToken") ?? FindStringRecursive(element, "RefreshToken");

        if (id != Guid.Empty || !string.IsNullOrWhiteSpace(jwt) || !string.IsNullOrWhiteSpace(refresh))
        {
            return new AvatarAuthResponse
            {
                Id = id,
                JwtToken = jwt,
                RefreshToken = refresh
            };
        }

        if (TryGetProperty(element, "Result", out var nested) && nested.ValueKind == JsonValueKind.Object)
            return ParseAvatarAuthResponse(nested);

        return new AvatarAuthResponse
        {
            Id = id,
            JwtToken = jwt,
            RefreshToken = refresh
        };
    }

    private static string? FindStringRecursive(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                        return property.Value.GetString();

                    var nestedDirect = FindStringRecursive(property.Value, propertyName);
                    if (!string.IsNullOrWhiteSpace(nestedDirect))
                        return nestedDirect;
                }
                else
                {
                    var nested = FindStringRecursive(property.Value, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                        return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindStringRecursive(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                    return nested;
            }
        }

        return null;
    }

    private static AvatarInfo? ParseAvatarInfo(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        Guid.TryParse(GetStringProperty(element, "Id"), out var id);
        return new AvatarInfo { Id = id };
    }

    private static string? ParseIdAsString(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return GetStringProperty(element, "Id")
                ?? GetStringProperty(element, "OASISNFTId")
                ?? GetStringProperty(element, "STARNETHolonId")
                ?? GetStringProperty(element, "Hash");
        }

        if (element.ValueKind == JsonValueKind.String)
            return element.GetString();

        return null;
    }

    private static bool TryExtractTopLevelResultId(string? json, out string? id)
    {
        id = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            if (!TryGetProperty(doc.RootElement, "Result", out var resultElement) &&
                !TryGetProperty(doc.RootElement, "result", out resultElement))
            {
                return false;
            }

            var parsedId = ParseIdAsString(resultElement);
            if (string.IsNullOrWhiteSpace(parsedId))
                return false;

            id = parsedId;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Guid ExtractAvatarIdFromJwt(string? jwtToken)
    {
        if (string.IsNullOrWhiteSpace(jwtToken))
            return Guid.Empty;

        var parts = jwtToken.Split('.');
        if (parts.Length < 2)
            return Guid.Empty;

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return Guid.Empty;

            var id = GetStringProperty(doc.RootElement, "id") ?? GetStringProperty(doc.RootElement, "Id");
            return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
        }
        catch
        {
            return Guid.Empty;
        }
    }

    private string ExtractMeta(Dictionary<string, JsonElement>? metadata, string key, string fallback)
    {
        if (metadata is not null && metadata.TryGetValue(key, out var value))
        {
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? fallback;

            return value.ToString();
        }

        return fallback;
    }

    private static string? GetStringProperty(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var prop))
            return null;

        return prop.ValueKind switch
        {
            JsonValueKind.String => prop.GetString(),
            JsonValueKind.Number => prop.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            _ => prop.GetRawText()
        };
    }

    /// <summary>Get a list of strings from element, e.g. element.MetaData.PrerequisiteQuestIds (array of string).</summary>
    private static List<string> GetStringListFromElement(JsonElement element, string parentKey, string arrayKey)
    {
        var list = new List<string>();
        if (!TryGetProperty(element, parentKey, out var parent) || parent.ValueKind != JsonValueKind.Object)
            return list;
        if (!TryGetProperty(parent, arrayKey, out var arr))
            return list;
        if (arr.ValueKind != JsonValueKind.Array)
            return list;
        foreach (var item in arr.EnumerateArray())
        {
            var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText()?.Trim('"');
            if (!string.IsNullOrEmpty(s))
                list.Add(s);
        }
        return list;
    }

    /// <summary>Log once per session which JSON key supplied objectives (objectives vs children). File-only so we can see why "objectives" is sometimes empty in the API response.</summary>
    private static void LogObjectivesSourceOnce(string path, int count)
    {
        if (string.IsNullOrEmpty(path) || count <= 0) return;
        try
        {
            var key = $"{path}:{count}";
            if (!_objectivesSourceLogged.Add(key)) return;
            var expected = path.IndexOf("objectives", StringComparison.OrdinalIgnoreCase) >= 0
                ? " (backend is serializing Quest.Objectives correctly)"
                : " (API sent empty 'objectives'; data came from 'children' – backend PromoteQuestMetaDataToProperties or serialization may not be populating objectives)";
            OGEngineExports.StarApiLogFileOnly($"[Quests] Objectives source: path={path} count={count}{expected}");
        }
        catch { /* ignore */ }
    }
    private static readonly HashSet<string> _objectivesSourceLogged = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Get objectives from a quest element. Path in API response: Result[i].objectives or Result[i].children (each quest in the array).
    /// We try: Objectives, objectives, QuestObjectives, questObjectives, Children, children (root then MetaData/MapMetaData) and use the first that yields a non-empty list.
    /// Backend (QuestManager.PromoteQuestMetaDataToProperties) should populate Quest.Objectives from MetaData so "objectives" is in the JSON; if the API sends empty "objectives" and data in "children", we use children.</summary>
    private static List<StarQuestObjective> GetObjectivesFromQuestElement(JsonElement questElement)
    {
        if (questElement.ValueKind != JsonValueKind.Object) return new List<StarQuestObjective>();

        /* Try each known key and use the first that yields a non-empty list. If API returns both "objectives": [] and "children": [...], we must not stop at the empty objectives. */
        static bool TryKnownKeys(JsonElement parent, out List<StarQuestObjective> list, out string? usedKey)
        {
            list = new List<StarQuestObjective>();
            usedKey = null;
            var keys = new[] { "Objectives", "objectives", "QuestObjectives", "questObjectives", "Children", "children" };
            foreach (var key in keys)
            {
                if (!TryGetProperty(parent, key, out var el)) continue;
                var parsed = ParseObjectivesFromElement(el);
                if (parsed.Count > 0)
                {
                    list = parsed;
                    usedKey = key;
                    return true;
                }
            }
            return false;
        }

        if (TryKnownKeys(questElement, out var fromRoot, out var keyUsed))
        {
            LogObjectivesSourceOnce(keyUsed, fromRoot.Count);
            return fromRoot;
        }
        if ((TryGetProperty(questElement, "MetaData", out var meta) || TryGetProperty(questElement, "metaData", out meta)) && meta.ValueKind == JsonValueKind.Object)
        {
            if (TryKnownKeys(meta, out var fromMeta, out keyUsed))
            {
                LogObjectivesSourceOnce("MetaData." + keyUsed, fromMeta.Count);
                return fromMeta;
            }
            if ((TryGetProperty(meta, "MapMetaData", out var mapMeta) || TryGetProperty(meta, "mapMetaData", out mapMeta)) && mapMeta.ValueKind == JsonValueKind.Object)
                if (TryKnownKeys(mapMeta, out var fromMap, out keyUsed))
                {
                    LogObjectivesSourceOnce("MetaData.MapMetaData." + keyUsed, fromMap.Count);
                    return fromMap;
                }
        }

        /* Safe fallback: only keys that contain "objective" (case-insensitive), so we never bind SubQuests/PrerequisiteQuestIds. Handles provider/API key variants. */
        static List<StarQuestObjective> TryKeysContainingObjective(JsonElement parent)
        {
            foreach (var prop in parent.EnumerateObject())
            {
                if (!prop.Name.Contains("objective", StringComparison.OrdinalIgnoreCase)) continue;
                var list = ParseObjectivesFromElement(prop.Value);
                if (list.Count > 0) return list;
            }
            return new List<StarQuestObjective>();
        }
        var fromScan = TryKeysContainingObjective(questElement);
        if (fromScan.Count > 0) return fromScan;
        if ((TryGetProperty(questElement, "MetaData", out var meta2) || TryGetProperty(questElement, "metaData", out meta2)) && meta2.ValueKind == JsonValueKind.Object)
        {
            fromScan = TryKeysContainingObjective(meta2);
            if (fromScan.Count > 0) return fromScan;
            if ((TryGetProperty(meta2, "MapMetaData", out var mapMeta2) || TryGetProperty(meta2, "mapMetaData", out mapMeta2)) && mapMeta2.ValueKind == JsonValueKind.Object)
            {
                fromScan = TryKeysContainingObjective(mapMeta2);
                if (fromScan.Count > 0) return fromScan;
            }
        }

        return new List<StarQuestObjective>();
    }

    /// <summary>Parse a single game-keyed dictionary from JSON. Values may be arrays of strings (preferred), a single string/number, or the whole property may be a JSON string (ONODE <see cref="CustomOASISPropertyAttribute.StoreAsJsonString"/>).</summary>
    private static Dictionary<string, List<string>> ParseStringListDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (element.ValueKind == JsonValueKind.String)
        {
            var raw = element.GetString();
            if (string.IsNullOrWhiteSpace(raw)) return dict;
            try
            {
                using var doc = JsonDocument.Parse(raw);
                return ParseStringListDictionary(doc.RootElement);
            }
            catch
            {
                return dict;
            }
        }
        if (element.ValueKind != JsonValueKind.Object)
            return dict;
        foreach (var prop in element.EnumerateObject())
        {
            var list = new List<string>();
            var v = prop.Value;
            if (v.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in v.EnumerateArray())
                {
                    var s = item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText()?.Trim('"');
                    if (!string.IsNullOrEmpty(s)) list.Add(s!);
                }
            }
            else if (v.ValueKind == JsonValueKind.String)
            {
                var s = v.GetString();
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            }
            else if (v.ValueKind == JsonValueKind.Number)
            {
                list.Add(v.GetRawText());
            }
            else if (v.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                list.Add(v.GetBoolean() ? "1" : "0");
            }
            if (list.Count > 0) dict[prop.Name] = list;
        }
        return dict;
    }

    /// <summary>Parse Objective requirement/progress dictionaries from a JSON object (backend Objective / IQuestObjectiveDictionaries). Tries root first, then common nested wrappers.</summary>
    private static StarQuestObjectiveDictionaries? ParseObjectiveDictionaries(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;
        var direct = ParseObjectiveDictionariesBody(element);
        if (direct != null) return direct;
        foreach (var wrap in new[] { "Dictionaries", "ObjectiveDictionaries", "QuestObjectiveDictionaries", "QuestObjectiveDictionary", "objectiveDictionaries", "questObjectiveDictionaries", "MetaData", "metaData" })
        {
            if (!TryGetProperty(element, wrap, out var nested) || nested.ValueKind != JsonValueKind.Object)
                continue;
            var inner = ParseObjectiveDictionariesBody(nested);
            if (inner != null) return inner;
            if ((TryGetProperty(nested, "MapMetaData", out var mapMeta) || TryGetProperty(nested, "mapMetaData", out mapMeta)) && mapMeta.ValueKind == JsonValueKind.Object)
            {
                inner = ParseObjectiveDictionariesBody(mapMeta);
                if (inner != null) return inner;
            }
        }
        return null;
    }

}
