using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS;

public partial class NEAROASIS
{
    public bool IsVersionControlEnabled { get; set; }

    private static T PrepareRecordSnapshot<T>(IHolonBase source) where T : class, IHolonBase
    {
        var snapshot = OasisJson.Project<T>(source);
        if (snapshot.Id == Guid.Empty) snapshot.Id = Guid.NewGuid();
        snapshot.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
        snapshot.ProviderUniqueStorageKey[Core.Enums.ProviderType.NEAROASIS] = snapshot.Id.ToString("D");
        return snapshot;
    }

    private static void PublishRecordStamp(IHolonBase source, IHolonBase stored,Guid originalId,string transactionId)
    {
        if(source.Id!=originalId)
            throw new NativeTransactionFollowupException(Core.Enums.ProviderType.NEAROASIS,transactionId,
                new InvalidOperationException("Caller identity changed after the NEAR record snapshot; acknowledged storage must be reconciled without publishing to the changed caller."));
        source.Id = stored.Id;
        source.ProviderUniqueStorageKey = stored.ProviderUniqueStorageKey;
        source.Version = stored.Version;
        source.VersionId = stored.VersionId;
        source.PreviousVersionId = stored.PreviousVersionId;
    }

    private static string HistoryPrefix(string key) => $"history:{key}:";
    private async Task<T> ReadStoredVersionAsync<T>(string key, Guid id, int version) where T : class, IHolonBase
    {
        if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
        var json = await _sdk.GetAsync(key);
        var current = string.IsNullOrWhiteSpace(json) ? null : OasisJson.Deserialize<T>(json);
        return await StoredRecordVersions.ReadAsync(id, current, version, async selected =>
        {
            var archived = await _sdk.GetAsync($"{HistoryPrefix(key)}{selected:D10}");
            return string.IsNullOrWhiteSpace(archived) ? null : OasisJson.Deserialize<T>(archived);
        });
    }

    private async Task StampStoredVersionAsync<T>(string key, T entity) where T : class, IHolonBase
    {
        var previous = await ReadStoredVersionAsync<T>(key, entity.Id, 0);
        await StoredRecordVersions.StampAsync(entity.Id, entity, previous, IsVersionControlEnabled
            ? async stored => { await _sdk.PutAsync($"{HistoryPrefix(key)}{stored.Version:D10}", OasisJson.Serialize(stored)); }
            : null);
    }

    private async Task DeleteStoredVersionsAsync(string key)
    {
        foreach (var entry in await _sdk.EntriesAsync(HistoryPrefix(key))) await _sdk.DeleteAsync(entry.Key);
    }

    private async Task<List<T>> ReadStoredVersionsAsync<T>(string prefix, int version) where T : class, IHolonBase
    {
        if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
        var records = new List<T>();
        foreach (var entry in await _sdk.EntriesAsync(prefix))
        {
            var current = OasisJson.Deserialize<T>(entry.Value);
            var selected = await StoredRecordVersions.ReadAsync(Guid.Parse(entry.Key.Substring(prefix.Length)),
                current, version, async requested =>
                {
                    var json = await _sdk.GetAsync($"{HistoryPrefix(entry.Key)}{requested:D10}");
                    return string.IsNullOrWhiteSpace(json) ? null : OasisJson.Deserialize<T>(json);
                });
            if (selected != null && !selected.IsDeleted) records.Add(selected);
        }
        return records;
    }

    private async Task<bool> EnsureSdkActiveAsync<T>(OASISResult<T> result)
    {
        if (_isActivated)
            return true;
        var activation = await ActivateProviderAsync();
        if (!activation.IsError && activation.Result)
            return true;
        OASISErrorHandling.HandleError(ref result, $"Failed to activate NEAR provider: {activation.Message}", activation.Exception);
        return false;
    }

