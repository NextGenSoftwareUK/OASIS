using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL
{
    public class Mutation
    {
        private static AvatarManager AvatarManager => Program.AvatarManager;

        private static HolonManager CreateHolonManager()
        {
            var result = Task.Run(
                OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new HolonManager(result.Result);
        }

        private static KeyManager CreateKeyManager()
        {
            var result = Task.Run(
                OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new KeyManager(result.Result);
        }

        private static WalletManager CreateWalletManager()
        {
            var result = Task.Run(
                OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new WalletManager(result.Result);
        }

        // ── Avatar ────────────────────────────────────────────────────────────────

        public async Task<IAvatar?> Register(
            string title,
            string firstName,
            string lastName,
            string email,
            string password,
            string username)
        {
            var result = await AvatarManager.RegisterAsync(
                title, firstName, lastName, email, password, username,
                AvatarType.User, OASISType.OASISAPIGraphQL);
            return result.IsError ? null : result.Result;
        }

        public async Task<IAvatar?> Authenticate(string username, string password)
        {
            var result = await AvatarManager.AuthenticateAsync(username, password, "GraphQL");
            return result.IsError ? null : result.Result;
        }

        public async Task<IAvatar?> UpdateAvatar(
            Guid id,
            string? title = null,
            string? firstName = null,
            string? lastName = null,
            string? email = null,
            string? username = null)
        {
            var loadResult = await AvatarManager.LoadAvatarAsync(id);
            if (loadResult.IsError || loadResult.Result == null)
                return null;
            var avatar = loadResult.Result;
            if (title != null) avatar.Title = title;
            if (firstName != null) avatar.FirstName = firstName;
            if (lastName != null) avatar.LastName = lastName;
            if (email != null) avatar.Email = email;
            if (username != null) avatar.Username = username;
            var saveResult = await AvatarManager.SaveAvatarAsync(avatar);
            return saveResult.IsError ? null : saveResult.Result;
        }

        public async Task<bool> VerifyEmail(string token)
        {
            var result = AvatarManager.VerifyEmail(token);
            return !result.IsError;
        }

        public async Task<string?> ForgotPassword(string email)
        {
            var result = await AvatarManager.ForgotPasswordAsync(email);
            return result.IsError ? null : result.Result;
        }

        public async Task<string?> ResetPassword(string token, string oldPassword, string newPassword)
        {
            var result = await AvatarManager.ResetPasswordAsync(token, oldPassword, newPassword);
            return result.IsError ? null : result.Result;
        }

        public async Task<bool> LogoutSessions(Guid avatarId, List<string> sessionIds)
        {
            var result = await AvatarManager.LogoutAvatarSessionsAsync(avatarId, sessionIds);
            return !result.IsError;
        }

        public async Task<bool> LogoutAllSessions(Guid avatarId)
        {
            var result = await AvatarManager.LogoutAllAvatarSessionsAsync(avatarId);
            return !result.IsError;
        }

        public async Task<bool> DeleteAvatar(Guid id)
        {
            var result = await AvatarManager.DeleteAvatarAsync(id);
            return !result.IsError;
        }

        public async Task<bool> DeleteAvatarByEmail(string email)
        {
            var result = await AvatarManager.DeleteAvatarByEmailAsync(email);
            return !result.IsError;
        }

        public async Task<bool> DeleteAvatarByUsername(string username)
        {
            var result = await AvatarManager.DeleteAvatarByUsernameAsync(username);
            return !result.IsError;
        }

        // ── Holon ─────────────────────────────────────────────────────────────────

        public async Task<IHolon?> SaveHolon(
            string name,
            string description,
            string holonType)
        {
            var manager = CreateHolonManager();
            var holon = new Holon
            {
                Name = name,
                Description = description,
                HolonType = Enum.TryParse<HolonType>(holonType, true, out var ht) ? ht : HolonType.All
            };
            var result = await manager.SaveHolonAsync(holon);
            return result.IsError ? null : result.Result;
        }

        public async Task<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            var manager = CreateHolonManager();
            var result = await manager.DeleteHolonAsync(id, Guid.Empty, softDelete);
            return !result.IsError;
        }

        // ── Karma ─────────────────────────────────────────────────────────────────

        public async Task<long> AddKarmaToAvatar(
            Guid avatarId,
            string karmaTypePositive,
            string karmaSource,
            string karmaSourceTitle,
            string karmaSourceDesc)
        {
            var ktp = Enum.TryParse<KarmaTypePositive>(karmaTypePositive, true, out var k)
                ? k : KarmaTypePositive.None;
            var ks = Enum.TryParse<KarmaSourceType>(karmaSource, true, out var s)
                ? s : KarmaSourceType.API;
            var result = await KarmaManager.Instance.AddKarmaToAvatarAsync(
                avatarId, ktp, ks, karmaSourceTitle, karmaSourceDesc);
            return result.IsError ? 0 : (result.Result?.TotalKarma ?? 0);
        }

        public async Task<long> RemoveKarmaFromAvatar(
            Guid avatarId,
            string karmaTypeNegative,
            string karmaSource,
            string karmaSourceTitle,
            string karmaSourceDesc)
        {
            var ktn = Enum.TryParse<KarmaTypeNegative>(karmaTypeNegative, true, out var k)
                ? k : KarmaTypeNegative.None;
            var ks = Enum.TryParse<KarmaSourceType>(karmaSource, true, out var s)
                ? s : KarmaSourceType.API;
            var result = await KarmaManager.Instance.RemoveKarmaFromAvatarAsync(
                avatarId, ktn, ks, karmaSourceTitle, karmaSourceDesc);
            return result.IsError ? 0 : (result.Result?.TotalKarma ?? 0);
        }

        public async Task<int> AddXpToAvatar(Guid avatarId, int xpToAdd)
        {
            var detailResult = await AvatarManager.LoadAvatarDetailAsync(avatarId);
            if (detailResult.IsError || detailResult.Result == null)
                return 0;
            var detail = detailResult.Result;
            detail.XP += xpToAdd;
            var saveResult = await AvatarManager.SaveAvatarDetailAsync(detail);
            return saveResult.IsError ? 0 : (saveResult.Result?.XP ?? 0);
        }

        public bool VoteForPositiveKarmaWeighting(string karmaType, int weighting)
        {
            return true;
        }

        public bool VoteForNegativeKarmaWeighting(string karmaType, int weighting)
        {
            return true;
        }

        // ── Social ────────────────────────────────────────────────────────────────

        public async Task<bool> RegisterSocialProvider(Guid avatarId, string providerName, string accessToken)
        {
            var result = await SocialManager.Instance.RegisterSocialProviderAsync(avatarId, providerName, accessToken);
            return !result.IsError;
        }

        public async Task<bool> ShareHolon(Guid avatarId, Guid holonId, string message)
        {
            var result = await SocialManager.Instance.ShareHolonAsync(avatarId, holonId, message);
            return !result.IsError;
        }

        // ── Clan ──────────────────────────────────────────────────────────────────

        public async Task<IClan?> CreateClan(Guid ownerAvatarId, string name, string? description = null)
        {
            var result = await ClanManager.Instance.CreateClanAsync(ownerAvatarId, name, description);
            return result.IsError ? null : result.Result;
        }

        public async Task<bool> DeleteClan(Guid clanId)
        {
            var result = await ClanManager.Instance.DeleteClanAsync(clanId);
            return !result.IsError;
        }

        public async Task<bool> AddAvatarToClan(Guid clanId, Guid avatarId)
        {
            var result = await ClanManager.Instance.AddAvatarToClanAsync(clanId, avatarId);
            return !result.IsError;
        }

        public async Task<bool> RemoveAvatarFromClan(Guid clanId, Guid avatarId)
        {
            var result = await ClanManager.Instance.RemoveAvatarFromClanAsync(clanId, avatarId);
            return !result.IsError;
        }

        public async Task<bool> SendItemToClan(Guid senderAvatarId, Guid clanId, string itemName, int quantity = 1)
        {
            var result = await ClanManager.Instance.SendItemToClanAsync(senderAvatarId, clanId, itemName, quantity);
            return !result.IsError;
        }

        // ── Chat ──────────────────────────────────────────────────────────────────

        public async Task<string?> StartChatSession(List<Guid> participantIds, string? sessionName = null)
        {
            var result = await ChatManager.Instance.StartNewChatSessionAsync(participantIds, sessionName);
            return result.IsError ? null : result.Result;
        }

        public async Task<bool> SendChatMessage(string sessionId, Guid senderId, string message)
        {
            var result = await ChatManager.Instance.SendMessageAsync(sessionId, senderId, message);
            return !result.IsError;
        }

        public async Task<bool> EndChatSession(string sessionId, Guid endedById)
        {
            var result = await ChatManager.Instance.EndChatSessionAsync(sessionId, endedById);
            return !result.IsError;
        }

        // ── Messaging ─────────────────────────────────────────────────────────────

        public async Task<bool> SendMessage(Guid fromAvatarId, Guid toAvatarId, string content)
        {
            var result = await MessagingManager.Instance.SendMessageToAvatarAsync(fromAvatarId, toAvatarId, content);
            return !result.IsError;
        }

        public async Task<bool> MarkMessagesAsRead(Guid avatarId, List<Guid> messageIds)
        {
            var result = await MessagingManager.Instance.MarkMessagesAsReadAsync(avatarId, messageIds);
            return !result.IsError;
        }

        public async Task<bool> MarkNotificationsAsRead(Guid avatarId, List<Guid> notificationIds)
        {
            var result = await MessagingManager.Instance.MarkNotificationsAsReadAsync(avatarId, notificationIds);
            return !result.IsError;
        }

        // ── Files ─────────────────────────────────────────────────────────────────

        public async Task<bool> DeleteFile(Guid avatarId, Guid fileId)
        {
            var result = await FilesManager.Instance.DeleteFileAsync(avatarId, fileId);
            return !result.IsError;
        }

        // ── Gifts ─────────────────────────────────────────────────────────────────

        public async Task<Gift?> SendGift(Guid fromAvatarId, Guid toAvatarId, string giftType, string? message = null)
        {
            var gt = Enum.TryParse<GiftType>(giftType, true, out var parsed) ? parsed : GiftType.Karma;
            var result = await GiftsManager.Instance.SendGiftAsync(fromAvatarId, toAvatarId, gt, message);
            return result.IsError ? null : result.Result;
        }

        public async Task<bool> ReceiveGift(Guid avatarId, Guid giftId)
        {
            var result = await GiftsManager.Instance.ReceiveGiftAsync(avatarId, giftId);
            return !result.IsError;
        }

        public async Task<bool> OpenGift(Guid avatarId, Guid giftId)
        {
            var result = await GiftsManager.Instance.OpenGiftAsync(avatarId, giftId);
            return !result.IsError;
        }

        // ── Eggs ──────────────────────────────────────────────────────────────────

        public async Task<Egg?> HatchEgg(Guid avatarId, Guid eggId)
        {
            var result = await EggsManager.Instance.HatchEggAsync(avatarId, eggId);
            return result.IsError ? null : result.Result;
        }

        // ── Video ─────────────────────────────────────────────────────────────────

        public async Task<string?> StartVideoCall(Guid initiatorId, List<Guid> participantIds, string? callName = null)
        {
            var result = await VideoManager.Instance.StartVideoCallAsync(initiatorId, participantIds, callName: callName);
            return result.IsError ? null : result.Result;
        }

        public async Task<bool> JoinVideoCall(string callId, Guid participantId)
        {
            var result = await VideoManager.Instance.JoinVideoCallAsync(callId, participantId);
            return !result.IsError;
        }

        public async Task<bool> EndVideoCall(string callId, Guid endedById)
        {
            var result = await VideoManager.Instance.EndVideoCallAsync(callId, endedById);
            return !result.IsError;
        }

        public async Task<bool> LeaveVideoCall(string callId, Guid participantId)
        {
            var result = await VideoManager.Instance.LeaveVideoCallAsync(callId, participantId);
            return !result.IsError;
        }

        // ── Keys ──────────────────────────────────────────────────────────────────

        public bool ClearKeyCache()
        {
            var manager = CreateKeyManager();
            var result = manager.ClearCache();
            return !result.IsError;
        }

        public bool LinkProviderPublicKey(Guid walletId, Guid avatarId, string providerType, string providerKey, string walletAddress)
        {
            var manager = CreateKeyManager();
            var pt = Enum.TryParse<ProviderType>(providerType, true, out var parsed) ? parsed : ProviderType.Default;
            var result = manager.LinkProviderPublicKeyToAvatarById(walletId, avatarId, pt, providerKey, walletAddress);
            return !result.IsError;
        }

        public bool LinkProviderPrivateKey(Guid walletId, Guid avatarId, string providerType, string providerPrivateKey)
        {
            var manager = CreateKeyManager();
            var pt = Enum.TryParse<ProviderType>(providerType, true, out var parsed) ? parsed : ProviderType.Default;
            var result = manager.LinkProviderPrivateKeyToAvatarById(walletId, avatarId, pt, providerPrivateKey);
            return !result.IsError;
        }

        public bool GenerateKeyPairAndLink(Guid avatarId, string providerType)
        {
            var manager = CreateKeyManager();
            var pt = Enum.TryParse<ProviderType>(providerType, true, out var parsed) ? parsed : ProviderType.Default;
            var result = manager.GenerateKeyPairWithWalletAddressAndLinkProviderKeysToAvatarById(avatarId, pt);
            return !result.IsError;
        }

        // ── Wallet ────────────────────────────────────────────────────────────────

        public async Task<IProviderWallet?> CreateWallet(Guid avatarId, string name, string description, string walletProviderType)
        {
            var manager = CreateWalletManager();
            var pt = Enum.TryParse<ProviderType>(walletProviderType, true, out var parsed) ? parsed : ProviderType.Default;
            var result = await manager.CreateWalletForAvatarByIdAsync(avatarId, name, description, pt);
            return result.IsError ? null : result.Result;
        }

        public async Task<IProviderWallet?> CreateWalletByUsername(string username, string name, string description, string walletProviderType)
        {
            var manager = CreateWalletManager();
            var pt = Enum.TryParse<ProviderType>(walletProviderType, true, out var parsed) ? parsed : ProviderType.Default;
            var result = await manager.CreateWalletForAvatarByUsernameAsync(username, name, description, pt);
            return result.IsError ? null : result.Result;
        }

        public async Task<IProviderWallet?> CreateWalletByEmail(string email, string name, string description, string walletProviderType)
        {
            var manager = CreateWalletManager();
            var pt = Enum.TryParse<ProviderType>(walletProviderType, true, out var parsed) ? parsed : ProviderType.Default;
            var result = await manager.CreateWalletForAvatarByEmailAsync(email, name, description, pt);
            return result.IsError ? null : result.Result;
        }

        // ── Competition ───────────────────────────────────────────────────────────

        public async Task<bool> JoinTournament(Guid avatarId, Guid tournamentId)
        {
            var result = await CompetitionManager.Instance.JoinTournamentAsync(avatarId, tournamentId);
            return !result.IsError;
        }

        // ── Settings ──────────────────────────────────────────────────────────────

        public async Task<bool> UpdateHyperDriveSettings(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = await SettingsManager.Instance.UpdateHyperDriveSettingsAsync(avatarId, settings);
            return !result.IsError;
        }

        public async Task<bool> UpdateSystemSettings(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = await SettingsManager.Instance.UpdateSystemSettingsAsync(avatarId, settings);
            return !result.IsError;
        }

        public async Task<bool> UpdateSubscriptionSettings(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = await SettingsManager.Instance.UpdateSubscriptionSettingsAsync(avatarId, settings);
            return !result.IsError;
        }

        public async Task<bool> UpdateNotificationSettings(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = await SettingsManager.Instance.UpdateNotificationSettingsAsync(avatarId, settings);
            return !result.IsError;
        }

        public async Task<bool> UpdatePrivacySettings(Guid avatarId, Dictionary<string, object> settings)
        {
            var result = await SettingsManager.Instance.UpdatePrivacySettingsAsync(avatarId, settings);
            return !result.IsError;
        }

        // ── HyperDrive ────────────────────────────────────────────────────────────

        public bool UpdateHyperDriveConfig(OASISHyperDriveConfig config)
        {
            var result = OASISHyperDriveConfigManager.Instance.UpdateConfiguration(config);
            return !result.IsError;
        }

        public bool ValidateHyperDriveConfig()
        {
            var result = OASISHyperDriveConfigManager.Instance.ValidateConfiguration();
            return !result.IsError;
        }

        public bool ResetHyperDriveConfig()
        {
            var result = OASISHyperDriveConfigManager.Instance.ResetToDefaults();
            return !result.IsError;
        }

        public void ResetHyperDriveProviderMetrics(string providerType)
        {
            var pt = Enum.TryParse<ProviderType>(providerType, true, out var parsed) ? parsed : ProviderType.Default;
            PerformanceMonitor.Instance.ResetMetrics(pt);
        }

        public void ResetAllHyperDriveMetrics()
        {
            PerformanceMonitor.Instance.ResetAllMetrics();
        }

        // ── Bridge ────────────────────────────────────────────────────────────────

        public async Task<CreateBridgeOrderResponse?> CreateBridgeOrder(string fromToken, string toToken, decimal amount)
        {
            var request = new CreateBridgeOrderRequest { FromToken = fromToken, ToToken = toToken, Amount = amount };
            var result = await BridgeManager.Instance.CreateBridgeOrderAsync(request);
            return result.IsError ? null : result.Result;
        }

        public async Task<CreateBridgeOrderResponse?> CreatePrivateBridgeOrder(string fromToken, string toToken, decimal amount)
        {
            var request = new CreateBridgeOrderRequest
            {
                FromToken = fromToken,
                ToToken = toToken,
                Amount = amount,
                EnableViewingKeyAudit = true,
                RequireProofVerification = true,
            };
            var result = await BridgeManager.Instance.CreateBridgeOrderAsync(request);
            return result.IsError ? null : result.Result;
        }

        public async Task<bool> VerifyBridgeProof(string proofPayload, string proofType)
        {
            var result = await BridgeManager.Instance.VerifyProofAsync(proofPayload, proofType);
            return !result.IsError && result.Result;
        }

        // ── Seeds ─────────────────────────────────────────────────────────────────

        private static SeedsManager CreateSeedsManager(Guid avatarId)
        {
            var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new SeedsManager(result.Result, avatarId);
        }

        public async Task<SeedTransaction?> SaveSeedTransaction(Guid avatarId, string avatarUsername, int amount, string memo)
        {
            var manager = CreateSeedsManager(avatarId);
            var result = await manager.SaveSeedTransactionAsync(avatarId, avatarUsername, amount, memo);
            return result.IsError ? null : result.Result;
        }

        // ── ONET ──────────────────────────────────────────────────────────────────

        private static ONETManager CreateONETManager()
        {
            var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new ONETManager(result.Result, OASISBootLoader.OASISBootLoader.OASISDNA);
        }

        private static ONODEManager CreateONODEManager()
        {
            var result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new ONODEManager(result.Result, OASISBootLoader.OASISBootLoader.OASISDNA);
        }

        public async Task<bool> UpdateOASISDNA(OASISDNA oasisdna)
        {
            var result = await CreateONETManager().UpdateOASISDNAAsync(oasisdna);
            return !result.IsError;
        }

        public async Task<bool> ConnectToNode(string nodeId, string nodeAddress)
        {
            var result = await CreateONETManager().ConnectToNodeAsync(nodeId, nodeAddress);
            return !result.IsError;
        }

        public async Task<bool> DisconnectFromNode(string nodeId)
        {
            var result = await CreateONETManager().DisconnectFromNodeAsync(nodeId);
            return !result.IsError;
        }

        public async Task<bool> StartNetwork()
        {
            var result = await CreateONETManager().StartNetworkAsync();
            return !result.IsError;
        }

        public async Task<bool> StopNetwork()
        {
            var result = await CreateONETManager().StopNetworkAsync();
            return !result.IsError;
        }

        public async Task<bool> BroadcastNetworkMessage(string message, string messageType = "general")
        {
            var result = await CreateONETManager().BroadcastMessageAsync(message, messageType);
            return !result.IsError;
        }

        // ── ONODE ─────────────────────────────────────────────────────────────────

        public async Task<bool> StartNode()
        {
            var result = await CreateONODEManager().StartNodeAsync();
            return !result.IsError;
        }

        public async Task<bool> StopNode()
        {
            var result = await CreateONODEManager().StopNodeAsync();
            return !result.IsError;
        }

        public async Task<bool> RestartNode()
        {
            var result = await CreateONODEManager().RestartNodeAsync();
            return !result.IsError;
        }

        public async Task<bool> UpdateNodeConfig(Dictionary<string, object> config)
        {
            var result = await CreateONODEManager().UpdateNodeConfigAsync(config);
            return !result.IsError;
        }

        // EOSIO
        private static NextGenSoftware.OASIS.API.Core.Managers.KeyManager CreateKeyManagerM()
        {
            var r = System.Threading.Tasks.Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new NextGenSoftware.OASIS.API.Core.Managers.KeyManager(r.Result);
        }

        public string LinkEOSIOAccountToAvatar(Guid walletId, Guid avatarId, string accountName, string walletAddress)
        {
            var r = CreateKeyManagerM().LinkProviderPublicKeyToAvatarById(walletId, avatarId, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.EOSIOOASIS, accountName, walletAddress);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        // Holochain
        public string LinkHolochainAgentIdToAvatar(Guid walletId, Guid avatarId, string agentId, string providerType)
        {
            NextGenSoftware.OASIS.API.Core.Enums.ProviderType pt = NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Default;
            if (!string.IsNullOrWhiteSpace(providerType)) System.Enum.TryParse(providerType, true, out pt);
            var r = CreateKeyManagerM().LinkProviderPublicKeyToAvatarById(walletId, avatarId, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.HoloOASIS, agentId, null, providerToLoadAvatarFrom: pt);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        // Map
        public async Task<bool> VisitLocation(Guid avatarId, Guid locationId, string purpose)
        {
            var r = await new NextGenSoftware.OASIS.API.ONODE.Core.Managers.MapManager(avatarId).VisitLocationAsync(avatarId, locationId, purpose);
            return !r.IsError;
        }

        // OLand
        public async Task<string> PurchaseOland(string purchaseOlandRequestJson)
        {
            var req = System.Text.Json.JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.ONODE.Core.Objects.PurchaseOlandRequest>(purchaseOlandRequestJson);
            if (req == null) return null;
            var manager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.OLandManager(new NextGenSoftware.OASIS.API.ONODE.Core.Managers.NFTManager(Guid.Empty), Guid.Empty);
            var r = await manager.PurchaseOlandAsync(req);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<bool> DeleteOland(Guid olandId, Guid avatarId)
        {
            var manager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.OLandManager(new NextGenSoftware.OASIS.API.ONODE.Core.Managers.NFTManager(Guid.Empty), Guid.Empty);
            var r = await manager.DeleteOlandAsync(olandId, avatarId);
            return !r.IsError;
        }

        public async Task<bool> SaveOland(string olandJson)
        {
            var oland = System.Text.Json.JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Objects.Oland>(olandJson);
            if (oland == null) return false;
            var manager = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.OLandManager(new NextGenSoftware.OASIS.API.ONODE.Core.Managers.NFTManager(Guid.Empty), Guid.Empty);
            var r = await manager.SaveOlandAsync(oland);
            return !r.IsError;
        }

        // Share
        public async Task<bool> ShareHolonToMany(Guid holonId, string avatarIdsCsv)
        {
            var ids = avatarIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
            var r = System.Threading.Tasks.Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            var mgr = new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(r.Result);
            var holon = await mgr.LoadHolonAsync(holonId);
            if (holon == null || holon.IsError || holon.Result == null) return false;
            if (holon.Result.MetaData == null) holon.Result.MetaData = new Dictionary<string, object>();
            holon.Result.MetaData["SHARED_AVATAR_IDS"] = System.Text.Json.JsonSerializer.Serialize(ids);
            var save = await mgr.SaveHolonAsync(holon.Result, Guid.Empty);
            return save != null && !save.IsError;
        }

        // Solana
        public async Task<string> MintSolanaNft(string requestJson, [Service] NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana.ISolanaService solanaService)
        {
            var req = System.Text.Json.JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.MintWeb3NFTRequest>(requestJson);
            if (req == null) return null;
            var r = await solanaService.MintNftAsync(req);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<string> SendSolanaTransaction(string fromAccount, string toAccount, long lamports, [Service] NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana.ISolanaService solanaService)
        {
            var req = new NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests.SendTransactionRequest
            {
                FromAccount = new NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common.BaseAccountRequest { PublicKey = fromAccount },
                ToAccount = new NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Common.BaseAccountRequest { PublicKey = toAccount },
                Amount = (ulong)lamports
            };
            var r = await solanaService.SendTransaction(req);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        // NFT
        private static NextGenSoftware.OASIS.API.ONODE.Core.Managers.NFTManager CreateNFTManagerM() => new NextGenSoftware.OASIS.API.ONODE.Core.Managers.NFTManager(Guid.Empty);

        public async Task<string> CollectNft(string requestJson)
        {
            var req = System.Text.Json.JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Objects.NFT.Request.CollectGeoNFTRequest>(requestJson);
            if (req == null) return null;
            var r = await CreateNFTManagerM().CollectNFTAsync(req);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public Task<string> PlaceGeoNft(string requestJson)
        {
            return System.Threading.Tasks.Task.FromResult<string>(null); // PlaceGeoNFTAsync not yet implemented in NFTManager
        }

        public async Task<string> MintWeb4Nft(string requestJson)
        {
            var req = System.Text.Json.JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.MintWeb4NFTRequest>(requestJson);
            if (req == null) return null;
            var r = await CreateNFTManagerM().MintNftAsync(req);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<string> SendNft(string requestJson)
        {
            var req = System.Text.Json.JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.SendWeb4NFTRequest>(requestJson);
            if (req == null) return null;
            var r = await CreateNFTManagerM().SendNFTAsync(Guid.Empty, req);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        public async Task<string> ImportWeb3Nft(string requestJson)
        {
            var req = System.Text.Json.JsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.ImportWeb3NFTRequest>(requestJson);
            if (req == null) return null;
            var r = await CreateNFTManagerM().ImportWeb3NFTAsync(req);
            return r.IsError ? null : System.Text.Json.JsonSerializer.Serialize(r.Result);
        }

        // Subscription
        public Task<string> CreateCheckoutSession(string requestJson, [Service] NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.ISubscriptionService subscriptionService)
        {
            return Task.FromResult<string>(null);
        }

        public async Task<bool> TogglePayAsYouGo(string userId, bool enable, [Service] NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.ISubscriptionService subscriptionService)
        {
            await subscriptionService.SetPayAsYouGoAsync(userId, enable);
            return true;
        }

        public Task<bool> UpdateSubscriptionHyperDriveConfig(string requestJson, [Service] NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription.ISubscriptionService subscriptionService)
        {
            return Task.FromResult(false);
        }
    }
}
