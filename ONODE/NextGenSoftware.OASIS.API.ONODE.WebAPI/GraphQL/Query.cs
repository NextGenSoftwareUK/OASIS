using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.GraphQL
{
    public class Query
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

        private static SearchManager CreateSearchManager()
        {
            var result = Task.Run(
                OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
            return new SearchManager(result.Result);
        }

        // ── Avatar ────────────────────────────────────────────────────────────────

        public async Task<IAvatar?> GetAvatarById(Guid id)
        {
            var result = await AvatarManager.LoadAvatarAsync(id);
            return result.IsError ? null : result.Result;
        }

        public async Task<IAvatar?> GetAvatarByUsername(string username)
        {
            var result = await AvatarManager.LoadAvatarAsync(username);
            return result.IsError ? null : result.Result;
        }

        public async Task<IAvatar?> GetAvatarByEmail(string email)
        {
            var result = await AvatarManager.LoadAvatarByEmailAsync(email);
            return result.IsError ? null : result.Result;
        }

        public async Task<IEnumerable<IAvatar>> GetAllAvatars()
        {
            var result = await AvatarManager.LoadAllAvatarsAsync();
            return result.IsError || result.Result == null
                ? Enumerable.Empty<IAvatar>()
                : result.Result;
        }

        public async Task<IEnumerable<string>> GetAllAvatarNames()
        {
            var result = await AvatarManager.LoadAllAvatarNamesAsync();
            return result.IsError || result.Result == null
                ? Enumerable.Empty<string>()
                : result.Result;
        }

        public async Task<IAvatarDetail?> GetAvatarDetailById(Guid id)
        {
            var result = await AvatarManager.LoadAvatarDetailAsync(id);
            return result.IsError ? null : result.Result;
        }

        public async Task<IAvatarDetail?> GetAvatarDetailByEmail(string email)
        {
            var result = await AvatarManager.LoadAvatarDetailByEmailAsync(email);
            return result.IsError ? null : result.Result;
        }

        public async Task<IAvatarDetail?> GetAvatarDetailByUsername(string username)
        {
            var result = await AvatarManager.LoadAvatarDetailByUsernameAsync(username);
            return result.IsError ? null : result.Result;
        }

        public async Task<IEnumerable<IAvatarDetail>> GetAllAvatarDetails()
        {
            var result = await AvatarManager.LoadAllAvatarDetailsAsync();
            return result.IsError || result.Result == null
                ? Enumerable.Empty<IAvatarDetail>()
                : result.Result;
        }

        // ── Karma ─────────────────────────────────────────────────────────────────

        public async Task<long> GetKarmaForAvatar(Guid avatarId)
        {
            var result = await AvatarManager.LoadAvatarDetailAsync(avatarId);
            return result.IsError ? 0 : (result.Result?.Karma ?? 0);
        }

        public IEnumerable<IKarmaAkashicRecord> GetKarmaAkashicRecords(Guid avatarId)
        {
            var result = AvatarManager.LoadAvatarDetail(avatarId);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<IKarmaAkashicRecord>();
            return result.Result.KarmaAkashicRecords ?? Enumerable.Empty<IKarmaAkashicRecord>();
        }

        public async Task<Dictionary<string, object>> GetKarmaStats(Guid avatarId)
        {
            var result = await KarmaManager.Instance.GetKarmaStatsAsync(avatarId);
            return result.IsError || result.Result == null ? new Dictionary<string, object>() : result.Result;
        }

        public async Task<IEnumerable<KarmaTransaction>> GetKarmaHistory(Guid avatarId, int limit = 50, int offset = 0)
        {
            var result = await KarmaManager.Instance.GetKarmaHistoryAsync(avatarId, limit, offset);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<KarmaTransaction>()
                : result.Result;
        }

        // ── Holon ─────────────────────────────────────────────────────────────────

        public async Task<IHolon?> GetHolonById(Guid id)
        {
            var manager = CreateHolonManager();
            var result = await manager.LoadHolonAsync(id);
            return result.IsError ? null : result.Result;
        }

        public async Task<IEnumerable<IHolon>> GetAllHolons(string holonType = "All")
        {
            var manager = CreateHolonManager();
            var ht = Enum.TryParse<HolonType>(holonType, true, out var parsed) ? parsed : HolonType.All;
            var result = await manager.LoadAllHolonsAsync(ht);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<IHolon>()
                : result.Result;
        }

        public async Task<IEnumerable<IHolon>> GetHolonsForParent(Guid parentId, string holonType = "All")
        {
            var manager = CreateHolonManager();
            var ht = Enum.TryParse<HolonType>(holonType, true, out var parsed) ? parsed : HolonType.All;
            var result = await manager.LoadHolonsForParentAsync(parentId, ht);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<IHolon>()
                : result.Result;
        }

        // ── Search ────────────────────────────────────────────────────────────────

        public async Task<ISearchResults?> Search(Guid avatarId, bool searchOnlyForCurrentAvatar = false)
        {
            var manager = CreateSearchManager();
            var searchParams = new SearchParams
            {
                AvatarId = avatarId,
                SearchOnlyForCurrentAvatar = searchOnlyForCurrentAvatar,
            };
            var result = await manager.SearchAsync(searchParams);
            return result.IsError ? null : result.Result;
        }

        // ── Social ────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<SocialPost>> GetSocialFeed(Guid avatarId)
        {
            var result = await SocialManager.Instance.GetSocialFeedAsync(avatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<SocialPost>()
                : result.Result;
        }

        public async Task<IEnumerable<SocialProvider>> GetRegisteredSocialProviders(Guid avatarId)
        {
            var result = await SocialManager.Instance.GetRegisteredProvidersAsync(avatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<SocialProvider>()
                : result.Result;
        }

        // ── Clan ──────────────────────────────────────────────────────────────────

        public async Task<IClan?> GetClan(Guid clanId)
        {
            var result = await ClanManager.Instance.LoadClanAsync(clanId);
            return result.IsError ? null : result.Result;
        }

        public async Task<IClan?> GetClanByName(string name)
        {
            var result = await ClanManager.Instance.LoadClanByNameAsync(name);
            return result.IsError ? null : result.Result;
        }

        public async Task<IEnumerable<IClan>> GetAllClans(Guid? ownerAvatarId = null)
        {
            var result = await ClanManager.Instance.ListClansAsync(ownerAvatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<IClan>()
                : result.Result;
        }

        public async Task<IEnumerable<Guid>> GetClanMembers(Guid clanId)
        {
            var result = await ClanManager.Instance.GetClanMembersAsync(clanId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<Guid>()
                : result.Result;
        }

        // ── Chat ──────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<ChatSession>> GetActiveChatSessions(Guid avatarId)
        {
            var result = await ChatManager.Instance.GetActiveSessionsAsync(avatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<ChatSession>()
                : result.Result;
        }

        public async Task<IEnumerable<ChatMessage>> GetChatHistory(string sessionId, int limit = 50, int offset = 0)
        {
            var result = await ChatManager.Instance.GetChatHistoryAsync(sessionId, limit, offset);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<ChatMessage>()
                : result.Result;
        }

        public async Task<Dictionary<string, object>> GetChatStats(Guid avatarId)
        {
            var result = await ChatManager.Instance.GetChatStatsAsync(avatarId);
            return result.IsError || result.Result == null ? new Dictionary<string, object>() : result.Result;
        }

        // ── Messaging ─────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Message>> GetMessages(Guid avatarId, int limit = 50, int offset = 0)
        {
            var result = await MessagingManager.Instance.GetMessagesAsync(avatarId, limit, offset);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<Message>()
                : result.Result;
        }

        public async Task<IEnumerable<Message>> GetConversation(Guid avatarId, Guid otherAvatarId, int limit = 50, int offset = 0)
        {
            var result = await MessagingManager.Instance.GetConversationAsync(avatarId, otherAvatarId, limit, offset);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<Message>()
                : result.Result;
        }

        public async Task<IEnumerable<Notification>> GetNotifications(Guid avatarId, int limit = 20, int offset = 0)
        {
            var result = await MessagingManager.Instance.GetNotificationsAsync(avatarId, limit, offset);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<Notification>()
                : result.Result;
        }

        // ── Files ─────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<StoredFile>> GetAllFiles(Guid avatarId)
        {
            var result = await FilesManager.Instance.GetAllFilesForAvatarAsync(avatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<StoredFile>()
                : result.Result;
        }

        public async Task<StoredFile?> GetFileMetadata(Guid avatarId, Guid fileId)
        {
            var result = await FilesManager.Instance.GetFileMetadataAsync(avatarId, fileId);
            return result.IsError ? null : result.Result;
        }

        // ── Gifts ─────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Gift>> GetAllGifts(Guid avatarId)
        {
            var result = await GiftsManager.Instance.GetAllGiftsAsync(avatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<Gift>()
                : result.Result;
        }

        public async Task<IEnumerable<GiftTransaction>> GetGiftHistory(Guid avatarId, int limit = 50, int offset = 0)
        {
            var result = await GiftsManager.Instance.GetGiftHistoryAsync(avatarId, limit, offset);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<GiftTransaction>()
                : result.Result;
        }

        public async Task<Dictionary<string, object>> GetGiftStats(Guid avatarId)
        {
            var result = await GiftsManager.Instance.GetGiftStatsAsync(avatarId);
            return result.IsError || result.Result == null ? new Dictionary<string, object>() : result.Result;
        }

        // ── Eggs ──────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<Egg>> GetAllEggs(Guid avatarId)
        {
            var result = await EggsManager.Instance.GetAllEggsAsync(avatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<Egg>()
                : result.Result;
        }

        public async Task<IEnumerable<EggQuest>> GetCurrentEggQuests(Guid avatarId)
        {
            var result = await EggsManager.Instance.GetCurrentEggQuestsAsync(avatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<EggQuest>()
                : result.Result;
        }

        public async Task<IEnumerable<EggQuestLeaderboard>> GetEggQuestLeaderboard(Guid avatarId)
        {
            var result = await EggsManager.Instance.GetCurrentEggQuestLeaderboardAsync(avatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<EggQuestLeaderboard>()
                : result.Result;
        }

        // ── Video ─────────────────────────────────────────────────────────────────

        public async Task<IEnumerable<VideoCall>> GetActiveCalls(Guid avatarId)
        {
            var result = await VideoManager.Instance.GetActiveCallsAsync(avatarId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<VideoCall>()
                : result.Result;
        }

        public async Task<IEnumerable<VideoParticipant>> GetCallParticipants(string callId)
        {
            var result = await VideoManager.Instance.GetCallParticipantsAsync(callId);
            return result.IsError || result.Result == null
                ? Enumerable.Empty<VideoParticipant>()
                : result.Result;
        }

        // ── Stats ─────────────────────────────────────────────────────────────────

        public async Task<Dictionary<string, object>> GetAvatarStats(Guid avatarId)
        {
            var result = await StatsManager.Instance.GetAvatarStatsAsync(avatarId);
            return result.IsError || result.Result == null ? new Dictionary<string, object>() : result.Result;
        }

        public async Task<Dictionary<string, object>> GetLeaderboardStats(Guid avatarId)
        {
            var result = await StatsManager.Instance.GetLeaderboardStatsAsync(avatarId);
            return result.IsError || result.Result == null ? new Dictionary<string, object>() : result.Result;
        }

        public async Task<Dictionary<string, object>> GetKeyStats(Guid avatarId)
        {
            var result = await StatsManager.Instance.GetKeyStatsAsync(avatarId);
            return result.IsError || result.Result == null ? new Dictionary<string, object>() : result.Result;
        }

        public async Task<Dictionary<string, object>> GetSystemStats()
        {
            var result = await StatsManager.Instance.GetSystemStatsAsync();
            return result.IsError || result.Result == null ? new Dictionary<string, object>() : result.Result;
        }

        // ── Keys ──────────────────────────────────────────────────────────────────

        public string? GetProviderUniqueStorageKey(Guid avatarId, string providerType = "Default")
        {
            var manager = CreateKeyManager();
            var pt = Enum.TryParse<ProviderType>(providerType, true, out var parsed) ? parsed : ProviderType.Default;
            var result = manager.GetProviderUniqueStorageKeyForAvatarById(avatarId, pt);
            return result.IsError ? null : result.Result;
        }

        public IEnumerable<string> GetProviderPublicKeys(Guid avatarId, string providerType = "Default")
        {
            var manager = CreateKeyManager();
            var pt = Enum.TryParse<ProviderType>(providerType, true, out var parsed) ? parsed : ProviderType.Default;
            var result = manager.GetProviderPublicKeysForAvatarById(avatarId, pt);
            return result.IsError || result.Result == null ? Enumerable.Empty<string>() : result.Result;
        }
    }
}
