using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS
{
    protected virtual Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write)
        =>SaveHolonRecordAsync(write.Snapshot,write);

    private async Task<OASISResult<IHolon>> SaveHolonRecordAsync(IHolon source,PlannedHolonWrite? write)
    {
        var result=new OASISResult<IHolon>();
        try{
            var publication=StoredRecordPublicationSnapshot<Holon>.Capture(source);
            var holon=publication.Proposal;
            if(holon.Id==Guid.Empty)holon.Id=Guid.NewGuid();
            holon.ProviderUniqueStorageKey??=new Dictionary<Core.Enums.ProviderType,string>();
            holon.ProviderUniqueStorageKey[StorageProviderType]=holon.Id.ToString();
            var committed=await WriteRecordAsync<Holon>(new PutItemRequest{
                TableName="OasisHolons",Item=new Dictionary<string,AttributeValue>{
                    ["id"]=new(){S=holon.Id.ToString()},["parentHolonId"]=new(){S=holon.ParentHolonId.ToString()},
                    ["holon_type"]=new(){N=((int)holon.HolonType).ToString()},["is_deleted"]=new(){BOOL=holon.IsDeleted},["data_json"]=new(){S=Ser(holon)}
                }
            },IsVersionControlEnabled,publication.ValidateUnchanged,write);
            ValidateAcknowledgedCaller(publication.ValidateUnchanged,committed.Id);
            source.Version=committed.Version;source.VersionId=committed.VersionId;source.PreviousVersionId=committed.PreviousVersionId;
            source.Id=holon.Id;source.ProviderUniqueStorageKey??=new Dictionary<Core.Enums.ProviderType,string>();
            source.ProviderUniqueStorageKey[StorageProviderType]=holon.Id.ToString();
            if(source is Holon caller)caller.ChildRoleManifestJson=committed.ChildRoleManifestJson;
            result.Result=source;result.IsSaved=true;result.Message=$"DynamoDBOASIS: Holon '{holon.Name}' saved.";
        }catch(Exception cause){result.Exception=cause;OASISErrorHandling.HandleError(ref result,$"DynamoDBOASIS: {cause.Message}");}
        return result;
    }
}
