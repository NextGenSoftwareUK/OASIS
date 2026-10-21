using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativePrimaryOutboxCommitsGraphReceiptAndEveryFrozenTargetTogether(bool synchronous)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_primary_outbox_fixture";
        var client=new MongoClient(uri);using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var first=new Provider(uri,name);var second=new Provider(uri,name);var complete=false;var actor=Guid.NewGuid();
        var rows=database.GetCollection<BsonDocument>("Holon");var receipts=database.GetCollection<BsonDocument>("CosmicWriteReceipt");
        var obligations=database.GetCollection<BsonDocument>("CosmicHolonOutbox");var counters=database.GetCollection<BsonDocument>("CosmicHolonOutboxSequence");
        var enrollment=new HolonReplicationEnrollment("owned-primary",new[]{"target-b","target-a"});
        Task<OASISResult<IEnumerable<IHolon>>> Write(Provider provider,ConditionalHolonWriteRequest request,HolonReplicationEnrollment enrolled=null)
            =>synchronous ? Task.Run(()=>provider.ExecuteReplicatingHolonWrite(request,enrolled ?? enrollment))
                :provider.ExecuteReplicatingHolonWriteAsync(request,enrolled ?? enrollment);
        Guid Token(IHolon row)=>CosmicConcurrency.GetProviderRevisionId(row,ProviderType.MongoDBOASIS)!.Value;
        ConditionalHolonWriteRequest Request(IHolon row,Guid? expected,ConditionalHolonOperation operation=ConditionalHolonOperation.Save)
            =>new(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.MongoDBOASIS,new[]{new HolonWriteCondition(row.Id,expected)}),operation,new[]{row},saveChildren:false);
        async Task<byte[]> Native(Guid id)=>(await rows.Find(new BsonDocument("HolonId",id.ToString("D"))).SingleAsync()).ToBson();
        try
        {
            Assert.False((await first.ActivateProviderAsync()).IsError);Assert.False((await second.ActivateProviderAsync()).IsError);
            var child=new Holon{Id=Guid.NewGuid(),Name="owned queued child"};
            var input=new Holon{Id=Guid.NewGuid(),Name="owned queued root",Children=new List<IHolon>{child},
                MetaData=new(){["date"]=new DateTime(1960,1,2,3,4,5,DateTimeKind.Utc).AddTicks(1234567),["bytes"]=new byte[]{0,17,255}}};
            var create=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.MongoDBOASIS,
                new[]{new HolonWriteCondition(input.Id,null),new HolonWriteCondition(child.Id,null)}),ConditionalHolonOperation.Save,new[]{input});
            var saved=await Write(first,create);Assert.False(saved.IsError,saved.Message+saved.Exception);var current=Assert.Single(saved.Result);
            Assert.Equal(0,input.Version);Assert.Equal(0,child.Version);
            var frozen=await obligations.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("TargetId",1)).ToListAsync();Assert.Equal(2,frozen.Count);
            Assert.Equal("target-a",frozen[0]["TargetId"].AsString);Assert.Equal("target-b",frozen[1]["TargetId"].AsString);
            Assert.Equal(frozen[0]["PayloadJson"],frozen[1]["PayloadJson"]);
            foreach(var obligation in frozen)
            {
                Assert.Equal(obligation["TargetId"].AsString=="target-a" ? 1L:2L,obligation["Sequence"].AsInt64);Assert.Equal("Pending",obligation["DeliveryState"].AsString);
                var payload=obligation["PayloadJson"].AsString;
                Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))),obligation["PayloadDigest"].AsString);
                var delivery=CommittedHolonReplicaPayload.Read(payload);Assert.Equal(create.Write.OperationId,delivery.OperationId);
                Assert.Equal(current.VersionId,delivery.Roots[0].VersionId);Assert.Single(delivery.Roots[0].Children);
                var actual=await second.LoadHolonAsync(input.Id,loadChildren:true);Assert.False(actual.IsError,actual.Message+actual.Exception);
                var plan=CommittedHolonReplicaPlan.Create(delivery,ProviderType.MongoDBOASIS);plan.ValidateAcknowledgedRecord(actual.Result);
                plan.ValidateAcknowledgedRecord(Assert.Single(actual.Result.Children));
            }
            var embedded=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(create.Write.OperationId,actor,ProviderType.MongoDBOASIS,
                create.Write.Conditions,new HolonReplicationEnrollment("owned-primary",new[]{"target-a","target-b"})),create.Operation,create.Roots);
            var replay=await second.ExecuteConditionalHolonWriteAsync(embedded);Assert.False(replay.IsError,replay.Message+replay.Exception);
            Assert.Equal(current.VersionId,Assert.Single(replay.Result).VersionId);
            Assert.Equal(2,await obligations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal("COSMIC_OPERATION_ID_REUSED",(await second.ExecuteConditionalHolonWriteAsync(create)).ErrorCode);
            Assert.Equal("COSMIC_OPERATION_ID_REUSED",(await Write(second,create,new HolonReplicationEnrollment("different",new[]{"target-a","target-b"}))).ErrorCode);
            Assert.Equal("COSMIC_OPERATION_ID_REUSED",(await Write(second,create,new HolonReplicationEnrollment("owned-primary",new[]{"target-c"}))).ErrorCode);
            long commits=1;
            foreach(var collection in new[]{"CosmicHolonOutbox","CosmicHolonOutboxSequence","CosmicWriteReceipt"})
            {
                var validator=collection=="CosmicHolonOutbox" ? new BsonDocument("TargetId","target-a")
                    :collection=="CosmicHolonOutboxSequence" ? new BsonDocument("Value",commits*2):new BsonDocument("Fingerprint","owned impossible receipt");
                var request=Request(new Holon{Id=input.Id,Name="owned fault "+collection},Token(current));var before=await Native(input.Id);
                await database.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod",collection},{"validator",validator},{"validationLevel","strict"}});
                try
                {
                    var rejected=await Write(first,request);Assert.True(rejected.IsError);Assert.Equal(before,await Native(input.Id));
                    Assert.Equal(commits*2,await obligations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    Assert.Equal(commits,await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                    Assert.Equal(commits*2,(await counters.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync())["Value"].AsInt64);
                }
                finally{await database.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod",collection},{"validator",new BsonDocument()}});}
                var retry=await Write(second,request);Assert.False(retry.IsError,retry.Message+retry.Exception);current=Assert.Single(retry.Result);commits++;
            }
            foreach(var operation in new[]{ConditionalHolonOperation.SoftDelete,ConditionalHolonOperation.HardDelete})
            {
                var request=Request(current,Token(current),operation);var deleted=await Write(first,request);
                Assert.False(deleted.IsError,deleted.Message+deleted.Exception);current=Assert.Single(deleted.Result);commits++;Assert.True(current.IsDeleted);
                Assert.Equal(current.VersionId,Assert.Single((await Write(second,request)).Result).VersionId);
            }
            Assert.Null((await second.LoadHolonAsync(input.Id,loadChildren:false)).Result);
            Assert.Equal(1,(await second.LoadHolonAsync(child.Id,loadChildren:false)).Result.Version);
            var ordered=await obligations.Find(new BsonDocument("TargetId","target-a")).Sort(new BsonDocument("Sequence",1)).ToListAsync();
            Assert.Equal(commits,ordered.Count);
            for(var index=0;index<ordered.Count;index++)
            {
                Assert.Equal(index*2L+1,ordered[index]["Sequence"].AsInt64);
                var revision=CommittedHolonReplicaPayload.Read(ordered[index]["PayloadJson"].AsString).Roots[0];Assert.Equal(index+1,revision.Version);
                if(index>0)Assert.Equal(CommittedHolonReplicaPayload.Read(ordered[index-1]["PayloadJson"].AsString).Roots[0].VersionId,revision.PreviousVersionId);
            }
            Assert.Equal(frozen[0]["PayloadJson"],ordered[0]["PayloadJson"]);
            Assert.Equal(1,Assert.Single((await Write(second,create)).Result).Version);
            Assert.Equal(commits*2,await obligations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings{LogSwitchingProviders=false}}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var providers=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna){
                IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=true,IsAutoLoadBalanceEnabled=false};
            providers.RegisterProvider(first);var manager=new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null,dna,providers);
            var managedRoot=new Holon{Name="owned enrolled manager root"};
            var options=new CosmicWriteOptions(Guid.NewGuid(),expectedRevisions:null,replicationEnrollment:enrollment);
            Task<OASISResult<IHolon>> Managed(CosmicWriteOptions write)=>synchronous ? Task.Run(()=>manager.SaveHolon(managedRoot,actor,write,
                saveChildren:false,providerType:ProviderType.MongoDBOASIS)):manager.SaveHolonAsync(managedRoot,actor,write,saveChildren:false,providerType:ProviderType.MongoDBOASIS);
            await database.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","CosmicHolonOutbox"},{"validator",new BsonDocument("TargetId","owned impossible target")}});
            try
            {
                var rejected=await Managed(options);Assert.True(rejected.IsError);Assert.Equal(Guid.Empty,managedRoot.Id);Assert.Equal(0,managedRoot.Version);
                Assert.True(managedRoot.HasHolonChanged());Assert.Equal(commits*2,await obligations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                Assert.Equal(commits,await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            }
            finally{await database.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","CosmicHolonOutbox"},{"validator",new BsonDocument()}});}
            var managed=await Managed(options);Assert.False(managed.IsError,managed.Message+managed.Exception);commits++;
            Assert.Same(managedRoot,managed.Result);Assert.NotEqual(Guid.Empty,managedRoot.Id);Assert.Equal(1,managedRoot.Version);Assert.False(managedRoot.HasHolonChanged());
            var noChanges=await Managed(new CosmicWriteOptions(Guid.NewGuid(),expectedRevisions:null,replicationEnrollment:enrollment));
            Assert.False(noChanges.IsError);Assert.False(noChanges.IsSaved);Assert.Equal(commits*2,await obligations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var managedPayload=CommittedHolonReplicaPayload.Read((await obligations.Find(new BsonDocument{{"OperationId",options.OperationId.ToString("D")},{"TargetId","target-a"}}).SingleAsync())["PayloadJson"].AsString);
            Assert.Equal(managedRoot.Id,managedPayload.Roots[0].Id);Assert.Equal(managedRoot.VersionId,managedPayload.Roots[0].VersionId);
            managedRoot.Name="owned pending change";Assert.Equal("COSMIC_OPERATION_ID_REUSED",(await Managed(options)).ErrorCode);
            Assert.True(managedRoot.HasHolonChanged());Assert.Equal(1,managedRoot.Version);
            Assert.Equal(commits*2,await obligations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var all=await obligations.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("Sequence",1)).ToListAsync();
            Assert.Equal(all.Count,all.Select(item=>item["Sequence"].AsInt64).Distinct().Count());
            for(var index=0;index<all.Count;index++)Assert.Equal(index+1L,all[index]["Sequence"].AsInt64);
            var beforeRegression=await Native(managedRoot.Id);
            await counters.UpdateOneAsync(new BsonDocument("_id","primary-holon-outbox-v1"),new BsonDocument("$set",new BsonDocument("Value",0L)));
            var regressed=await Managed(new CosmicWriteOptions(Guid.NewGuid(),expectedRevisions:null,replicationEnrollment:enrollment));
            Assert.True(regressed.IsError);Assert.IsType<InvalidDataException>(regressed.Exception);Assert.Equal(beforeRegression,await Native(managedRoot.Id));
            Assert.Equal(commits*2,await obligations.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(0L,(await counters.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync())["Value"].AsInt64);
            complete=true;
        }
        finally
        {
            await second.DeActivateProviderAsync();await first.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
        }
    }
}
