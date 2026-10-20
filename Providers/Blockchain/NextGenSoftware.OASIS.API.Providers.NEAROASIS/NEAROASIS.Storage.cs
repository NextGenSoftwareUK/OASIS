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

    // Mutates only the caller's already-detached proposed snapshot. No SDK/native writes occur here.
    private static async Task<KeyValuePair<string,string>?> StageObservedRecordVersionAsync<T>(T proposed,NearSdkService.StoredSnapshot observed,bool history,
        PlannedHolonWrite plannedHolon=null) where T:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(proposed);ArgumentNullException.ThrowIfNull(observed);
        var prefix=typeof(T)==typeof(Avatar)?"avatar:":typeof(T)==typeof(AvatarDetail)?"avatar-detail:":typeof(T)==typeof(Holon)?"holon:":throw new ArgumentException("Unsupported native record kind.");
        if(proposed.Id==Guid.Empty||observed.Key!=prefix+proposed.Id.ToString("D"))throw new InvalidOperationException("Observed record key differs from proposed record identity/kind.");
        if((observed.Ciphertext==null)!=(observed.Plaintext==null))throw new InvalidOperationException("Observed ciphertext/plaintext presence differs.");
        var previous=observed.Plaintext==null?null:OasisJson.DeserializeStored<T>(observed.Plaintext);
        if(previous!=null&&(previous.Id!=proposed.Id||previous.Version<0))throw new InvalidOperationException("Observed record identity/version is invalid.");
        if(typeof(T)==typeof(Holon)&&proposed is Holon holon)
            holon.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(
                plannedHolon??new PlannedHolonWrite(holon,holon),HolonChildRoleWriteKind.SourceSave,
                previous!=null,(previous as Holon)?.ChildRoleManifestJson);
        KeyValuePair<string,string>? archive=null;
        await StoredRecordVersions.StampAsync(proposed.Id,proposed,previous,history?stored=>{
            // Version zero denotes current, not an addressable historical version. Begin history at version one.
            if(stored.Version>0)archive=new KeyValuePair<string,string>(HistoryPrefix(observed.Key)+stored.Version.ToString("D10",System.Globalization.CultureInfo.InvariantCulture),OasisJson.Serialize(stored));
            return Task.CompletedTask;
        }:null);
        return archive;
    }
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

    private async Task<string> WriteObservedRecordAsync<T>(string key,T entity,bool history,PlannedHolonWrite plannedHolon=null) where T:class,IHolonBase
    {
        var observed=await _sdk.ReadSnapshotAsync(key);
        var archive=await StageObservedRecordVersionAsync(entity,observed,history,plannedHolon);
        return await _sdk.PutRecordCheckedAsync(observed,OasisJson.Serialize(entity),archive?.Key,archive?.Value);
    }

    private static void HandleRecordSaveError<T>(ref OASISResult<T> result,string message,Exception cause)
    {
        OASISErrorHandling.HandleError(ref result,message,cause);
        if(cause is NearRecordWriteRejectedException rejected&&rejected.IsRecordConflict)
            result.ErrorCode=NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode;
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
            var history=IsVersionControlEnabled;
            var snapshot = PrepareRecordSnapshot<Avatar>(avatar);
            if (!await EnsureSdkActiveAsync(result)) return result;
            var tx=await WriteObservedRecordAsync($"avatar:{snapshot.Id:D}",snapshot,history);
            PublishRecordStamp(avatar,snapshot,originalId,tx);
            result.Result = avatar;
            result.IsSaved=true;
            result.Message = $"Avatar saved to NEAR contract storage in transaction {tx}.";
        }
        catch (Exception ex) { HandleRecordSaveError(ref result, $"Error saving avatar to NEAR: {ex.Message}", ex); }
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
                if (saved.IsError) return new OASISResult<bool>(false) { IsError = true, ErrorCode=saved.ErrorCode, Message = saved.Message, Exception = saved.Exception };
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

    private async Task<OASISResult<IHolon>> SaveHolonRecordAsync(IHolon holon,Action<string> recordAcknowledgement=null,PlannedHolonWrite plannedHolon=null)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (holon == null) throw new ArgumentNullException(nameof(holon));
            var originalId=holon.Id;
            var history=IsVersionControlEnabled;
            var snapshot = PrepareRecordSnapshot<Holon>(holon);
            if (!await EnsureSdkActiveAsync(result)) return result;
            var tx=await WriteObservedRecordAsync($"holon:{snapshot.Id:D}",snapshot,history,plannedHolon);
            recordAcknowledgement?.Invoke(tx);
            PublishRecordStamp(holon,snapshot,originalId,tx);
            if(holon is Holon source)source.ChildRoleManifestJson=snapshot.ChildRoleManifestJson;
            result.Result = holon;
            result.IsSaved=true;
            result.Message = $"Holon saved to NEAR contract storage in transaction {tx}.";
        }
        catch (Exception ex) { HandleRecordSaveError(ref result, $"Error saving holon to NEAR: {ex.Message}", ex); }
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
            var history=IsVersionControlEnabled;
            var snapshot = PrepareRecordSnapshot<AvatarDetail>(detail);
            if (!await EnsureSdkActiveAsync(result)) return result;
            var tx=await WriteObservedRecordAsync($"avatar-detail:{snapshot.Id:D}",snapshot,history);
            PublishRecordStamp(detail,snapshot,originalId,tx);
            result.Result = detail;
            result.IsSaved=true;
            result.Message = $"Avatar detail saved to NEAR contract storage in transaction {tx}.";
        }
        catch (Exception ex) { HandleRecordSaveError(ref result, $"Error saving avatar detail to NEAR: {ex.Message}", ex); }
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
