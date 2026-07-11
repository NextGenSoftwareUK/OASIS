using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.MCP.Server.Tools
{
    /// <summary>
    /// WEB4 (OASIS API/ONODE) - specific, typed, in-process MCP tools wrapping the highest-value manager methods
    /// (Avatar, Karma, Holon CRUD, NFT, Wallet) directly, verified against the actual ONODE/API.Core source.
    /// Everything else on the WEB4 REST API (e.g. NFT minting with its nested Web3 request list, geo-NFTs,
    /// SCMS) remains reachable via the web4_request generic passthrough in Web4Web5Tools.cs.
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

        [McpServerTool(Name = "web4_avatar_load_by_id"), Description("WEB4: loads an avatar by its id.")]
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

        // ── KARMA ───────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_karma_get"), Description("WEB4: gets an avatar's current karma total.")]
        public static async Task<string> KarmaGet(string avatarId)
        {
            var result = await KarmaManager.Instance.GetKarmaAsync(Guid.Parse(avatarId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_karma_add"), Description("WEB4: adds karma to an avatar.")]
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

        [McpServerTool(Name = "web4_karma_get_history"), Description("WEB4: gets an avatar's karma transaction history.")]
        public static async Task<string> KarmaGetHistory(string avatarId, int limit = 50, int offset = 0)
        {
            var result = await KarmaManager.Instance.GetKarmaHistoryAsync(Guid.Parse(avatarId), limit, offset);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_karma_transfer"), Description("WEB4: transfers karma from one avatar to another.")]
        public static async Task<string> KarmaTransfer(string fromAvatarId, string toAvatarId, long amount, string? description = null)
        {
            var result = await KarmaManager.Instance.TransferKarmaAsync(Guid.Parse(fromAvatarId), Guid.Parse(toAvatarId), amount, description);
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
            // Holon is a deep, self-referential OASIS Core type (parent/children graphs) that MCP's automatic
            // JSON-schema generation cannot reflect over without infinite recursion - bound as a raw JSON string
            // and deserialized manually instead of as a typed tool parameter.
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

        [McpServerTool(Name = "web4_holon_search"), Description("WEB4 COSMIC ORM: searches holons by a free-text search term.")]
        public static async Task<string> HolonSearch(string searchTerm, string avatarId, string holonType = "All")
        {
            HolonManager manager = new HolonManager(Provider);
            var result = await manager.SearchHolonsAsync(searchTerm, Guid.Parse(avatarId), holonType: Enum.Parse<HolonType>(holonType, true));
            return JsonSerializer.Serialize(result);
        }

        // ── NFT ─────────────────────────────────────────────────────────────

        [McpServerTool(Name = "web4_nft_load"), Description("WEB4: loads a Web4 NFT by id.")]
        public static async Task<string> NftLoad(string nftId)
        {
            NFTManager manager = new NFTManager(Guid.Empty);
            var result = await manager.LoadWeb4NftAsync(Guid.Parse(nftId));
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_nft_load_all_for_avatar"), Description("WEB4: loads every Web4 NFT minted by/for an avatar.")]
        public static async Task<string> NftLoadAllForAvatar(string avatarId)
        {
            NFTManager manager = new NFTManager(Guid.Parse(avatarId));
            var result = await manager.LoadAllWeb4NFTsForAvatarAsync(Guid.Parse(avatarId));
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

        [McpServerTool(Name = "web4_wallet_load_provider_wallets"), Description("WEB4: loads every provider wallet for an avatar, grouped by provider type.")]
        public static async Task<string> WalletLoadProviderWallets(string avatarId, bool showOnlyDefault = false)
        {
            WalletManager manager = new WalletManager(Provider);
            var result = await manager.LoadProviderWalletsForAvatarByIdAsync(Guid.Parse(avatarId), showOnlyDefault);
            return JsonSerializer.Serialize(result);
        }

        [McpServerTool(Name = "web4_wallet_create"), Description("WEB4: creates a new provider wallet for an avatar.")]
        public static async Task<string> WalletCreate(string avatarId, string name, string description, string walletProviderType, bool generateKeyPair = true, bool isDefaultWallet = false)
        {
            WalletManager manager = new WalletManager(Provider);
            var result = await manager.CreateWalletForAvatarByIdAsync(Guid.Parse(avatarId), name, description, Enum.Parse<ProviderType>(walletProviderType, true), generateKeyPair, isDefaultWallet);
            return JsonSerializer.Serialize(result);
        }
    }
}
