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

}
