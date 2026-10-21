using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private async Task<OASISResult<IHolon>> DeleteHolonRecordAsync(string field,string value)
    {
        try
        {
            if(field!="Id" && field!="ProviderKey")throw new ArgumentException("Unknown holon deletion selector.");
            if(string.IsNullOrWhiteSpace(value)||field=="Id"&&(!Guid.TryParse(value,out var id)||id==Guid.Empty))
                throw new ArgumentException("A nonempty holon deletion identity is required.");
            return await ExecuteOwnedNeoMutationAsync<IHolon>(async transaction=>
            {
                var found=await(await transaction.RunAsync("MATCH (h:Holon {"+field+":$value}) RETURN h",new{value}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
                if(found.Count==0)return(new OASISResult<IHolon>{IsError=true,ErrorCount=1,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Holon not found in Neo4j."},false);
                if(found.Count!=1)throw new StorageLifecycleConflictException("Neo4j holon deletion selector is not unique.");
                var node=found[0]["h"].As<Neo4j.Driver.INode>();var snapshot=ReadHolonNode(node);var expected=(string)node["SnapshotJson"];
                var query="MATCH (h:Holon {Id:$id}) WITH collect(h) AS candidates WHERE size(candidates)=1 "
                    +"UNWIND candidates AS h SET h.__oasisHolonLock=true REMOVE h.__oasisHolonLock "
                    +"WITH h WHERE h.SnapshotJson=$expected AND h."+field+"=$value DETACH DELETE h RETURN 1 AS affected";
                var changed=await(await transaction.RunAsync(query,new{id=snapshot.Id.ToString(),expected,value}).ConfigureAwait(false)).ToListAsync().ConfigureAwait(false);
                if(changed.Count==0)throw new StorageLifecycleConflictException("Neo4j holon changed before deletion.");
                if(changed.Count!=1||changed[0]["affected"].As<int>()!=1)throw new InvalidDataException("Neo4j holon deletion acknowledgement is invalid.");
                await(await transaction.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon',RecordId:$id}) DETACH DELETE v",new{id=snapshot.Id.ToString()}).ConfigureAwait(false)).ConsumeAsync().ConfigureAwait(false);
                return(new OASISResult<IHolon>{Result=snapshot,IsSaved=true,IsDeleted=true,DeletedCount=1,HasAnyHolonsChanged=true},true);
            }).ConfigureAwait(false);
        }
        catch(Exception cause){return NeoMutationAdmissionFailure<IHolon>(cause);}
    }
}
