using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    [Theory,InlineData(false),InlineData(true)]
    public async Task NativeSaveLifecyclePreservesExactTombstoneAndOriginalActor(bool history)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var name=history?"cosmic_lifecycle_history_fixture":"cosmic_lifecycle_summary_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);var id=Guid.NewGuid();var actor=Guid.NewGuid();var creation=Guid.NewGuid();var complete=false;
        await database.GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(new BsonDocument{{"_id","owned lifecycle v1"},{"root",id.ToString()},{"actor",actor.ToString()},{"creation",creation.ToString()},{"history",history}});
        var rows=database.GetCollection<BsonDocument>("Holon");var receipts=database.GetCollection<BsonDocument>("CosmicWriteReceipt");
        var provider=new Provider(uri,name){IsVersionControlEnabled=history};
        Guid Revision(IHolon row)=>CosmicConcurrency.GetProviderRevisionId(row,ProviderType.MongoDBOASIS)!.Value;
        ConditionalHolonWriteRequest Request(IHolon value,Guid? expected,ConditionalHolonOperation operation=ConditionalHolonOperation.Save,Guid? op=null)
            =>new(new ConditionalHolonWrite(op??Guid.NewGuid(),actor,ProviderType.MongoDBOASIS,new[]{new HolonWriteCondition(value.Id,expected)}),operation,new[]{value},saveChildren:false);
        async Task<byte[]> Native()=> (await rows.Find(new BsonDocument("HolonId",id.ToString())).SingleAsync()).ToBson();
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var created=await provider.ExecuteConditionalHolonWriteAsync(Request(new Holon{Id=id,Name="owned lifecycle root"},null,op:creation));Assert.False(created.IsError,created.Message+created.Exception);
            var soft=await provider.ExecuteConditionalHolonWriteAsync(Request(created.Result.Single(),Revision(created.Result.Single()),ConditionalHolonOperation.SoftDelete));Assert.False(soft.IsError,soft.Message+soft.Exception);
            var tombstone=soft.Result.Single();Assert.Equal(actor,tombstone.DeletedByAvatarId);var original=await Native();
            foreach(var scenario in new[]{"active","cleared","retimed","reattributed"})
            {
                var proposal=OasisJson.Project<Holon>(tombstone);proposal.Name="owned invalid lifecycle "+scenario;
                if(scenario=="active"){proposal.IsActive=true;proposal.DeletedDate=DateTime.MinValue;proposal.DeletedByAvatarId=Guid.Empty;}
                if(scenario=="cleared")proposal.DeletedDate=DateTime.MinValue;
                if(scenario=="retimed")proposal.DeletedDate=proposal.DeletedDate.AddTicks(1);
                if(scenario=="reattributed")proposal.DeletedByAvatarId=Guid.NewGuid();
                var before=OasisJson.Serialize(proposal);var failed=await provider.ExecuteConditionalHolonWriteAsync(Request(proposal,Revision(tombstone)));
                Assert.True(failed.IsError);Assert.Equal(StorageLifecycleConflictException.ErrorCode,failed.ErrorCode);Assert.False(failed.IsSaved);Assert.False(NativeMutationReconciliation.IsRequired(failed.Exception));
                Assert.False(failed.MetaData?.ContainsKey(HolonCommittedDelta.ResultMetadataKey)??false);Assert.Equal(before,OasisJson.Serialize(proposal));Assert.Equal(original,await Native());
            }
            var ordinary=new Holon{Id=id,Name="owned invalid ordinary lifecycle"};var ordinaryFailure=await provider.SaveHolonAsync(ordinary,saveChildren:false);
            Assert.True(ordinaryFailure.IsError);Assert.Equal(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,ordinaryFailure.ErrorCode);Assert.Equal(original,await Native());
            var graphFailure=await provider.SaveHolonsAsync(new[]{ordinary},saveChildren:false);Assert.True(graphFailure.IsError);Assert.Equal(StorageLifecycleConflictException.ErrorCode,graphFailure.ErrorCode);Assert.Equal(original,await Native());
            Assert.Equal(2,await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var compatible=OasisJson.Project<Holon>(tombstone);compatible.Name="owned compatible tombstone edit";
            var accepted=await provider.ExecuteConditionalHolonWriteAsync(Request(compatible,Revision(tombstone)));Assert.False(accepted.IsError,accepted.Message+accepted.Exception);
            Assert.Equal(tombstone.DeletedDate,accepted.Result.Single().DeletedDate);Assert.Equal(actor,accepted.Result.Single().DeletedByAvatarId);Assert.True(accepted.Result.Single().IsDeleted);
            Assert.Equal(3,await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned lifecycle fixture: "+name+" id="+id+" actor="+actor+" creation="+creation);
        }
    }
}
