using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Services.Radix;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS;

/// <summary>
/// On-ledger storage through the OasisStorage Scrypto component (contracts/src/oasis_storage.rs), used when
/// <c>OasisBlueprintAddress</c> is configured. Entities are stored as JSON keyed by their OASIS id. The Radix provider key
/// of a stored avatar/holon is its OASIS id, so it stays stable across saves.
/// </summary>
public partial class RadixOASIS
{
    private RadixOasisStorageClient? _storage;

    private RadixOasisStorageClient Storage => _storage ??= new RadixOasisStorageClient(_config, _httpClient);

    private static OASISResult<T> LedgerResult<T>(OASISResult<string> json, Func<string, T> map, string what)
    {
        var result = new OASISResult<T>();
        if (json.IsError)
        {
            OASISErrorHandling.HandleError(ref result, json.Message, json.Exception);
            return result;
        }
        if (json.Result == null)
        {
            OASISErrorHandling.HandleError(ref result, $"{what} not found on the Radix ledger.");
            return result;
        }
        try
        {
            result.Result = map(json.Result);
            result.Message = $"{what} loaded from the Radix ledger.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{what} on the Radix ledger is not valid JSON: {ex.Message}", ex);
        }
        return result;
    }

    // ------------------------------------------------------------------------------------------------ avatars

    private async Task<OASISResult<IAvatar>> LoadAvatarOnLedgerAsync(Task<OASISResult<string>> read) =>
        LedgerResult<IAvatar>(await read.ConfigureAwait(false), json => JsonConvert.DeserializeObject<Avatar>(json)!, "Avatar");

    private async Task<OASISResult<IAvatar>> SaveAvatarOnLedgerAsync(IAvatar avatar)
    {
        var result = new OASISResult<IAvatar>();
        if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
        avatar.ProviderUniqueStorageKey[ProviderType.Value] = RadixOasisStorageClient.Key(avatar.Id);

        var saved = await Storage.SaveAvatarAsync(avatar.Id, JsonConvert.SerializeObject(avatar), avatar.Username, avatar.Email)
            .ConfigureAwait(false);
        if (saved.IsError)
        {
            OASISErrorHandling.HandleError(ref result, saved.Message, saved.Exception);
            return result;
        }
        result.Result = avatar;
        result.IsSaved = true;
        result.Message = $"Avatar saved to the Radix ledger (transaction {saved.Result}).";
        return result;
    }

    private async Task<OASISResult<bool>> DeleteAvatarOnLedgerAsync(Guid id, bool softDelete)
    {
        var result = new OASISResult<bool>();
        if (softDelete)
        {
            // Soft delete keeps the record and marks it deleted, as other providers do.
            var loaded = await LoadAvatarOnLedgerAsync(Storage.GetAvatarJsonAsync(id)).ConfigureAwait(false);
            if (loaded.IsError || loaded.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, loaded.Message);
                return result;
            }
            loaded.Result.DeletedDate = DateTime.UtcNow;
            loaded.Result.IsActive = false;
            var saved = await SaveAvatarOnLedgerAsync(loaded.Result).ConfigureAwait(false);
            result.Result = !saved.IsError;
            if (saved.IsError) OASISErrorHandling.HandleError(ref result, saved.Message);
            return result;
        }

        var deleted = await Storage.DeleteAvatarAsync(id).ConfigureAwait(false);
        result.Result = !deleted.IsError;
        if (deleted.IsError) OASISErrorHandling.HandleError(ref result, deleted.Message, deleted.Exception);
        return result;
    }

    // ----------------------------------------------------------------------------------------- avatar details

    private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailOnLedgerAsync(Guid id) =>
        LedgerResult<IAvatarDetail>(await Storage.GetAvatarDetailJsonAsync(id).ConfigureAwait(false),
            json => JsonConvert.DeserializeObject<AvatarDetail>(json)!, "AvatarDetail");

    private async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailOnLedgerAsync(IAvatarDetail avatarDetail)
    {
        var result = new OASISResult<IAvatarDetail>();
        if (avatarDetail.Id == Guid.Empty) avatarDetail.Id = Guid.NewGuid();
        avatarDetail.ProviderUniqueStorageKey[ProviderType.Value] = RadixOasisStorageClient.Key(avatarDetail.Id);

        var saved = await Storage.SaveAvatarDetailAsync(avatarDetail.Id, JsonConvert.SerializeObject(avatarDetail))
            .ConfigureAwait(false);
        if (saved.IsError)
        {
            OASISErrorHandling.HandleError(ref result, saved.Message, saved.Exception);
            return result;
        }
        result.Result = avatarDetail;
        result.IsSaved = true;
        result.Message = $"AvatarDetail saved to the Radix ledger (transaction {saved.Result}).";
        return result;
    }

    // ------------------------------------------------------------------------------------------------- holons

    private async Task<OASISResult<IHolon>> LoadHolonOnLedgerAsync(Guid id) =>
        LedgerResult<IHolon>(await Storage.GetHolonJsonAsync(id).ConfigureAwait(false),
            json => JsonConvert.DeserializeObject<Holon>(json)!, "Holon");

    /// <summary>Provider keys are OASIS ids (see class remarks), so a GUID key loads directly; others use the index.</summary>
    private async Task<OASISResult<IHolon>> LoadHolonOnLedgerAsync(string providerKey)
    {
        var byIndex = await Storage.GetHolonJsonByProviderKeyAsync(providerKey).ConfigureAwait(false);
        if (!byIndex.IsError && byIndex.Result == null && Guid.TryParse(providerKey, out var id))
            return await LoadHolonOnLedgerAsync(id).ConfigureAwait(false);
        return LedgerResult<IHolon>(byIndex, json => JsonConvert.DeserializeObject<Holon>(json)!, "Holon");
    }

    private async Task<OASISResult<IHolon>> SaveHolonOnLedgerAsync(IHolon holon)
    {
        var result = new OASISResult<IHolon>();
        if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
        var providerKey = RadixOasisStorageClient.Key(holon.Id);
        holon.ProviderUniqueStorageKey[ProviderType.Value] = providerKey;

        var saved = await Storage.SaveHolonAsync(holon.Id, JsonConvert.SerializeObject(holon), providerKey).ConfigureAwait(false);
        if (saved.IsError)
        {
            OASISErrorHandling.HandleError(ref result, saved.Message, saved.Exception);
            return result;
        }
        result.Result = holon;
        result.IsSaved = true;
        result.Message = $"Holon saved to the Radix ledger (transaction {saved.Result}).";
        return result;
    }

    private async Task<OASISResult<IHolon>> DeleteHolonOnLedgerAsync(Guid id)
    {
        var result = await LoadHolonOnLedgerAsync(id).ConfigureAwait(false);
        if (result.IsError) return result;
        var deleted = await Storage.DeleteHolonAsync(id).ConfigureAwait(false);
        if (deleted.IsError) OASISErrorHandling.HandleError(ref result, deleted.Message, deleted.Exception);
        else result.Message = $"Holon deleted from the Radix ledger (transaction {deleted.Result}).";
        return result;
    }
}
