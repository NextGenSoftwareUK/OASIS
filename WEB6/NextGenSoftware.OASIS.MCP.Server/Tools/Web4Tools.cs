using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
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
    }
}
