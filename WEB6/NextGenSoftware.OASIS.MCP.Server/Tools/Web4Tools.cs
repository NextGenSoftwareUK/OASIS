using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>
    /// WEB4 (OASIS API/ONODE) typed in-process MCP tools covering Avatar, Karma, Holon CRUD, NFT (mint/transfer/
    /// geo-NFT/collections), and Wallet operations — verified against the actual manager source in ONODE/API.Core.
    /// No generic HTTP passthrough: every operation is named and typed so the agent knows exactly what's available.
    /// </summary>
    [McpServerToolType]
    public static class Web4Tools
    {
        private static IOASISStorageProvider Provider => ProviderManager.Instance.CurrentStorageProvider;

        // ── AVATAR ──────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_avatar_register"), Description("WEB4: registers a new OASIS avatar.")]
        public static async Task<string> AvatarRegister(string avatarTitle, string firstName, string lastName, string email, string password, string username, string avatarType = "User")
        {
            var result = await AvatarManager.Instance.RegisterAsync(avatarTitle, firstName, lastName, email, password, username, Enum.Parse<AvatarType>(avatarType, true), OASISType.OASISAPIREST);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_authenticate"), Description("WEB4: authenticates an avatar by username/password and returns the avatar (including its JWT token) on success.")]
        public static async Task<string> AvatarAuthenticate(string username, string password, string ipAddress = "127.0.0.1")
        {
            var result = await AvatarManager.Instance.AuthenticateAsync(username, password, ipAddress);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_load_by_id"), Description("WEB4: loads an avatar by its GUID id.")]
        public static async Task<string> AvatarLoadById(string avatarId)
        {
            var result = await AvatarManager.Instance.LoadAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_load_by_email"), Description("WEB4: loads an avatar by its email address.")]
        public static async Task<string> AvatarLoadByEmail(string email)
        {
            var result = await AvatarManager.Instance.LoadAvatarByEmailAsync(email);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_load_by_username"), Description("WEB4: loads an avatar by its username.")]
        public static async Task<string> AvatarLoadByUsername(string username)
        {
            var result = await AvatarManager.Instance.LoadAvatarAsync(username);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_load_all"), Description("WEB4: loads every avatar registered in the OASIS network (admin/server-side use).")]
        public static async Task<string> AvatarLoadAll()
        {
            var result = await AvatarManager.Instance.LoadAllAvatarsAsync();
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_save"), Description("WEB4: saves (creates or updates) an avatar. avatarJson should be a JSON object with fields: Id (omit for create), Username, Email, FirstName, LastName, Password, AvatarType, etc.")]
        public static async Task<string> AvatarSave(string avatarJson)
        {
            Avatar? avatar = JsonSerializer.Deserialize<Avatar>(avatarJson);
            if (avatar == null)
                return JsonSerializer.Serialize(new { error = true, message = "avatarJson did not deserialize to a valid Avatar." });
            var result = await AvatarManager.Instance.SaveAvatarAsync(avatar);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_delete"), Description("WEB4: deletes (soft-deletes by default) an avatar by id.")]
        public static async Task<string> AvatarDelete(string avatarId, bool softDelete = true)
        {
            var result = await AvatarManager.Instance.DeleteAvatarAsync(Guid.Parse(avatarId), softDelete);
            return JsonSerializer.Serialize(result);
        }

        // ── KARMA ───────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_karma_get"), Description("WEB4: gets an avatar's current karma total.")]
        public static async Task<string> KarmaGet(string avatarId)
        {
            var result = await KarmaManager.Instance.GetKarmaAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_karma_add"), Description("WEB4: adds karma to an avatar. sourceType is a KarmaSourceType enum value (e.g. 'Action', 'Event', 'API').")]
        public static async Task<string> KarmaAdd(string avatarId, long amount, string sourceType, string? description = null)
        {
            var result = await KarmaManager.Instance.AddKarmaAsync(Guid.Parse(avatarId), amount, Enum.Parse<KarmaSourceType>(sourceType, true), description);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_karma_deduct"), Description("WEB4: deducts karma from an avatar.")]
        public static async Task<string> KarmaDeduct(string avatarId, long amount, string sourceType, string? description = null)
        {
            var result = await KarmaManager.Instance.DeductKarmaAsync(Guid.Parse(avatarId), amount, Enum.Parse<KarmaSourceType>(sourceType, true), description);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_karma_transfer"), Description("WEB4: transfers karma from one avatar to another.")]
        public static async Task<string> KarmaTransfer(string fromAvatarId, string toAvatarId, long amount, string? description = null)
        {
            var result = await KarmaManager.Instance.TransferKarmaAsync(Guid.Parse(fromAvatarId), Guid.Parse(toAvatarId), amount, description);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_karma_get_history"), Description("WEB4: gets an avatar's karma transaction history.")]
        public static async Task<string> KarmaGetHistory(string avatarId, int limit = 50, int offset = 0)
        {
            var result = await KarmaManager.Instance.GetKarmaHistoryAsync(Guid.Parse(avatarId), limit, offset);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_karma_get_stats"), Description("WEB4: gets aggregate karma statistics for an avatar.")]
        public static async Task<string> KarmaGetStats(string avatarId)
        {
            var result = await KarmaManager.Instance.GetKarmaStatsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        // ── HOLON CRUD ──────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_holon_load"), Description("WEB4 COSMIC ORM: loads a holon by id.")]
        public static async Task<string> HolonLoad(string holonId)
        {
            HolonManager manager = new HolonManager(Provider);
            var result = await manager.LoadHolonAsync(Guid.Parse(holonId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_holon_save"), Description("WEB4 COSMIC ORM: saves (creates or updates) a holon. holonJson should be a JSON object matching the Holon shape (Id, Name, Description, HolonType, MetaData, etc).")]
        public static async Task<string> HolonSave(string holonJson, string avatarId)
        {
            Holon? holon = JsonSerializer.Deserialize<Holon>(holonJson);
            if (holon == null)
                return JsonSerializer.Serialize(new { error = true, message = "holonJson did not deserialize to a valid Holon." });
            HolonManager manager = new HolonManager(Provider);
            var result = await manager.SaveHolonAsync(holon, Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_holon_delete"), Description("WEB4 COSMIC ORM: deletes (soft-deletes by default) a holon.")]
        public static async Task<string> HolonDelete(string holonId, string avatarId, bool softDelete = true)
        {
            HolonManager manager = new HolonManager(Provider);
            var result = await manager.DeleteHolonAsync(Guid.Parse(holonId), Guid.Parse(avatarId), softDelete);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_holon_search"), Description("WEB4 COSMIC ORM: searches holons by a free-text search term. holonType defaults to 'All'.")]
        public static async Task<string> HolonSearch(string searchTerm, string avatarId, string holonType = "All")
        {
            HolonManager manager = new HolonManager(Provider);
            var result = await manager.SearchHolonsAsync(searchTerm, Guid.Parse(avatarId), holonType: Enum.Parse<HolonType>(holonType, true));
            return JsonSerializer.Serialize(result);
        }

        // ── NFT ─────────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_nft_mint"), Description("WEB4: mints a new Web4 NFT. mintRequestJson fields: Title, Description, Price, NumberToMint, OnChainProvider (e.g. 'Solana'), OffChainProvider (e.g. 'IPFSOASIS'), NFTStandardType (e.g. 'Metaplex'), StoreNFTMetaDataOnChain.")]
        public static async Task<string> NftMint(string avatarId, string mintRequestJson)
        {
            MintWeb4NFTRequest? request = JsonSerializer.Deserialize<MintWeb4NFTRequest>(mintRequestJson);
            if (request == null)
                return JsonSerializer.Serialize(new { error = true, message = "mintRequestJson did not deserialize to a valid MintWeb4NFTRequest." });
            request.MintedByAvatarId = Guid.Parse(avatarId);
            NFTManager manager = new NFTManager(Guid.Parse(avatarId));
            var result = await manager.MintNftAsync(request);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_nft_send"), Description("WEB4: sends (transfers) a Web4 NFT to another wallet. sendRequestJson fields: NFTId (GUID), ToWalletAddress or ToAvatarId, optionally Message.")]
        public static async Task<string> NftSend(string avatarId, string sendRequestJson)
        {
            SendWeb4NFTRequest? request = JsonSerializer.Deserialize<SendWeb4NFTRequest>(sendRequestJson);
            if (request == null)
                return JsonSerializer.Serialize(new { error = true, message = "sendRequestJson did not deserialize to a valid SendWeb4NFTRequest." });
            NFTManager manager = new NFTManager(Guid.Parse(avatarId));
            var result = await manager.SendNFTAsync(Guid.Parse(avatarId), request);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_nft_load"), Description("WEB4: loads a Web4 NFT by its GUID id.")]
        public static async Task<string> NftLoad(string nftId)
        {
            NFTManager manager = new NFTManager(Guid.Empty);
            var result = await manager.LoadWeb4NftAsync(Guid.Parse(nftId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_nft_delete"), Description("WEB4: deletes a Web4 NFT (soft-delete by default). Set burnChildWeb3NFTs=true to also burn its on-chain tokens.")]
        public static async Task<string> NftDelete(string avatarId, string nftId, bool softDelete = true, bool burnChildWeb3NFTs = false)
        {
            NFTManager manager = new NFTManager(Guid.Parse(avatarId));
            var result = await manager.DeleteWeb4NFTAsync(Guid.Parse(avatarId), Guid.Parse(nftId), softDelete, deleteChildWeb3NFTs: true, burnChildWeb3NFTs: burnChildWeb3NFTs);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_nft_load_all_for_avatar"), Description("WEB4: loads every Web4 NFT minted by/for an avatar.")]
        public static async Task<string> NftLoadAllForAvatar(string avatarId)
        {
            NFTManager manager = new NFTManager(Guid.Parse(avatarId));
            var result = await manager.LoadAllWeb4NFTsForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_nft_collection_load"), Description("WEB4: loads a Web4 NFT collection by id, optionally pre-loading child NFT records.")]
        public static async Task<string> NftCollectionLoad(string collectionId, bool loadChildNFTs = true)
        {
            NFTManager manager = new NFTManager(Guid.Empty);
            var result = await manager.LoadWeb4NFTCollectionAsync(Guid.Parse(collectionId), loadChildNFTs);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_nft_collection_load_all_for_avatar"), Description("WEB4: loads every Web4 NFT collection owned by an avatar.")]
        public static async Task<string> NftCollectionLoadAllForAvatar(string avatarId, bool loadChildNFTs = true)
        {
            NFTManager manager = new NFTManager(Guid.Parse(avatarId));
            var result = await manager.LoadWeb4NFTCollectionsForAvatarAsync(Guid.Parse(avatarId), loadChildNFTs);
            return JsonSerializer.Serialize(result);
        }

        // ── GEO-NFT ─────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_geo_nft_load"), Description("WEB4: loads a Web4 Geo-Spatial NFT by id.")]
        public static async Task<string> GeoNftLoad(string geoNftId)
        {
            NFTManager manager = new NFTManager(Guid.Empty);
            var result = await manager.LoadWeb4GeoNftAsync(Guid.Parse(geoNftId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_geo_nft_load_all_for_avatar"), Description("WEB4: loads every Geo-Spatial NFT owned by/minted for an avatar.")]
        public static async Task<string> GeoNftLoadAllForAvatar(string avatarId)
        {
            NFTManager manager = new NFTManager(Guid.Parse(avatarId));
            var result = await manager.LoadAllWeb4GeoNFTsForAvatarAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_geo_nft_load_near_location"), Description("WEB4: finds Geo-Spatial NFTs within a radius of a coordinate. latLocation and longLocation are integer degrees × 1e6 (micro-degrees). radiusMetres is the search radius.")]
        public static async Task<string> GeoNftLoadNearLocation(long latLocation, long longLocation, int radiusMetres)
        {
            NFTManager manager = new NFTManager(Guid.Empty);
            var result = await manager.LoadAllWeb4GeoNFTsForAvatarLocationAsync(latLocation, longLocation, radiusMetres);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_geo_nft_mint_and_place"), Description("WEB4: mints a Geo-Spatial NFT and pins it to a geographic coordinate in one step. mintAndPlaceRequestJson fields: Title, Description, Lat, Long, OnChainProvider, OffChainProvider, NFTStandardType.")]
        public static async Task<string> GeoNftMintAndPlace(string avatarId, string mintAndPlaceRequestJson)
        {
            MintAndPlaceWeb4GeoSpatialNFTRequest? request = JsonSerializer.Deserialize<MintAndPlaceWeb4GeoSpatialNFTRequest>(mintAndPlaceRequestJson);
            if (request == null)
                return JsonSerializer.Serialize(new { error = true, message = "mintAndPlaceRequestJson did not deserialize to a valid MintAndPlaceWeb4GeoSpatialNFTRequest." });
            request.MintedByAvatarId = Guid.Parse(avatarId);
            NFTManager manager = new NFTManager(Guid.Parse(avatarId));
            var result = await manager.MintAndPlaceWeb4GeoNFTAsync(request);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_geo_nft_delete"), Description("WEB4: deletes a Web4 Geo-Spatial NFT (soft-delete by default). Set burnChildWeb3NFTs=true to also burn its on-chain tokens.")]
        public static async Task<string> GeoNftDelete(string avatarId, string geoNftId, bool softDelete = true, bool burnChildWeb3NFTs = false)
        {
            NFTManager manager = new NFTManager(Guid.Parse(avatarId));
            var result = await manager.DeleteWeb4GeoNFTAsync(Guid.Parse(avatarId), Guid.Parse(geoNftId), softDelete, deleteChildWeb3NFTs: true, burnChildWeb3NFTs: burnChildWeb3NFTs);
            return JsonSerializer.Serialize(result);
        }

        // ── WALLET ──────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_wallet_get_total_balance"), Description("WEB4: gets an avatar's total balance summed across every provider wallet.")]
        public static async Task<string> WalletGetTotalBalance(string avatarId)
        {
            WalletManager manager = new WalletManager(Provider);
            var result = await manager.GetTotalBalanceForAllProviderWalletsForAvatarByIdAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_wallet_load_provider_wallets"), Description("WEB4: loads every provider wallet for an avatar, grouped by provider type. Set showOnlyDefault=true to return only the default wallet per provider.")]
        public static async Task<string> WalletLoadProviderWallets(string avatarId, bool showOnlyDefault = false)
        {
            WalletManager manager = new WalletManager(Provider);
            var result = await manager.LoadProviderWalletsForAvatarByIdAsync(Guid.Parse(avatarId), showOnlyDefault);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_wallet_create"), Description("WEB4: creates a new provider wallet for an avatar. walletProviderType is a ProviderType enum value (e.g. Ethereum, Solana, EOS, Holochain).")]
        public static async Task<string> WalletCreate(string avatarId, string name, string description, string walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false)
        {
            WalletManager manager = new WalletManager(Provider);
            var result = await manager.CreateWalletForAvatarByIdAsync(Guid.Parse(avatarId), name, description, Enum.Parse<ProviderType>(walletProviderType, true), generateKeyPair, isDefaultWallet);
            return JsonSerializer.Serialize(result);
        }

        // ── AVATAR (EXTRAS) ─────────────────────────────────────────────────

        [McpServerTool(Name = "web4_avatar_verify_email"), Description("WEB4: verifies an avatar's email address using the token sent after registration.")]
        public static Task<string> AvatarVerifyEmail(string token)
        {
            var result = AvatarManager.Instance.VerifyEmail(token);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_avatar_forgot_password"), Description("WEB4: triggers the forgot-password email flow for the given avatar email address.")]
        public static async Task<string> AvatarForgotPassword(string email, string? returnUrl = null)
        {
            var result = await AvatarManager.Instance.ForgotPasswordAsync(email, returnUrl: returnUrl);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_validate_reset_token"), Description("WEB4: validates a password-reset token (checks it has not expired or been used).")]
        public static Task<string> AvatarValidateResetToken(string token)
        {
            var result = AvatarManager.Instance.ValidateResetToken(token);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_avatar_reset_password"), Description("WEB4: resets an avatar's password using a valid token issued by web4_avatar_forgot_password.")]
        public static async Task<string> AvatarResetPassword(string token, string oldPassword, string newPassword)
        {
            var result = await AvatarManager.Instance.ResetPasswordAsync(token, oldPassword, newPassword);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_load_detail_by_id"), Description("WEB4: loads the detailed avatar profile (AvatarDetail) by GUID id.")]
        public static async Task<string> AvatarLoadDetailById(string avatarId)
        {
            var result = await AvatarManager.Instance.LoadAvatarDetailAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_load_detail_by_email"), Description("WEB4: loads the detailed avatar profile by email address.")]
        public static async Task<string> AvatarLoadDetailByEmail(string email)
        {
            var result = await AvatarManager.Instance.LoadAvatarDetailByEmailAsync(email);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_load_detail_by_username"), Description("WEB4: loads the detailed avatar profile by username.")]
        public static async Task<string> AvatarLoadDetailByUsername(string username)
        {
            var result = await AvatarManager.Instance.LoadAvatarDetailByUsernameAsync(username);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_load_all_details"), Description("WEB4: loads the full detailed profile for every avatar in the OASIS network (admin/server-side use).")]
        public static async Task<string> AvatarLoadAllDetails()
        {
            var result = await AvatarManager.Instance.LoadAllAvatarDetailsAsync();
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_load_all_names"), Description("WEB4: loads a flat list of avatar display names. includeUsernames and includeIds both default to true.")]
        public static async Task<string> AvatarLoadAllNames(bool includeUsernames = true, bool includeIds = true)
        {
            var result = await AvatarManager.Instance.LoadAllAvatarNamesAsync(includeUsernames, includeIds);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_add_karma"), Description("WEB4: adds positive karma to an avatar via AvatarManager. karmaType is KarmaTypePositive (e.g. 'JoinOASIS'), sourceType is KarmaSourceType (e.g. 'API').")]
        public static async Task<string> AvatarAddKarma(string avatarId, string karmaType, string sourceType, string title, string description)
        {
            var result = await AvatarManager.Instance.AddKarmaToAvatarAsync(Guid.Parse(avatarId), Enum.Parse<KarmaTypePositive>(karmaType, true), Enum.Parse<KarmaSourceType>(sourceType, true), title, description);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_remove_karma"), Description("WEB4: removes karma from an avatar. karmaType is KarmaTypeNegative (e.g. 'Hacking'), sourceType is KarmaSourceType.")]
        public static async Task<string> AvatarRemoveKarma(string avatarId, string karmaType, string sourceType, string title, string description)
        {
            var result = await AvatarManager.Instance.RemoveKarmaFromAvatarAsync(Guid.Parse(avatarId), Enum.Parse<KarmaTypeNegative>(karmaType, true), Enum.Parse<KarmaSourceType>(sourceType, true), title, description);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_get_portrait_by_id"), Description("WEB4: retrieves an avatar's portrait image by GUID id.")]
        public static async Task<string> AvatarGetPortraitById(string avatarId)
        {
            var result = await AvatarManager.Instance.GetAvatarPortraitByIdAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_get_portrait_by_username"), Description("WEB4: retrieves an avatar's portrait image by username.")]
        public static async Task<string> AvatarGetPortraitByUsername(string username)
        {
            var result = await AvatarManager.Instance.GetAvatarPortraitByUsernameAsync(username);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_get_portrait_by_email"), Description("WEB4: retrieves an avatar's portrait image by email address.")]
        public static async Task<string> AvatarGetPortraitByEmail(string email)
        {
            var result = await AvatarManager.Instance.GetAvatarPortraitByEmailAsync(email);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_avatar_upload_portrait"), Description("WEB4: uploads or replaces an avatar portrait. Provide at least one of avatarId, username, or email. imageBase64 is the base-64-encoded image bytes.")]
        public static async Task<string> AvatarUploadPortrait(string? avatarId = null, string? username = null, string? email = null, string imageBase64 = "")
        {
            var result = await AvatarManager.Instance.UploadAvatarPortraitAsync(
                avatarId != null ? Guid.Parse(avatarId) : Guid.Empty,
                username ?? string.Empty,
                email ?? string.Empty,
                imageBase64);
            return JsonSerializer.Serialize(result);
        }

        // ── CHAT ────────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_chat_start_session"), Description("WEB4: creates a new chat session. participantIdsJson is a JSON array of GUID strings. Returns the new session id.")]
        public static async Task<string> ChatStartSession(string participantIdsJson, string? sessionName = null)
        {
            var ids = JsonSerializer.Deserialize<List<string>>(participantIdsJson)?.Select(Guid.Parse).ToList() ?? new List<Guid>();
            var result = await ChatManager.Instance.StartNewChatSessionAsync(ids, sessionName);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_chat_send_message"), Description("WEB4: sends a message into an existing chat session. sessionId is the string returned by web4_chat_start_session.")]
        public static async Task<string> ChatSendMessage(string sessionId, string avatarId, string message)
        {
            var result = await ChatManager.Instance.SendMessageAsync(sessionId, Guid.Parse(avatarId), message);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_chat_get_history"), Description("WEB4: retrieves paginated message history for a chat session.")]
        public static async Task<string> ChatGetHistory(string sessionId, int limit = 50, int offset = 0)
        {
            var result = await ChatManager.Instance.GetChatHistoryAsync(sessionId, limit, offset);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_chat_get_active_sessions"), Description("WEB4: lists all active chat sessions for an avatar.")]
        public static async Task<string> ChatGetActiveSessions(string avatarId)
        {
            var result = await ChatManager.Instance.GetActiveSessionsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_chat_end_session"), Description("WEB4: ends and closes a chat session.")]
        public static async Task<string> ChatEndSession(string sessionId, string endedByAvatarId)
        {
            var result = await ChatManager.Instance.EndChatSessionAsync(sessionId, Guid.Parse(endedByAvatarId));
            return JsonSerializer.Serialize(result);
        }

        // ── MESSAGING ───────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_message_send"), Description("WEB4: sends a direct message from one avatar to another. messageType is a MessagingType value (e.g. 'Direct', 'Broadcast').")]
        public static async Task<string> MessageSend(string fromAvatarId, string toAvatarId, string content, string messageType = "Direct")
        {
            var result = await MessagingManager.Instance.SendMessageToAvatarAsync(Guid.Parse(fromAvatarId), Guid.Parse(toAvatarId), content, Enum.Parse<MessagingType>(messageType, true));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_message_get"), Description("WEB4: retrieves messages for an avatar, newest first (paginated).")]
        public static async Task<string> MessageGet(string avatarId, int limit = 50, int offset = 0)
        {
            var result = await MessagingManager.Instance.GetMessagesAsync(Guid.Parse(avatarId), limit, offset);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_message_get_conversation"), Description("WEB4: retrieves the conversation thread between two avatars, newest first (paginated).")]
        public static async Task<string> MessageGetConversation(string avatarId, string otherAvatarId, int limit = 50, int offset = 0)
        {
            var result = await MessagingManager.Instance.GetConversationAsync(Guid.Parse(avatarId), Guid.Parse(otherAvatarId), limit, offset);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_message_mark_read"), Description("WEB4: marks a set of messages as read. messageIdsJson is a JSON array of GUID strings.")]
        public static async Task<string> MessageMarkRead(string avatarId, string messageIdsJson)
        {
            var ids = JsonSerializer.Deserialize<List<string>>(messageIdsJson)?.Select(Guid.Parse).ToList() ?? new List<Guid>();
            var result = await MessagingManager.Instance.MarkMessagesAsReadAsync(Guid.Parse(avatarId), ids);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_notification_get"), Description("WEB4: retrieves notifications for an avatar, newest first (paginated).")]
        public static async Task<string> NotificationGet(string avatarId, int limit = 20, int offset = 0)
        {
            var result = await MessagingManager.Instance.GetNotificationsAsync(Guid.Parse(avatarId), limit, offset);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_notification_mark_read"), Description("WEB4: marks a set of notifications as read. notificationIdsJson is a JSON array of GUID strings.")]
        public static async Task<string> NotificationMarkRead(string avatarId, string notificationIdsJson)
        {
            var ids = JsonSerializer.Deserialize<List<string>>(notificationIdsJson)?.Select(Guid.Parse).ToList() ?? new List<Guid>();
            var result = await MessagingManager.Instance.MarkNotificationsAsReadAsync(Guid.Parse(avatarId), ids);
            return JsonSerializer.Serialize(result);
        }

        // ── SEARCH ──────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_search"), Description("WEB4 COSMIC ORM: full cross-entity OASIS search. searchParamsJson is a JSON object matching SearchParams (fields include SearchQuery, SearchAvatars, SearchHolons, SearchAll, etc.).")]
        public static async Task<string> SearchOasis(string searchParamsJson)
        {
            var searchParams = JsonSerializer.Deserialize<SearchParams>(searchParamsJson);
            if (searchParams == null)
                return JsonSerializer.Serialize(new { error = true, message = "searchParamsJson did not deserialize to a valid SearchParams." });
            var result = await new SearchManager(Provider).SearchAsync(searchParams);
            return JsonSerializer.Serialize(result);
        }

        // ── SOCIAL ──────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_social_get_feed"), Description("WEB4: retrieves the aggregated social-media feed from all registered providers for an avatar.")]
        public static async Task<string> SocialGetFeed(string avatarId)
        {
            var result = await SocialManager.Instance.GetSocialFeedAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_social_register_provider"), Description("WEB4: registers a social-media provider (e.g. 'Twitter', 'Facebook') for an avatar with an access token. settingsJson is an optional JSON object with extra provider settings.")]
        public static async Task<string> SocialRegisterProvider(string avatarId, string providerName, string accessToken, string? settingsJson = null)
        {
            Dictionary<string, object>? settings = settingsJson != null
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(settingsJson)
                : null;
            var result = await SocialManager.Instance.RegisterSocialProviderAsync(Guid.Parse(avatarId), providerName, accessToken, settings);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_social_share_holon"), Description("WEB4: shares a holon to social media. providerIdsJson is an optional JSON array of provider id strings to share to; omit to share to all registered providers.")]
        public static async Task<string> SocialShareHolon(string avatarId, string holonId, string message, string? providerIdsJson = null)
        {
            List<string>? providerIds = providerIdsJson != null
                ? JsonSerializer.Deserialize<List<string>>(providerIdsJson)
                : null;
            var result = await SocialManager.Instance.ShareHolonAsync(Guid.Parse(avatarId), Guid.Parse(holonId), message, providerIds);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_social_get_registered_providers"), Description("WEB4: lists all social-media providers registered for an avatar.")]
        public static async Task<string> SocialGetRegisteredProviders(string avatarId)
        {
            var result = await SocialManager.Instance.GetRegisteredProvidersAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        // ── STATS ───────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_stats_get_avatar"), Description("WEB4: gets comprehensive statistics for an avatar (karma, achievements, NFTs, gifts, etc.).")]
        public static async Task<string> StatsGetAvatar(string avatarId)
        {
            var result = await StatsManager.Instance.GetAvatarStatsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_stats_get_karma"), Description("WEB4: gets karma statistics for an avatar.")]
        public static async Task<string> StatsGetKarma(string avatarId)
        {
            var result = await StatsManager.Instance.GetKarmaStatsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_stats_get_karma_history"), Description("WEB4: gets karma transaction history for an avatar. limit defaults to 50.")]
        public static async Task<string> StatsGetKarmaHistory(string avatarId, int limit = 50)
        {
            var result = await StatsManager.Instance.GetKarmaHistoryAsync(Guid.Parse(avatarId), limit);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_stats_get_gift"), Description("WEB4: gets gift (item-transfer) statistics for an avatar.")]
        public static async Task<string> StatsGetGift(string avatarId)
        {
            var result = await StatsManager.Instance.GetGiftStatsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_stats_get_chat"), Description("WEB4: gets chat activity statistics for an avatar.")]
        public static async Task<string> StatsGetChat(string avatarId)
        {
            var result = await StatsManager.Instance.GetChatStatsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_stats_get_key"), Description("WEB4: gets cryptographic-key statistics for an avatar.")]
        public static async Task<string> StatsGetKey(string avatarId)
        {
            var result = await StatsManager.Instance.GetKeyStatsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_stats_get_leaderboard"), Description("WEB4: gets leaderboard statistics for an avatar.")]
        public static async Task<string> StatsGetLeaderboard(string avatarId)
        {
            var result = await StatsManager.Instance.GetLeaderboardStatsAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_stats_get_system"), Description("WEB4: gets system-wide OASIS network statistics (total avatars, holons, NFTs, etc.).")]
        public static async Task<string> StatsGetSystem()
        {
            var result = await StatsManager.Instance.GetSystemStatsAsync();
            return JsonSerializer.Serialize(result);
        }

        // ── DATA ────────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_data_load_all_holons"), Description("WEB4 COSMIC ORM: loads every holon of a given type from the OASIS network. holonType defaults to 'All'. loadChildren and recursive control child loading.")]
        public static async Task<string> DataLoadAllHolons(string holonType = "All", bool loadChildren = true, bool recursive = true, int maxChildDepth = 0)
        {
            HolonManager manager = new HolonManager(Provider);
            var result = await manager.LoadAllHolonsAsync(Enum.Parse<HolonType>(holonType, true), loadChildren, recursive, maxChildDepth);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_data_load_holons_for_parent"), Description("WEB4 COSMIC ORM: loads all child holons of a given parent by its GUID id. holonType filters to a specific subtype.")]
        public static async Task<string> DataLoadHolonsForParent(string parentId, string holonType = "All", bool loadChildren = true, bool recursive = true, int maxChildDepth = 0)
        {
            HolonManager manager = new HolonManager(Provider);
            var result = await manager.LoadHolonsForParentAsync(Guid.Parse(parentId), Enum.Parse<HolonType>(holonType, true), loadChildren, recursive, maxChildDepth);
            return JsonSerializer.Serialize(result);
        }

        // ── KEYS ────────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_key_link_public_key_by_id"), Description("WEB4: links a provider public key to an avatar by GUID id. providerType is a ProviderType enum (e.g. 'Ethereum', 'Solana').")]
        public static Task<string> KeyLinkPublicKeyById(string walletId, string avatarId, string providerType, string providerKey, string walletAddress, string? walletAddressSegwitP2SH = null, bool showSecretRecoveryWords = false)
        {
            var km = new KeyManager(Provider);
            var result = km.LinkProviderPublicKeyToAvatarById(Guid.Parse(walletId), Guid.Parse(avatarId), Enum.Parse<ProviderType>(providerType, true), providerKey, walletAddress, walletAddressSegwitP2SH);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_link_public_key_by_username"), Description("WEB4: links a provider public key to an avatar by username.")]
        public static Task<string> KeyLinkPublicKeyByUsername(string walletId, string username, string providerType, string providerKey, string walletAddress, string? walletAddressSegwitP2SH = null, bool showSecretRecoveryWords = false)
        {
            var km = new KeyManager(Provider);
            var result = km.LinkProviderPublicKeyToAvatarByUsername(Guid.Parse(walletId), username, Enum.Parse<ProviderType>(providerType, true), providerKey, walletAddress, walletAddressSegwitP2SH);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_link_public_key_by_email"), Description("WEB4: links a provider public key to an avatar by email address.")]
        public static Task<string> KeyLinkPublicKeyByEmail(string walletId, string email, string providerType, string providerKey, string walletAddress, string? walletAddressSegwitP2SH = null, bool showSecretRecoveryWords = false)
        {
            var km = new KeyManager(Provider);
            var result = km.LinkProviderPublicKeyToAvatarByEmail(Guid.Parse(walletId), email, Enum.Parse<ProviderType>(providerType, true), providerKey, walletAddress, walletAddressSegwitP2SH);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_link_private_key_by_id"), Description("WEB4: links a provider private key to an avatar by GUID id. Set showPrivateKey=true to return the key in the response.")]
        public static Task<string> KeyLinkPrivateKeyById(string walletId, string avatarId, string providerType, string providerPrivateKey, bool showPrivateKey = false, bool showSecretRecoveryWords = false)
        {
            var km = new KeyManager(Provider);
            var result = km.LinkProviderPrivateKeyToAvatarById(Guid.Parse(walletId), Guid.Parse(avatarId), Enum.Parse<ProviderType>(providerType, true), providerPrivateKey, showPrivateKey, showSecretRecoveryWords);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_link_private_key_by_username"), Description("WEB4: links a provider private key to an avatar by username.")]
        public static Task<string> KeyLinkPrivateKeyByUsername(string walletId, string username, string providerType, string providerPrivateKey, bool showPrivateKey = false, bool showSecretRecoveryWords = false)
        {
            var km = new KeyManager(Provider);
            var result = km.LinkProviderPrivateKeyToAvatarByUsername(Guid.Parse(walletId), username, Enum.Parse<ProviderType>(providerType, true), providerPrivateKey, showPrivateKey, showSecretRecoveryWords);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_generate_and_link_by_id"), Description("WEB4: generates a key pair with a wallet address and links the keys to an avatar by GUID id.")]
        public static Task<string> KeyGenerateAndLinkById(string avatarId, string providerType, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false)
        {
            var km = new KeyManager(Provider);
            var result = km.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(Guid.Parse(avatarId), Enum.Parse<ProviderType>(providerType, true), showPublicKey: showPublicKey, showPrivateKey: showPrivateKey, showSecretRecoveryWords: showSecretRecoveryWords);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_generate_and_link_by_username"), Description("WEB4: generates a key pair with a wallet address and links it to an avatar by username.")]
        public static Task<string> KeyGenerateAndLinkByUsername(string username, string providerType, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false)
        {
            var km = new KeyManager(Provider);
            var result = km.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByUsername(username, Enum.Parse<ProviderType>(providerType, true), showPublicKey: showPublicKey, showPrivateKey: showPrivateKey, showSecretRecoveryWords: showSecretRecoveryWords);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_generate_and_link_by_email"), Description("WEB4: generates a key pair with a wallet address and links it to an avatar by email.")]
        public static Task<string> KeyGenerateAndLinkByEmail(string email, string providerType, bool showPublicKey = true, bool showPrivateKey = false, bool showSecretRecoveryWords = false)
        {
            var km = new KeyManager(Provider);
            var result = km.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarByEmail(email, Enum.Parse<ProviderType>(providerType, true), showPublicKey: showPublicKey, showPrivateKey: showPrivateKey, showSecretRecoveryWords: showSecretRecoveryWords);
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_generate_keypair_with_wallet"), Description("WEB4: generates a standalone key pair plus a wallet address for a given provider type (does not link to any avatar).")]
        public static Task<string> KeyGenerateKeypairWithWallet(string providerType)
        {
            var km = new KeyManager(Provider);
            var result = km.GenerateKeyPairWithWalletAddress(Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_unique_storage_key_by_id"), Description("WEB4: gets the provider-specific unique storage key for an avatar by GUID id.")]
        public static Task<string> KeyGetUniqueStorageKeyById(string avatarId, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetProviderUniqueStorageKeyForAvatarById(Guid.Parse(avatarId), Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_unique_storage_key_by_username"), Description("WEB4: gets the provider-specific unique storage key for an avatar by username.")]
        public static Task<string> KeyGetUniqueStorageKeyByUsername(string username, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetProviderUniqueStorageKeyForAvatarByUsername(username, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_unique_storage_key_by_email"), Description("WEB4: gets the provider-specific unique storage key for an avatar by email.")]
        public static Task<string> KeyGetUniqueStorageKeyByEmail(string email, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetProviderUniqueStorageKeyForAvatarByEmail(email, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_public_keys_by_id"), Description("WEB4: gets provider public keys for an avatar by GUID id.")]
        public static Task<string> KeyGetPublicKeysById(string avatarId, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetProviderPublicKeysForAvatarById(Guid.Parse(avatarId), Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_public_keys_by_username"), Description("WEB4: gets provider public keys for an avatar by username.")]
        public static Task<string> KeyGetPublicKeysByUsername(string username, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetProviderPublicKeysForAvatarByUsername(username, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_public_keys_by_email"), Description("WEB4: gets provider public keys for an avatar by email.")]
        public static Task<string> KeyGetPublicKeysByEmail(string email, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetProviderPublicKeysForAvatarByEmail(email, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_private_keys_by_id"), Description("WEB4: gets provider private keys for an avatar by GUID id.")]
        public static Task<string> KeyGetPrivateKeysById(string avatarId, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetProviderPrivateKeysForAvatarById(Guid.Parse(avatarId), Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_private_keys_by_username"), Description("WEB4: gets provider private keys for an avatar by username.")]
        public static Task<string> KeyGetPrivateKeysByUsername(string username, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetProviderPrivateKeysForAvatarByUsername(username, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_all_public_keys_by_id"), Description("WEB4: gets public keys across all providers for an avatar by GUID id.")]
        public static Task<string> KeyGetAllPublicKeysById(string avatarId, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAllProviderPublicKeysForAvatarById(Guid.Parse(avatarId), Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_all_unique_storage_keys_by_id"), Description("WEB4: gets unique storage keys across all providers for an avatar by GUID id.")]
        public static Task<string> KeyGetAllUniqueStorageKeysById(string avatarId, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAllProviderUniqueStorageKeysForAvatarById(Guid.Parse(avatarId), Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_all_unique_storage_keys_by_username"), Description("WEB4: gets unique storage keys across all providers for an avatar by username.")]
        public static Task<string> KeyGetAllUniqueStorageKeysByUsername(string username, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAllProviderUniqueStorageKeysForAvatarByUsername(username, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_all_unique_storage_keys_by_email"), Description("WEB4: gets unique storage keys across all providers for an avatar by email.")]
        public static Task<string> KeyGetAllUniqueStorageKeysByEmail(string email, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAllProviderUniqueStorageKeysForAvatarByEmail(email, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_avatar_id_for_storage_key"), Description("WEB4: looks up the avatar GUID id from a provider unique storage key.")]
        public static Task<string> KeyGetAvatarIdForStorageKey(string providerKey, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAvatarIdForProviderUniqueStorageKey(providerKey, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_avatar_id_for_public_key"), Description("WEB4: looks up the avatar GUID id from a provider public key.")]
        public static Task<string> KeyGetAvatarIdForPublicKey(string providerKey, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAvatarIdForProviderPublicKey(providerKey, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_avatar_username_for_public_key"), Description("WEB4: looks up the avatar username from a provider public key.")]
        public static Task<string> KeyGetAvatarUsernameForPublicKey(string providerKey, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAvatarUsernameForProviderPublicKey(providerKey, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_avatar_email_for_public_key"), Description("WEB4: looks up the avatar email from a provider public key.")]
        public static Task<string> KeyGetAvatarEmailForPublicKey(string providerKey, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAvatarEmailForProviderPublicKey(providerKey, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_avatar_for_public_key"), Description("WEB4: loads the full avatar object for a given provider public key.")]
        public static Task<string> KeyGetAvatarForPublicKey(string providerKey, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAvatarForProviderPublicKey(providerKey, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }

        [McpServerTool(Name = "web4_key_get_avatar_for_storage_key"), Description("WEB4: loads the full avatar object for a given provider unique storage key.")]
        public static Task<string> KeyGetAvatarForStorageKey(string providerKey, string providerType = "Default")
        {
            var km = new KeyManager(Provider);
            var result = km.GetAvatarForProviderUniqueStorageKey(providerKey, Enum.Parse<ProviderType>(providerType, true));
            return Task.FromResult(JsonSerializer.Serialize(result));
        }
    }
}
