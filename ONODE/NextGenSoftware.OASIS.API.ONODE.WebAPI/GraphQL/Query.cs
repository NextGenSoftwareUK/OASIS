using System;
using System.Threading.Tasks;
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
    }
}
