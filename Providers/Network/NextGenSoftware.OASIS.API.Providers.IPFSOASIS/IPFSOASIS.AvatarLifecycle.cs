using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS : IAvatarTombstoneStorageProvider
{
    public OASISResult<bool> ApplyAvatarTombstone(AvatarTombstone tombstone)
        => ApplyAvatarTombstoneAsync(tombstone).GetAwaiter().GetResult();

    public Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(AvatarTombstone tombstone)
        => DeleteIndexedAvatarAsync(row=>row.Id==tombstone.AvatarId,true,tombstone!=null,tombstone);

    private async Task<OASISResult<bool>> DeleteIndexedAvatarAsync(Func<HolonResume,bool> predicate,bool softDelete,bool validSelector=true,AvatarTombstone exact=null)
    {
        var result=new OASISResult<bool>();
        var acknowledged=new List<string>();
        try
        {
            if(!validSelector) throw new ArgumentException("A nonempty avatar deletion selector is required.");
            var snapshot=await LoadLookupToJson();
            var matches=snapshot.Where(row=>row.Value?.RecordKind=="avatar" && predicate(row.Value)).Take(2).ToList();
            if(matches.Count>1) throw new StorageIdentityAmbiguousException();
            if(matches.Count==0) return new OASISResult<bool> {IsError=true,ErrorCount=1,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="No IPFS avatar matches the deletion selector."};
            var selected=matches[0]; var id=selected.Value.Id;
            var details=snapshot.Where(row=>row.Value?.RecordKind=="avatar-detail" && row.Value.Id==id).Take(2).ToList();
            if(details.Count>1) throw new StorageIdentityAmbiguousException();
            var proposed=new Dictionary<string,HolonResume>(snapshot);
            AvatarTombstone tombstone=null;
            if(softDelete)
            {
                var avatar=await ReadPrivateRecordAsync<Avatar>(selected.Key,"avatar",id);
                var detail=details.Count==0 ? null:await ReadPrivateRecordAsync<AvatarDetail>(details[0].Key,"avatar-detail",id);
                if(avatar.DeletedDate!=default && (avatar.IsActive || avatar.DeletedDate.Kind!=DateTimeKind.Utc))
                    throw new StorageLifecycleConflictException("Existing IPFS avatar deletion timestamp conflicts with its active/UTC state; reconcile before deletion.");
                if(detail!=null && detail.DeletedDate!=default && (detail.IsActive || detail.DeletedDate.Kind!=DateTimeKind.Utc))
                    throw new StorageLifecycleConflictException("Existing IPFS detail deletion timestamp conflicts with its active/UTC state; reconcile before deletion.");
                if(avatar.DeletedDate!=default && !avatar.IsActive)
                {
                    if(detail!=null && (detail.IsActive || detail.DeletedDate!=avatar.DeletedDate || detail.DeletedByAvatarId!=avatar.DeletedByAvatarId))
                        throw new StorageLifecycleConflictException("Existing IPFS avatar/detail lifecycle states disagree; reconcile before deletion.");
                    tombstone=new AvatarTombstone(id,avatar.DeletedDate,avatar.DeletedByAvatarId);
                    if(exact!=null && (exact.DeletedAtUtc!=tombstone.DeletedAtUtc || exact.DeletedByAvatarId!=tombstone.DeletedByAvatarId))
                        throw new StorageLifecycleConflictException("Committed IPFS tombstone conflicts with the requested replica state.");
                    result.Result=true; AvatarTombstoneEvidence.Attach(result,tombstone); return result;
                }
                if(detail!=null && detail.DeletedDate!=default)
                    throw new StorageLifecycleConflictException("Existing IPFS detail tombstone conflicts with active avatar lifecycle.");
                tombstone=exact ?? new AvatarTombstone(id,DateTime.UtcNow,AvatarManager.LoggedInAvatar?.Id ?? Guid.Empty);
                async Task Prepare(IHolonBase entity,string previous,HolonResume entry)
                {
                    entity.DeletedDate=tombstone.DeletedAtUtc; entity.DeletedByAvatarId=tombstone.DeletedByAvatarId;
                    entity.IsActive=false;
                    entity.PreviousVersionProviderUniqueStorageKey ??= new();
                    entity.PreviousVersionProviderUniqueStorageKey[Core.Enums.ProviderType.IPFSOASIS]=previous;
                    var json=ProtectRecord(entry.RecordKind,id,OasisJson.Serialize(entity));
                    var node=await IPFSClient.FileSystem.AddTextAsync(json);
                    var cid=node.Id.ToString(); acknowledged.Add(cid);
                    var keys=new Dictionary<Core.Enums.ProviderType,string>(entry.ProviderUniqueStorageKey);
                    keys[Core.Enums.ProviderType.IPFSOASIS]=cid;
                    proposed.Remove(previous);
                    proposed.Add(cid,new HolonResume {Id=id,RecordKind=entry.RecordKind,login=entry.login,email=entry.email,
                        ParentHolonId=entry.ParentHolonId,HolonType=entry.HolonType,ProviderUniqueStorageKey=keys});
                }
                await Prepare(avatar,selected.Key,selected.Value);
                if(detail!=null) await Prepare(detail,details[0].Key,details[0].Value);
            }
            else
            {
                // Logical removal from the current private root; immutable historical CIDs are not erased.
                proposed.Remove(selected.Key);
                foreach(var detail in details) proposed.Remove(detail.Key);
            }
            await SaveLookupToFile(proposed);
            _idLookup=proposed;
            result.Result=true; result.IsSaved=true;
            if(tombstone!=null) AvatarTombstoneEvidence.Attach(result,tombstone);
        }
        catch(Exception cause)
        {
            var failure=acknowledged.Count>0 && cause is not IPFSRecordPublicationException
                ? new IPFSRecordPublicationException(acknowledged,cause):cause;
            OASISErrorHandling.HandleError(ref result,"IPFS avatar deletion failed: "+cause.Message,failure);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
            if(cause is StorageLifecycleConflictException) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }
}
