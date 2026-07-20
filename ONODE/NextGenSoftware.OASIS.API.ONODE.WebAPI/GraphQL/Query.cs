using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

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

        // ── Avatar queries ────────────────────────────────────────────────────────

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

        // ── Karma query ───────────────────────────────────────────────────────────

        public async Task<long> GetKarmaForAvatar(Guid avatarId)
        {
            var result = await AvatarManager.LoadAvatarDetailAsync(avatarId);
            return result.IsError ? 0 : (result.Result?.Karma ?? 0);
        }

        // ── Holon queries ─────────────────────────────────────────────────────────

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
    }
}
