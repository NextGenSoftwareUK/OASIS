using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

public static class DelegatedStorageIdentityConformance
{
    public static async Task RunAsync(IOASISStorageProvider provider, ProviderType outer, ProviderType inner, bool sync)
    {
        static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Check(IHolonBase record)
        {
            Require(record.ProviderUniqueStorageKey != null && record.ProviderUniqueStorageKey.TryGetValue(outer, out var key)
                && !string.IsNullOrWhiteSpace(key), "Missing composing-provider storage key.");
            Require(!record.ProviderUniqueStorageKey.ContainsKey(inner), "Inner provider identity leaked into a fresh record.");
        }
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "key-" + Guid.NewGuid().ToString("N"), Email = Guid.NewGuid().ToString("N") + "@example.invalid" };
        var holon = new Holon { Id = Guid.NewGuid(), Name = "delegated-key" };
        try
        {
            var activated = sync ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
            Require(!activated.IsError, activated.Message);
            var savedAvatar = sync ? provider.SaveAvatar(avatar) : await provider.SaveAvatarAsync(avatar);
            Require(!savedAvatar.IsError && savedAvatar.IsSaved, savedAvatar.Message);
            Check(avatar);
            var avatarKey = avatar.ProviderUniqueStorageKey[outer];
            var loadedAvatar = sync ? provider.LoadAvatarByProviderKey(avatarKey) : await provider.LoadAvatarByProviderKeyAsync(avatarKey);
            Require(!loadedAvatar.IsError && loadedAvatar.Result?.Id == avatar.Id, loadedAvatar.Message);
            Check(loadedAvatar.Result);
            avatar.FirstName = "updated-by-outer-provider";
            var updated = sync ? provider.SaveAvatar(avatar) : await provider.SaveAvatarAsync(avatar);
            Require(!updated.IsError && updated.IsSaved, updated.Message);
            // A save may replace a transaction-signature key; always use its acknowledged key.
            avatarKey = avatar.ProviderUniqueStorageKey[outer];
            loadedAvatar = sync ? provider.LoadAvatarByProviderKey(avatarKey) : await provider.LoadAvatarByProviderKeyAsync(avatarKey);
            Require(!loadedAvatar.IsError && loadedAvatar.Result?.FirstName == avatar.FirstName, loadedAvatar.Message);
            Check(loadedAvatar.Result);
            var savedHolon = sync ? provider.SaveHolon(holon, saveChildren: false) : await provider.SaveHolonAsync(holon, saveChildren: false);
            Require(!savedHolon.IsError && savedHolon.IsSaved, savedHolon.Message);
            Check(holon);
            var holonKey = holon.ProviderUniqueStorageKey[outer];
            var loadedHolon = sync ? provider.LoadHolon(holonKey, loadChildren: false) : await provider.LoadHolonAsync(holonKey, loadChildren: false);
            Require(!loadedHolon.IsError && loadedHolon.Result?.Id == holon.Id, loadedHolon.Message);
            Check(loadedHolon.Result);
            Console.WriteLine(outer + ": composing-provider keys persisted and round-tripped in " + (sync ? "sync" : "async") + " mode.");
        }
        finally
        {
            var deletedHolon = sync ? provider.DeleteHolon(holon.Id) : await provider.DeleteHolonAsync(holon.Id);
            var deletedAvatar = sync ? provider.DeleteAvatar(avatar.Id, softDelete: false) : await provider.DeleteAvatarAsync(avatar.Id, softDelete: false);
            Require(!deletedHolon.IsError && !deletedAvatar.IsError, "Delegated-key fixture cleanup failed: " + deletedHolon.Message + "; " + deletedAvatar.Message);
        }
    }
}
