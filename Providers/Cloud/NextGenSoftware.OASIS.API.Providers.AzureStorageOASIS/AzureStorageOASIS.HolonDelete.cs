using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS;

public partial class AzureStorageOASIS
{
    private async Task<OASISResult<IHolon>> DeleteCanonicalHolonAsync(string key,Guid? expectedId=null)
    {
        var result=new OASISResult<IHolon>();
        try
        {
            if(string.IsNullOrWhiteSpace(key) || expectedId==Guid.Empty) throw new ArgumentException("A nonempty holon key/identity is required.");
            var blob=_serviceClient.GetBlobContainerClient(_holonContainer).GetBlobClient(key);
            var snapshot=(await blob.DownloadContentAsync()).Value;
            if(string.IsNullOrWhiteSpace(snapshot.Details.ETag.ToString())) throw new InvalidDataException("Azure holon snapshot has no ETag.");
            var record=Deserialise<Holon>(snapshot.Content.ToString()) ?? throw new InvalidDataException("Azure holon record is null.");
            if(record.Id==Guid.Empty || HolonKey(record)!=key || expectedId.HasValue && record.Id!=expectedId.Value)
                throw new InvalidDataException("Azure holon identity/native key does not match the requested record.");
            record.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            record.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS]=key;
            var condition=new BlobRequestConditions {IfMatch=snapshot.Details.ETag};
            await ObserveBlobMutationAsync(async()=> {
                var response=await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots,condition);
                if(response.Status<200 || response.Status>=300) throw new InvalidDataException("Azure holon deletion lacks authoritative acknowledgement.");
                return response;
            },blob);
            result.Result=record;result.IsSaved=true;result.IsDeleted=true;result.DeletedCount=1;
        }
        catch(RequestFailedException cause) when(cause.Status==404 && cause.ErrorCode=="BlobNotFound")
        {
            result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Azure holon not found.";
        }
        catch(RequestFailedException cause) when(cause.Status==412)
        {
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        catch(Exception cause)
        {
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);
            if(cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
        }
        return result;
    }
}
