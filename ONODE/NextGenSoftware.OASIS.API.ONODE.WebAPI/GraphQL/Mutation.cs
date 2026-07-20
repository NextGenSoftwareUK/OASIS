using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

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
    }
}
