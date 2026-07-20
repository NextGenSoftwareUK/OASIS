using System;
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

        // ── Avatar mutations ──────────────────────────────────────────────────────

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

        // ── Holon mutations ───────────────────────────────────────────────────────

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
                HolonType = Enum.TryParse<HolonType>(holonType, true, out var ht)
                    ? ht
                    : HolonType.All
            };
            var result = await manager.SaveHolonAsync(holon);
            return result.IsError ? null : result.Result;
        }

        // ── Karma mutations ───────────────────────────────────────────────────────

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

        public async Task<bool> DeleteAvatar(Guid id)
        {
            var result = await AvatarManager.DeleteAvatarAsync(id);
            return !result.IsError;
        }

        // ── Holon mutations ───────────────────────────────────────────────────────

        public async Task<bool> DeleteHolon(Guid id, bool softDelete = true)
        {
            var manager = CreateHolonManager();
            var result = await manager.DeleteHolonAsync(id, Guid.Empty, softDelete);
            return !result.IsError;
        }
    }
}
