using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!_isActivated)
                await _sdk.ProbeAsync();

            _isActivated = true;
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "NEAR provider activated through the official near-api-js SDK.";
        }
        catch (Exception ex)
        {
            _isActivated = false;
            IsProviderActivated = false;
            OASISErrorHandling.HandleError(ref result, $"Error activating NEAR provider: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        _isActivated = false;
        IsProviderActivated = false;
        return Task.FromResult(new OASISResult<bool>(true) { Message = "NEAR provider deactivated successfully." });
    }

    public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

    public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0) => LoadAvatarRecordAsync(id);
    public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        if (Guid.TryParse(providerKey, out var id))
            return await LoadAvatarRecordAsync(id);

        var all = await LoadAllAvatarRecordsAsync();
        if (all.IsError) return new OASISResult<IAvatar>(null) { IsError = true, Message = all.Message, Exception = all.Exception };
        var avatar = all.Result?.FirstOrDefault(item =>
            item.ProviderUniqueStorageKey != null &&
            item.ProviderUniqueStorageKey.TryGetValue(Core.Enums.ProviderType.NEAROASIS, out var key) &&
            string.Equals(key, providerKey, StringComparison.Ordinal));
        return new OASISResult<IAvatar>(avatar)
        {
            Message = avatar == null ? $"Avatar with NEAR provider key '{providerKey}' was not found." : "Avatar loaded from NEAR contract storage."
        };
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) =>
        LoadAvatarByProviderKeyAsync(providerKey, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
    {
        var all = await LoadAllAvatarRecordsAsync();
        if (all.IsError) return new OASISResult<IAvatar>(null) { IsError = true, Message = all.Message, Exception = all.Exception };
        var avatar = all.Result?.FirstOrDefault(item => string.Equals(item.Email, avatarEmail, StringComparison.OrdinalIgnoreCase));
        return new OASISResult<IAvatar>(avatar) { Message = avatar == null ? $"Avatar email '{avatarEmail}' was not found on NEAR." : "Avatar loaded from NEAR contract storage." };
    }

    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) =>
        LoadAvatarByEmailAsync(avatarEmail, version).GetAwaiter().GetResult();

    public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var all = await LoadAllAvatarRecordsAsync();
        if (all.IsError) return new OASISResult<IAvatar>(null) { IsError = true, Message = all.Message, Exception = all.Exception };
        var avatar = all.Result?.FirstOrDefault(item => string.Equals(item.Username, avatarUsername, StringComparison.OrdinalIgnoreCase));
        return new OASISResult<IAvatar>(avatar) { Message = avatar == null ? $"Avatar username '{avatarUsername}' was not found on NEAR." : "Avatar loaded from NEAR contract storage." };
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) =>
        LoadAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

    public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar) => SaveAvatarRecordAsync(avatar);
    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).GetAwaiter().GetResult();
    public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) => DeleteAvatarRecordAsync(id, softDelete);
    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).GetAwaiter().GetResult();
    public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) => LoadAllAvatarRecordsAsync();
    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).GetAwaiter().GetResult();
}
