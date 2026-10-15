using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public partial class AzureStorageOASIS
{
    private static void ValidateDetailVersion(int version)
    {
        if(version!=0) throw new NotSupportedException("Azure detail storage currently retains only the latest version (0).");
    }

    private static AvatarDetail DecodeDetail(string json,string key)
    {
        var detail=Deserialise<AvatarDetail>(json) ?? throw new InvalidDataException("Azure detail record is null.");
        if(detail.Id==Guid.Empty || detail.Id.ToString()!=key) throw new InvalidDataException("Azure detail identity does not match blob key.");
        detail.ProviderUniqueStorageKey ??= new Dictionary<ProviderType,string>();
        detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]=key;
        return detail;
    }

    private async Task<OASISResult<IAvatarDetail>> ReadDetailAsync(Guid id,int version)
    {
        var result=new OASISResult<IAvatarDetail>();
        try {
            ValidateDetailVersion(version);
            if(id==Guid.Empty) throw new ArgumentException("A nonempty avatar identity is required.");
            var key=id.ToString();
            var detail=DecodeDetail(await ReadBlobAsync(_serviceClient.GetBlobContainerClient(_detailContainer).GetBlobClient(key)),key);
            if(detail.IsDeleted) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Azure detail is deleted.";}
            else {result.Result=detail;result.IsLoaded=true;}
        }
        catch(RequestFailedException cause) when(cause.Status==404 && cause.ErrorCode=="BlobNotFound") {
            result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Azure detail not found.";
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IEnumerable<IAvatarDetail>>> ReadAllDetailsAsync(int version)
    {
        var result=new OASISResult<IEnumerable<IAvatarDetail>>();
        try {
            ValidateDetailVersion(version);
            var details=new List<IAvatarDetail>();
            var container=_serviceClient.GetBlobContainerClient(_detailContainer);
            await foreach(var blob in container.GetBlobsAsync()) {
                var detail=DecodeDetail(await ReadBlobAsync(container.GetBlobClient(blob.Name)),blob.Name);
                if(!detail.IsDeleted) details.Add(detail);
            }
            result.Result=details;result.IsLoaded=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }

    private async Task<OASISResult<IAvatarDetail>> ReadSelectedDetailAsync(string selector,bool email,int version)
    {
        var result=new OASISResult<IAvatarDetail>();
        try {
            ValidateDetailVersion(version);
            if(string.IsNullOrWhiteSpace(selector)) throw new ArgumentException("A nonblank avatar selector is required.");
            var loaded=await ReadAllDetailsAsync(version);
            if(loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatarDetail>,IAvatarDetail>(loaded);
            var matches=loaded.Result.Where(row=>string.Equals(email ? row.Email:row.Username,selector,StringComparison.OrdinalIgnoreCase)).Take(2).ToList();
            if(matches.Count>1) throw new StorageIdentityAmbiguousException();
            if(matches.Count==0) {result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="No Azure detail matches selector.";}
            else {result.Result=matches[0];result.IsLoaded=true;}
        }
        catch(Exception cause) {
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            if(cause is StorageIdentityAmbiguousException) result.ErrorCode=OASISResultErrorCodes.AmbiguousIdentity;
        }
        return result;
    }

    private async Task<OASISResult<IAvatarDetail>> WriteDetailAsync(IAvatarDetail detail)
    {
        var result=new OASISResult<IAvatarDetail>();
        try {
            ArgumentNullException.ThrowIfNull(detail);
            if(detail.Id==Guid.Empty) detail.Id=Guid.NewGuid();
            var key=detail.Id.ToString();
            var payload=BinaryData.FromString(Serialise(detail));
            await _serviceClient.GetBlobContainerClient(_detailContainer).GetBlobClient(key).UploadAsync(payload,overwrite:true);
            detail.ProviderUniqueStorageKey ??= new Dictionary<ProviderType,string>();
            detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]=key;
            result.Result=detail;result.IsSaved=true;
        }
        catch(Exception cause) {OASISErrorHandling.HandleError(ref result,cause.Message,cause);}
        return result;
    }
}