    private async Task<OASISResult<IAvatar>> LoadAvatarRecordAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            var avatar = await ReadStoredVersionAsync<Avatar>($"avatar:{id:D}", id, version);
            if (avatar == null)
                return new OASISResult<IAvatar>(null) { Message = $"Avatar {id} was not found on NEAR." };
            if (avatar?.IsDeleted == true)
                return new OASISResult<IAvatar>(null) { Message = $"Avatar {id} is deleted on NEAR." };
            result.Result = avatar;
            result.Message = "Avatar loaded from NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar from NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<IAvatar>> SaveAvatarRecordAsync(IAvatar avatar)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (avatar == null) throw new ArgumentNullException(nameof(avatar));
            var originalId=avatar.Id;
            var snapshot = PrepareRecordSnapshot<Avatar>(avatar);
            if (!await EnsureSdkActiveAsync(result)) return result;
            await StampStoredVersionAsync($"avatar:{snapshot.Id:D}", snapshot);
            var tx = await _sdk.PutAsync($"avatar:{snapshot.Id:D}", OasisJson.Serialize(snapshot));
            PublishRecordStamp(avatar,snapshot,originalId,tx);
            result.Result = avatar;
            result.Message = $"Avatar saved to NEAR contract storage in transaction {tx}.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving avatar to NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<bool>> DeleteAvatarRecordAsync(Guid id, bool softDelete)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            if (softDelete)
            {
                var loaded = await LoadAvatarRecordAsync(id);
                if (loaded.IsError || loaded.Result == null)
                    return new OASISResult<bool>(false) { IsError = true, Message = loaded.Message };
                if (loaded.Result is not Avatar avatar)
                    throw new InvalidOperationException("The stored NEAR avatar could not be materialized as an Avatar record.");
                avatar.DeletedDate = DateTime.UtcNow;
                var saved = await SaveAvatarRecordAsync(loaded.Result);
                if (saved.IsError) return new OASISResult<bool>(false) { IsError = true, Message = saved.Message, Exception = saved.Exception };
            }
            else
            {
                await DeleteStoredVersionsAsync($"avatar:{id:D}");
                await _sdk.DeleteAsync($"avatar:{id:D}");
            }
            result.Result = true;
            result.Message = softDelete ? "Avatar tombstone saved to NEAR contract storage." : "Avatar removed from NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarRecordsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            result.Result = (await ReadStoredVersionsAsync<Avatar>("avatar:", version)).Cast<IAvatar>().ToList();
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatars from NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<IHolon>> LoadHolonRecordAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            var holon = await ReadStoredVersionAsync<Holon>($"holon:{id:D}", id, version);
            if (holon == null)
                return new OASISResult<IHolon>(null) { Message = $"Holon {id} was not found on NEAR." };
            if (holon?.IsDeleted == true)
                return new OASISResult<IHolon>(null) { Message = $"Holon {id} is deleted on NEAR." };
            result.Result = holon;
            result.Message = "Holon loaded from NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading holon from NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<IHolon>> SaveHolonRecordAsync(IHolon holon,Action<string> recordAcknowledgement=null)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (holon == null) throw new ArgumentNullException(nameof(holon));
            var originalId=holon.Id;
            var snapshot = PrepareRecordSnapshot<Holon>(holon);
            if (!await EnsureSdkActiveAsync(result)) return result;
            await StampStoredVersionAsync($"holon:{snapshot.Id:D}", snapshot);
            var tx = await _sdk.PutAsync($"holon:{snapshot.Id:D}", OasisJson.Serialize(snapshot));
            recordAcknowledgement?.Invoke(tx);
            PublishRecordStamp(holon,snapshot,originalId,tx);
            result.Result = holon;
            result.Message = $"Holon saved to NEAR contract storage in transaction {tx}.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving holon to NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<IHolon>> DeleteHolonRecordAsync(Guid id)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            var loaded = await LoadHolonRecordAsync(id);
            if (loaded.IsError || loaded.Result == null) return loaded;
            if (loaded.Result is not Holon holon)
                throw new InvalidOperationException("The stored NEAR holon could not be materialized as a Holon record.");
            holon.DeletedDate = DateTime.UtcNow;
            var saved = await SaveHolonRecordAsync(loaded.Result);
            if (saved.IsError) return saved;
            result.Result = saved.Result;
            result.Message = "Holon tombstone saved to NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting holon from NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonRecordsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            result.Result = (await ReadStoredVersionsAsync<Holon>("holon:", version)).Cast<IHolon>().ToList();
            result.Message = "Holons loaded from NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading holons from NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailRecordAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            var detail = await ReadStoredVersionAsync<AvatarDetail>($"avatar-detail:{id:D}", id, version);
            if (detail == null)
                return new OASISResult<IAvatarDetail>(null) { Message = $"Avatar detail {id} was not found on NEAR." };
            if (detail?.IsDeleted == true)
                return new OASISResult<IAvatarDetail>(null) { Message = $"Avatar detail {id} is deleted on NEAR." };
            result.Result = detail;
            result.Message = "Avatar detail loaded from NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailRecordAsync(IAvatarDetail detail)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (detail == null) throw new ArgumentNullException(nameof(detail));
            var originalId=detail.Id;
            var snapshot = PrepareRecordSnapshot<AvatarDetail>(detail);
            if (!await EnsureSdkActiveAsync(result)) return result;
            await StampStoredVersionAsync($"avatar-detail:{snapshot.Id:D}", snapshot);
            var tx = await _sdk.PutAsync($"avatar-detail:{snapshot.Id:D}", OasisJson.Serialize(snapshot));
            PublishRecordStamp(detail,snapshot,originalId,tx);
            result.Result = detail;
            result.Message = $"Avatar detail saved to NEAR contract storage in transaction {tx}.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to NEAR: {ex.Message}", ex); }
        return result;
    }

    private async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailRecordsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            if (!await EnsureSdkActiveAsync(result)) return result;
            result.Result = (await ReadStoredVersionsAsync<AvatarDetail>("avatar-detail:", version)).Cast<IAvatarDetail>().ToList();
            result.Message = "Avatar details loaded from NEAR contract storage.";
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from NEAR: {ex.Message}", ex); }
        return result;
    }
}
