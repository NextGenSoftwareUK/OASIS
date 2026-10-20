using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public partial class AzureStorageOASIS : IAvatarTombstoneStorageProvider
{
    public OASISResult<bool> ApplyAvatarTombstone(AvatarTombstone tombstone)
        => ApplyAvatarTombstoneAsync(tombstone).GetAwaiter().GetResult();
    public Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(AvatarTombstone tombstone)
        => tombstone==null ? Task.FromResult(new OASISResult<bool> {IsError=true,ErrorCode="HYPERDRIVE_PAYLOAD_INVALID",Message="Committed tombstone required."})
            : ApplyAvatarLifecycleAsync(tombstone.AvatarId,true,tombstone);

    private static async Task WriteLifecycleBlobAsync(BlobClient blob,ETag etag,BinaryData payload)
    {
        var condition=new BlobRequestConditions {IfMatch=etag};
        if(payload!=null)
            await ObserveBlobMutationAsync(async()=> {
                var response=await blob.UploadAsync(payload,new BlobUploadOptions {Conditions=condition});
                if(response.GetRawResponse().Status<200 || response.GetRawResponse().Status>=300 || string.IsNullOrWhiteSpace(response.Value.ETag.ToString()))
                    throw new InvalidDataException("Azure lifecycle upload lacks authoritative acknowledgement.");
                return response;
            },blob);
        else
            await ObserveBlobMutationAsync(async()=> {
                var response=await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,condition);
                if(response.Status<200 || response.Status>=300) throw new InvalidDataException("Azure lifecycle delete lacks authoritative acknowledgement.");
                return response;
            },blob);
    }

    private async Task<OASISResult<bool>> ApplyAvatarLifecycleAsync(Guid id,bool softDelete,AvatarTombstone supplied=null,Func<IAvatar,bool> selectedIdentity=null)
    {
        var result=new OASISResult<bool>();var acknowledged=new List<string>();
        try
        {
            if(id==Guid.Empty) throw new ArgumentException("Nonempty avatar identity required.");
            var profileBlob=_serviceClient.GetBlobContainerClient(_avatarContainer).GetBlobClient(id.ToString());
            var profileRead=(await profileBlob.DownloadContentAsync()).Value;
            if(string.IsNullOrWhiteSpace(profileRead.Details.ETag.ToString())) throw new InvalidDataException("Azure avatar read lacks an ETag.");
            var profile=await ReadBlobHistoryAsync<Avatar>(profileRead.Content.ToString(),0) ?? throw new InvalidDataException("Azure avatar is null.");
            if(profile.Id!=id) throw new InvalidDataException("Azure avatar identity differs from blob key.");
            if(selectedIdentity!=null&&!selectedIdentity(profile))throw new StorageLifecycleConflictException("Azure avatar identity changed after selection; refusing deletion.");
            if(supplied!=null && profile.IsDeleted && (profile.DeletedDate!=supplied.DeletedAtUtc || profile.DeletedByAvatarId!=supplied.DeletedByAvatarId))
                throw new StorageLifecycleConflictException("Azure avatar has a conflicting committed tombstone.");
            var detailBlob=_serviceClient.GetBlobContainerClient(_detailContainer).GetBlobClient(id.ToString());
            BlobDownloadResult detailRead=null;
            try {detailRead=(await detailBlob.DownloadContentAsync()).Value;}
            catch(RequestFailedException cause) when(cause.Status==404 && cause.ErrorCode=="BlobNotFound") { }
            if(detailRead!=null && string.IsNullOrWhiteSpace(detailRead.Details.ETag.ToString())) throw new InvalidDataException("Azure detail read lacks an ETag.");
            if(supplied!=null && detailRead==null) throw new StorageLifecycleConflictException("Committed tombstone requires an existing Azure detail.");
            var detail=detailRead==null ? null:await ReadBlobHistoryAsync<AvatarDetail>(detailRead.Content.ToString(),0);
            if(detailRead!=null && (detail==null || detail.Id!=id)) throw new InvalidDataException("Azure detail identity differs from blob key.");
            AvatarTombstone tombstone=null;
            if(softDelete)
            {
                var date=supplied?.DeletedAtUtc ?? (profile.IsDeleted ? profile.DeletedDate:DateTime.UtcNow);
                var actor=supplied?.DeletedByAvatarId ?? profile.DeletedByAvatarId;
                tombstone=new AvatarTombstone(id,date,actor);
                if(detail!=null && detail.IsDeleted && (detail.DeletedDate!=date || detail.DeletedByAvatarId!=actor))
                    throw new StorageLifecycleConflictException("Azure profile/detail committed tombstones conflict.");
                profile.IsActive=false;profile.DeletedDate=date;profile.DeletedByAvatarId=actor;
                if(detail!=null) {detail.IsActive=false;detail.DeletedDate=date;detail.DeletedByAvatarId=actor;}
            }
            // Materialize both payloads before the first mutation. ETags guard each
            // blob; this is deliberately NOT represented as a cross-blob transaction.
            var profilePayload=softDelete ? BinaryData.FromString((await StageBlobHistoryAsync(profile,profileRead.Content.ToString(),IsVersionControlEnabled)).ToString(Newtonsoft.Json.Formatting.None)):null;
            var detailPayload=softDelete && detail!=null ? BinaryData.FromString((await StageBlobHistoryAsync(detail,detailRead.Content.ToString(),IsVersionControlEnabled)).ToString(Newtonsoft.Json.Formatting.None)):null;
            if(softDelete)
            {
                await WriteLifecycleBlobAsync(profileBlob,profileRead.Details.ETag,profilePayload);
                acknowledged.Add(_avatarContainer+"/"+id);
                if(detailRead!=null) {await WriteLifecycleBlobAsync(detailBlob,detailRead.Details.ETag,detailPayload);acknowledged.Add(_detailContainer+"/"+id);}
            }
            else
            {
                if(detailRead!=null) {await WriteLifecycleBlobAsync(detailBlob,detailRead.Details.ETag,null);acknowledged.Add(_detailContainer+"/"+id);}
                await WriteLifecycleBlobAsync(profileBlob,profileRead.Details.ETag,null);acknowledged.Add(_avatarContainer+"/"+id);
            }
            result.Result=true;result.IsSaved=true;result.IsDeleted=true;
            if(tombstone!=null) AvatarTombstoneEvidence.Attach(result,tombstone);
        }
        catch(Exception cause)
        {
            result.IsError=true;result.Exception=acknowledged.Count>0 ? new AzureAvatarLifecycleException(id,acknowledged,cause):cause;
            result.Message=result.Exception.Message;
            if(acknowledged.Count==0)
            {
                if(cause is RequestFailedException request && request.Status==412 || cause is StorageLifecycleConflictException)
                    result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
                else if(cause is RequestFailedException absent && absent.Status==404 && absent.ErrorCode=="BlobNotFound") result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
                else if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            }
        }
        return result;
    }
}
