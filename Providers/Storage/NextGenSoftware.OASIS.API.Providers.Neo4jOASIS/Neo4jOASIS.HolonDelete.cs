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
        var result=new OASISResult<IHolon>();var dispatched=false;
        try
        {
            if(field!="Id" && field!="ProviderKey") throw new ArgumentException("Unknown holon deletion selector.");
            if(string.IsNullOrWhiteSpace(value) || field=="Id" && (!Guid.TryParse(value,out var id) || id==Guid.Empty))
                throw new ArgumentException("A nonempty holon deletion identity is required.");
            if(!IsProviderActivated || _driver==null) throw new InvalidOperationException("Neo4j provider is not activated.");
            IHolon snapshot;
            using(var session=_driver.AsyncSession())
            {
                var found=await (await session.RunAsync("MATCH (h:Holon {"+field+":$value}) RETURN h",new {value})).ToListAsync();
                if(found.Count==0) return new OASISResult<IHolon> {IsError=true,ErrorCode=OASISResultErrorCodes.RecordNotFound,Message="Holon not found in Neo4j."};
                if(found.Count!=1) throw new StorageLifecycleConflictException("Neo4j holon deletion selector is not unique.");
                var node=found[0]["h"].As<Neo4j.Driver.INode>();
                snapshot=ReadHolonNode(node);
                var expected=(string)node["SnapshotJson"];
                using var transaction=await session.BeginTransactionAsync();
                dispatched=true;
                // Same reserved-property write-lock protocol as paired avatar deletion.
                // Recheck both the complete observed snapshot and selector after acquiring the lock.
                var query="MATCH (h:Holon {Id:$id}) WITH collect(h) AS candidates WHERE size(candidates)=1 "
                    +"UNWIND candidates AS h SET h.__oasisHolonLock=true REMOVE h.__oasisHolonLock "
                    +"WITH h WHERE h.SnapshotJson=$expected AND h."+field+"=$value DETACH DELETE h RETURN 1 AS affected";
                var changed=await (await transaction.RunAsync(query,new {id=snapshot.Id.ToString(),expected,value})).ToListAsync();
                if(changed.Count==0)
                {
                    await transaction.RollbackAsync();dispatched=false;
                    throw new StorageLifecycleConflictException("Neo4j holon changed before deletion.");
                }
                if(changed.Count!=1 || changed[0]["affected"].As<int>()!=1)
                    throw new InvalidDataException("Neo4j holon deletion acknowledgement is invalid.");
                await transaction.CommitAsync();
            }
            result.Result=snapshot;result.IsSaved=true;result.IsDeleted=true;result.DeletedCount=1;
        }
        catch(Exception cause)
        {
            result.IsError=true;result.Exception=dispatched ? new Neo4jMutationReconciliationException(cause):cause;
            result.Message=result.Exception.Message;
            if(cause is StorageLifecycleConflictException) result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
        }
        return result;
    }
}
