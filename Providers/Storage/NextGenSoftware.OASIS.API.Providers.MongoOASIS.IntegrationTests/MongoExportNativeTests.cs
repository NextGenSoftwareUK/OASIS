using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using Xunit;
using Xunit.Abstractions;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class MongoExportNativeTests(ITestOutputHelper output)
{
    public class SessionIntentBoundary:System.Reflection.DispatchProxy
    {
        private IMongoClient inner=null!;private Action mutate=null!;
        public static IMongoClient Wrap(IMongoClient client,Action action){var proxy=Create<IMongoClient,SessionIntentBoundary>();var owner=(SessionIntentBoundary)(object)proxy;owner.inner=client;owner.mutate=action;return proxy;}
        protected override object? Invoke(System.Reflection.MethodInfo? method,object?[]? args)
        {
            object? result;
            try{result=method!.Invoke(inner,args);}
            catch(System.Reflection.TargetInvocationException cause)when(cause.InnerException!=null){System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;}
            if(method!.Name=="StartSessionAsync")return After((Task<IClientSessionHandle>)result!);
            if(method.Name=="StartSession")mutate();
            return result;
        }
        private async Task<IClientSessionHandle> After(Task<IClientSessionHandle> pending){var session=await pending;mutate();return session;}
    }

    private sealed class SearchIntentBoundary(NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces.ISearchRepository inner,Action mutate)
        :NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces.ISearchRepository
    {
        public OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.Search.ISearchResults> Search(NextGenSoftware.OASIS.API.Core.Objects.Search.ISearchParams query,int version=0)
        {mutate();return inner.Search(query,version);}
        public async Task<OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.Search.ISearchResults>> SearchAsync(NextGenSoftware.OASIS.API.Core.Objects.Search.ISearchParams query,int version=0)
        {await Task.Yield();mutate();return await inner.SearchAsync(query,version);}
    }

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(true,true)]
    public async Task GameplayMutationsRetainNativeAccountFieldsAndExistingArchive(bool transfer,bool overflow)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var sourceId=Guid.NewGuid();var targetId=Guid.NewGuid();var itemId=Guid.NewGuid();
            var typed=database.GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>("AvatarDetail");
            await typed.InsertManyAsync(new[]{
                new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail {Id=ObjectId.GenerateNewId().ToString(),HolonId=sourceId,Version=1,VersionId=Guid.NewGuid(),Inventory=new List<NextGenSoftware.OASIS.API.Core.Objects.InventoryItem>{new(){Id=itemId,Name="Native seed",GameSource="Our World",Quantity=3,ItemType=InventoryItemType.QuestItem}}},
                new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail {Id=ObjectId.GenerateNewId().ToString(),HolonId=targetId,Version=overflow?int.MaxValue:1,VersionId=Guid.NewGuid()}});
            var raw=database.GetCollection<BsonDocument>("AvatarDetail");
            var expected=new Dictionary<ObjectId,BsonDocument>();
            foreach(var row in await raw.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())
            {
                row["UnknownAccountField"]=new BsonDocument{{"bytes",new BsonBinaryData(new byte[]{0,255,19})},{"date",new BsonDateTime(DateTime.UtcNow)}};
                var old=row.DeepClone().AsBsonDocument;old["Version"]=0;
                var entry=new BsonDocument{{"bson",old},{"sha256",Convert.ToHexString(SHA256.HashData(old.ToBson()))}};
                row["OasisHolonRevisionArchive"]=new BsonDocument{{"schema",2},{"records",new BsonArray{entry}}};
                var saved=await raw.ReplaceOneAsync(new BsonDocument("_id",row["_id"]),row);Assert.Equal(1,saved.MatchedCount);
                expected.Add(row["_id"].AsObjectId,row.DeepClone().AsBsonDocument);
            }
            var payload=transfer?new HyperDriveAvatarGameplayCommand {Action=HyperDriveAvatarGameplayAction.TransferInventory,InventoryItemId=itemId,TargetAvatarId=targetId,Amount=1}
                :new HyperDriveAvatarGameplayCommand {Action=HyperDriveAvatarGameplayAction.UpdateInventory,InventoryItemId=itemId,InventoryUpdate=new(){Id=itemId,Name="Updated seed",GameSource="Our World",Quantity=2,MaxQuantity=10,ItemType=(int)InventoryItemType.QuestItem,IsStackable=true}};
            var command=new HostedSyncCommandItem {OperationId=Guid.NewGuid(),AvatarId=sourceId,EntityId=sourceId,EntityType=HyperDriveEntityTypes.AvatarGameplay,PayloadJson=HyperDriveJson.Serialize(payload)};
            var applied=await provider.ApplyAvatarGameplayCommandAsync(command,default);
            if(overflow)
            {
                Assert.True(applied.IsError);Assert.Equal("AVATAR_VERSION_OVERFLOW",applied.ErrorCode);Assert.IsType<OverflowException>(applied.Exception);
                foreach(var row in await raw.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())Assert.Equal(expected[row["_id"].AsObjectId].ToBson(),row.ToBson());
                complete=true;return;
            }
            Assert.False(applied.IsError,applied.Message+applied.Exception);
            var after=await raw.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();Assert.Equal(2,after.Count);
            foreach(var row in after)
            {
                var original=expected[row["_id"].AsObjectId];
                Assert.Equal(original["UnknownAccountField"].AsBsonDocument.ToBson(),row["UnknownAccountField"].AsBsonDocument.ToBson());
                Assert.Equal(original["OasisHolonRevisionArchive"].AsBsonDocument.ToBson(),row["OasisHolonRevisionArchive"].AsBsonDocument.ToBson());
                if(transfer || row["HolonId"].AsString==sourceId.ToString())
                {
                    Assert.Equal(original["Version"].AsInt32+1,row["Version"].AsInt32);
                    Assert.Equal(original["VersionId"],row["PreviousVersionId"]);Assert.NotEqual(original["VersionId"],row["VersionId"]);
                }
                else Assert.Equal(original.ToBson(),row.ToBson());
            }
            var replay=await provider.ApplyAvatarGameplayCommandAsync(command,default);Assert.False(replay.IsError,replay.Message+replay.Exception);
            foreach(var row in after)Assert.Equal(row.ToBson(),(await raw.Find(new BsonDocument("_id",row["_id"])).SingleAsync()).ToBson());
            complete=true;
        }
        finally {await provider.DeActivateProviderAsync();if(complete)await client.DropDatabaseAsync(name);else output.WriteLine("Preserved owned gameplay native-field fixture for reviewed reconciliation.");}
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task PublicReadsRejectMalformedAuditWithoutMutatingStoredRecord(int kind)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.HolonBase record=kind switch{
                0=>new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar(),
                1=>new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail(),
                _=>new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon()};
            record.HolonId=Guid.NewGuid();record.Id=ObjectId.GenerateNewId().ToString();record.Name="owned conversion record";
            var raw=database.GetCollection<BsonDocument>(kind==0?"Avatar":kind==1?"AvatarDetail":"Holon");
            await raw.InsertOneAsync(record.ToBsonDocument());
            void Reject<T>(OASISResult<T> result)
            {
                Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsLoaded);Assert.Equal(0,result.LoadedCount);
                Assert.NotNull(result.Exception);Assert.Equal(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
                Assert.DoesNotContain("malformed-owned-actor",result.Message);
            }
            foreach(var field in new[]{"CreatedByAvatarId","ModifiedByAvatarId","DeletedByAvatarId"})
            {
                var changed=await raw.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,Builders<BsonDocument>.Update.Set(field,"malformed-owned-actor"));Assert.Equal(1,changed.MatchedCount);
                var before=Assert.Single(await raw.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToBson();
                if(kind==0){Reject(await provider.LoadAvatarAsync(record.HolonId));Reject(await provider.LoadAllAvatarsAsync());}
                else if(kind==1){Reject(await provider.LoadAvatarDetailAsync(record.HolonId));Reject(await provider.LoadAllAvatarDetailsAsync());}
                else{Reject(await provider.LoadHolonAsync(record.HolonId,loadChildren:false));Reject(await provider.LoadAllHolonsAsync(loadChildren:false));}
                Assert.Equal(before,Assert.Single(await raw.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToBson());
                var restored=await raw.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,Builders<BsonDocument>.Update.Unset(field));Assert.Equal(1,restored.MatchedCount);
            }
            if(kind==0)Assert.False((await provider.LoadAvatarAsync(record.HolonId)).IsError);
            else if(kind==1)Assert.False((await provider.LoadAvatarDetailAsync(record.HolonId)).IsError);
            else Assert.False((await provider.LoadHolonAsync(record.HolonId,loadChildren:false)).IsError);
            complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_cql_fixture public conversion failure evidence for reviewed reconciliation.");
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ClanTransferPreservesHistoryOrRejectsDeletedTarget(bool deletedTarget)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var sourceId=Guid.NewGuid();var itemId=Guid.NewGuid();var destinationId=Guid.NewGuid();
            var details=database.GetCollection<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>("AvatarDetail");
            await details.InsertOneAsync(new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail{Id=ObjectId.GenerateNewId().ToString(),HolonId=sourceId,Version=1,VersionId=Guid.NewGuid(),
                Inventory=new List<NextGenSoftware.OASIS.API.Core.Objects.InventoryItem>{new(){Id=itemId,Name="Clan seed",GameSource="Our World",Quantity=3,ItemType=InventoryItemType.QuestItem}}});
            var clan=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=sourceId,HolonType=HolonType.Clan,Name="original clan",Version=1,VersionId=Guid.NewGuid(),MetaData=new(){
                [ClanManager.ClanStateMetadataKey]=Newtonsoft.Json.JsonConvert.SerializeObject(new ClanManager.ClanPersistentState{OwnerAvatarId=sourceId,MemberIds=new(){sourceId},Inventory=new()})}};
            var first=await provider.SaveHolonAsync(clan,saveChildren:false);Assert.False(first.IsError,first.Message+first.Exception);
            var current=first.Result;current.PreviousVersionId=current.VersionId;current.Version=2;current.VersionId=Guid.NewGuid();current.Name="current clan";
            if(deletedTarget)current.DeletedDate=DateTime.UtcNow;
            var second=await provider.SaveHolonAsync(current,saveChildren:false);Assert.False(second.IsError,second.Message+second.Exception);
            var rawClans=database.GetCollection<BsonDocument>("Holon");
            var marker=await rawClans.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,Builders<BsonDocument>.Update.Set("UnknownClanField","retained"));Assert.Equal(1,marker.MatchedCount);
            var rawDetails=database.GetCollection<BsonDocument>("AvatarDetail");
            var accountMarker=await rawDetails.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,Builders<BsonDocument>.Update.Set("UnknownAccountField",new BsonDocument("native",new BsonBinaryData(new byte[]{0,255,19}))));Assert.Equal(1,accountMarker.MatchedCount);
            async Task<byte[]> ClanBytes()=>Assert.Single(await rawClans.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToBson();
            async Task<byte[]> SourceBytes()=>Assert.Single(await rawDetails.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToBson();
            var beforeClan=await ClanBytes();var beforeSource=await SourceBytes();
            var originalSourceRevision=(await details.Find(FilterDefinition<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>.Empty).SingleAsync()).VersionId;
            var payload=new HyperDriveAvatarGameplayCommand{Action=HyperDriveAvatarGameplayAction.TransferInventoryToClan,InventoryItemId=itemId,TargetClanId=clan.Id,DestinationInventoryItemId=destinationId,Amount=2};
            var command=new HostedSyncCommandItem{OperationId=Guid.NewGuid(),AvatarId=sourceId,EntityId=sourceId,EntityType=HyperDriveEntityTypes.AvatarGameplay,PayloadJson=HyperDriveJson.Serialize(payload)};
            var applied=await provider.ApplyAvatarGameplayCommandAsync(command,default);
            if(deletedTarget)
            {
                Assert.True(applied.IsError);Assert.Equal(beforeClan,await ClanBytes());Assert.Equal(beforeSource,await SourceBytes());
            }
            else
            {
                Assert.False(applied.IsError,applied.Message+applied.Exception);
                var savedSource=await details.Find(FilterDefinition<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail>.Empty).SingleAsync();
                Assert.Equal(1,Assert.Single(savedSource.Inventory).Quantity);
                Assert.Equal(2,savedSource.Version);Assert.Equal(originalSourceRevision,savedSource.PreviousVersionId);Assert.NotEqual(originalSourceRevision,savedSource.VersionId);
                Assert.Equal(new byte[]{0,255,19},Assert.Single(await rawDetails.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync())["UnknownAccountField"]["native"].AsBsonBinaryData.Bytes);
                var stored=Assert.Single(await rawClans.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
                Assert.Equal(3,stored["Version"].AsInt32);Assert.Equal("retained",stored["UnknownClanField"].AsString);
                var all=await provider.ExportAllAsync(-1);Assert.False(all.IsError,all.Message+all.Exception);
                Assert.Equal(new[]{1,2,3},all.Result.OrderBy(row=>row.Version).Select(row=>row.Version));
                var latest=all.Result.Single(row=>row.Version==3);
                var state=Newtonsoft.Json.JsonConvert.DeserializeObject<ClanManager.ClanPersistentState>(latest.MetaData[ClanManager.ClanStateMetadataKey].ToString());
                Assert.Equal(2,Assert.Single(state.Inventory).Quantity);
                var afterClan=await ClanBytes();var afterSource=await SourceBytes();
                var replay=await provider.ApplyAvatarGameplayCommandAsync(command,default);Assert.False(replay.IsError,replay.Message+replay.Exception);
                Assert.Equal(afterClan,await ClanBytes());Assert.Equal(afterSource,await SourceBytes());
                payload.Amount=1;command.PayloadJson=HyperDriveJson.Serialize(payload);
                var mismatch=await provider.ApplyAvatarGameplayCommandAsync(command,default);Assert.True(mismatch.IsError);
                Assert.Equal(afterClan,await ClanBytes());Assert.Equal(afterSource,await SourceBytes());
            }
            complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_cql_fixture clan-transfer history evidence for reviewed reconciliation.");
        }
    }

    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task ReconcileReviewedHistoricalCollectionFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var client=new MongoClient(uri);var database=client.GetDatabase("cosmic_cql_fixture");
        var names=(await (await database.ListCollectionNamesAsync()).ToListAsync()).OrderBy(name=>name).ToArray();
        Assert.Equal(new[]{"Avatar","AvatarDetail","CosmicHolonOutbox","CosmicOwnedFixture","Holon"},names);
        foreach(var name in names.Where(name=>name!="Holon"))Assert.Equal(0,await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var expected=new Dictionary<string,string>{
            {"6adc6af6b7619719aaec0915","B56B2DF3505A2C2F938EB0C28952CDCADA357AAC35F16BEC7C82CDD8648048EB"},
            {"6adc6af6b7619719aaec0916","C9F4F84D6756E27B13BEF622709B54FEF07156A36E5DE6D180ED22EAB9BDA483"},
            {"6adc6af6b7619719aaec0917","EEAB68A5E3F01F3DEFB23201058BBC7AC3B81C6AC6223141FE059F703F410BDF"}};
        var collection=database.GetCollection<BsonDocument>("Holon");
        var rows=await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();Assert.Equal(3,rows.Count);
        foreach(var row in rows){Assert.True(expected.TryGetValue(row["_id"].ToString(),out var hash));Assert.Equal(hash,Convert.ToHexString(SHA256.HashData(row.ToBson())));}
        foreach(var row in rows)
        {
            var exact=new BsonDocument{{"_id",row["_id"]},{"$expr",new BsonDocument("$eq",new BsonArray{"$$ROOT",new BsonDocument("$literal",row)})}};
            var deleted=await collection.DeleteOneAsync(exact);Assert.True(deleted.IsAcknowledged);Assert.Equal(1,deleted.DeletedCount);
        }
        foreach(var name in names)Assert.Equal(0,await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        await client.DropDatabaseAsync("cosmic_cql_fixture");
        output.WriteLine("Removed only three reviewed hash-matched synthetic records and verified-empty fixture DB; original failure/inspection evidence retained.");
    }

    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task InspectPreservedHistoricalCollectionFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var database=new MongoClient(uri).GetDatabase("cosmic_cql_fixture");
        var names=(await (await database.ListCollectionNamesAsync()).ToListAsync()).OrderBy(name=>name).ToArray();
        Assert.Contains("CosmicOwnedFixture",names);output.WriteLine("Collections: "+string.Join(",",names));
        foreach(var name in names.Where(name=>name!="Holon"))
            Assert.Equal(0,await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var rows=await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();Assert.Equal(3,rows.Count);
        foreach(var row in rows.OrderBy(row=>row["_id"].ToString()))
            output.WriteLine($"Record {row["_id"]} Name={row["Name"]} Version={row["Version"]} SHA256={Convert.ToHexString(SHA256.HashData(row.ToBson()))} Bytes={row.ToBson().Length}");
        var root=Assert.Single(rows.Where(row=>row["Name"].AsString=="current collection root"));
        Assert.Equal(2,root["Version"].AsInt32);
        output.WriteLine("Current date BSON type: "+root["MetaData"]["date"].BsonType);
        using var json=System.Text.Json.JsonDocument.Parse(root["OasisHolonRevisionArchive"]["records"][0]["json"].AsString);
        output.WriteLine("Archived date JSON kind: "+json.RootElement.GetProperty("MetaData").GetProperty("date").ValueKind);
        output.WriteLine("Archived date JSON: "+json.RootElement.GetProperty("MetaData").GetProperty("date").GetRawText());
    }

    [Fact][Trait("Category","OwnedFixtureReconciliation")]
    public async Task InspectPreservedDateOrSearchFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var report=Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-history-collection-native-fixed.trx.date-or-original-failed");
        Assert.Equal("93B33A5E1093541DAAE55FF9E669937758A1BDFBA8A3B195209301439E3B6059",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        var database=new MongoClient(uri).GetDatabase("cosmic_cql_fixture");
        var rows=await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();Assert.Equal(3,rows.Count);
        var root=Assert.Single(rows.Where(row=>row["Name"]=="current collection root"));Assert.Equal(2,root["Version"].AsInt32);
        var old=Assert.Single(root["OasisHolonRevisionArchive"]["records"].AsBsonArray).AsBsonDocument;
        Assert.Equal(Convert.ToHexString(SHA256.HashData(old["bson"].AsBsonDocument.ToBson())),old["sha256"].AsString);
        Assert.Equal("historical collection root",old["bson"]["Name"].AsString);
        Assert.Equal(DateTime.MinValue,old["bson"]["ModifiedDate"].ToUniversalTime());
        output.WriteLine("Archived first-insert ModifiedDate="+old["bson"]["ModifiedDate"].ToJson()+"; current="+root["ModifiedDate"].ToJson());
        foreach(var row in rows.OrderBy(row=>row["Name"].AsString))output.WriteLine(row["Name"].AsString+"|"+row["_id"]+"|"+row["HolonId"]+"|"+Convert.ToHexString(SHA256.HashData(row.ToBson())));
        using var names=await database.ListCollectionNamesAsync();
        foreach(var collection in await names.ToListAsync())output.WriteLine("Count|"+collection+"|"+await database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var after=await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Equal(rows.OrderBy(row=>row["_id"].ToString()).Select(row=>Convert.ToHexString(SHA256.HashData(row.ToBson()))),after.OrderBy(row=>row["_id"].ToString()).Select(row=>Convert.ToHexString(SHA256.HashData(row.ToBson()))));
    }

    [Fact][Trait("Category","OwnedFixtureReconciliation")]
    public async Task CleanupVerifiedDateOrSearchFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var work=Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent");
        Assert.Equal("93B33A5E1093541DAAE55FF9E669937758A1BDFBA8A3B195209301439E3B6059",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"mongo-history-collection-native-fixed.trx.date-or-original-failed")))));
        Assert.Equal("FFBFB7951D2678572DAD3630B524F0007AE6C190A6D32FECE767E4CFDEE10126",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"mongo-search-date-original-inspection.trx")))));
        var expected=new[]{("6adcb369f7744203d121ab6c","0bc65415-e824-45b4-85ca-cf2ed3c4d766","F3DB85A7BBC69B6FBB834DC6220286201B7D7C3BD92B8E17100BD10D29D8AE35"),
            ("6adcb369f7744203d121ab6d","c67bf6a0-d2f7-417e-bd99-165b22262536","6DF682EB4518FBA728675CC90CEBB3B147BFBBC679B022B19AF3DCA41FFB24E9"),
            ("6adcb369f7744203d121ab6e","e1e8d921-4228-4bc2-a5e3-c5144d83c9d2","8FDAF97C3849D46F191F83025E83342395A9991E8D8375F9D849F43120AAAB68")};
        var client=new MongoClient(uri);var database=client.GetDatabase("cosmic_cql_fixture");
        var allowed=new[]{"Holon","AvatarDetail","CosmicHolonOutbox","CosmicOwnedFixture","Avatar"};
        using var names=await database.ListCollectionNamesAsync();Assert.Equal(allowed.OrderBy(value=>value), (await names.ToListAsync()).OrderBy(value=>value));
        foreach(var name in allowed.Where(value=>value!="Holon"))Assert.Equal(0,await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var collection=database.GetCollection<BsonDocument>("Holon");var rows=await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();Assert.Equal(3,rows.Count);
        // Verify ALL original identities and hashes before the first destructive operation.
        foreach(var item in expected){var row=Assert.Single(rows.Where(row=>row["_id"].AsObjectId.ToString()==item.Item1));Assert.Equal(item.Item2,row["HolonId"].AsString);Assert.Equal(item.Item3,Convert.ToHexString(SHA256.HashData(row.ToBson())));}
        foreach(var row in rows){var deleted=await collection.DeleteOneAsync(new BsonDocument{{"_id",row["_id"]},{"$expr",new BsonDocument("$eq",new BsonArray{"$$ROOT",new BsonDocument("$literal",row)})}});Assert.True(deleted.IsAcknowledged);Assert.Equal(1,deleted.DeletedCount);Assert.Equal(0,await collection.CountDocumentsAsync(new BsonDocument("_id",row["_id"])));}
        foreach(var name in allowed)Assert.Equal(0,await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        await client.DropDatabaseAsync("cosmic_cql_fixture");using var remaining=await client.ListDatabaseNamesAsync();Assert.DoesNotContain("cosmic_cql_fixture",await remaining.ToListAsync());
    }

    [Fact][Trait("Category","OwnedFixtureReconciliation")]
    public async Task InspectPreservedSearchBatchFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        Assert.Equal("207561F1FE58DF482A7C08ADE115FC87411473D08F7E36A006B6F7803754A6D1",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-history-collection-native-fixed.trx.batch-original-failed")))));
        const string name="cosmic_cql_fixture";var database=new MongoClient(uri).GetDatabase(name);
        var rows=await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();Assert.Equal(273,rows.Count);
        var journal=await database.GetCollection<BsonDocument>("CosmicOwnedFixture").Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();Assert.Equal("owned-historical-search-batch",journal["kind"].AsString);
        var entries=journal["rows"].AsBsonArray;Assert.Equal(270,entries.Count);Assert.Equal(270,entries.Select(row=>row["_id"].AsObjectId).Distinct().Count());
        var ids=entries.Select(row=>row["_id"].AsObjectId).ToHashSet();
        foreach(var entry in entries){var row=Assert.Single(rows.Where(row=>row["_id"]==entry["_id"]));Assert.Equal(entry["sha256"].AsString,Convert.ToHexString(SHA256.HashData(row.ToBson())));Assert.Equal("current batch root",row["Name"].AsString);}
        output.WriteLine("JournalSHA="+Convert.ToHexString(SHA256.HashData(journal.ToBson())));
        foreach(var row in rows.Where(row=>!ids.Contains(row["_id"].AsObjectId)).OrderBy(row=>row["Name"].AsString))output.WriteLine("Original|"+row["Name"].AsString+"|"+row["_id"]+"|"+Convert.ToHexString(SHA256.HashData(row.ToBson())));
        _=new Provider(uri,name);var repository=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories.SearchRepository(new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories.MongoDbContext(uri,name));
        foreach(var type in new[]{HolonType.Default,HolonType.Park})foreach(var synchronous in new[]{false,true})
        {
            var query=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,
                SearchGroups=new(){new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup{SearchQuery="historical batch root",HolonType=type,SearchHolons=true}}};
            var found=synchronous?repository.Search(query,1):await repository.SearchAsync(query,1);Assert.False(found.IsError,found.Message+found.Exception);
            Assert.Equal(type==HolonType.Park?270:0,found.Result.SearchResultHolons.Count);
        }
        var after=await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Equal(rows.OrderBy(row=>row["_id"].ToString()).Select(row=>Convert.ToHexString(SHA256.HashData(row.ToBson()))),after.OrderBy(row=>row["_id"].ToString()).Select(row=>Convert.ToHexString(SHA256.HashData(row.ToBson()))));
    }

    [Fact][Trait("Category","OwnedFixtureReconciliation")]
    public async Task CleanupVerifiedSearchBatchFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var work=Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent");
        Assert.Equal("207561F1FE58DF482A7C08ADE115FC87411473D08F7E36A006B6F7803754A6D1",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"mongo-history-collection-native-fixed.trx.batch-original-failed")))));
        Assert.Equal("CD56B3C73F096373694EE5F668AB498A4DC7F3C794D6F712D740504058F1DD1A",Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(work,"mongo-search-batch-original-inspection.trx")))));
        var client=new MongoClient(uri);var database=client.GetDatabase("cosmic_cql_fixture");var allowed=new[]{"Holon","AvatarDetail","CosmicHolonOutbox","CosmicOwnedFixture","Avatar"};
        using var names=await database.ListCollectionNamesAsync();Assert.Equal(allowed.OrderBy(value=>value),(await names.ToListAsync()).OrderBy(value=>value));
        foreach(var name in allowed.Where(value=>value!="Holon"&&value!="CosmicOwnedFixture"))Assert.Equal(0,await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var markers=database.GetCollection<BsonDocument>("CosmicOwnedFixture");var journal=await markers.Find(FilterDefinition<BsonDocument>.Empty).SingleAsync();
        Assert.Equal("4175821BF4287CB8445587999B12B32744CD52407C0CEC09BA36CD9DCA61BCA6",Convert.ToHexString(SHA256.HashData(journal.ToBson())));
        var expected=journal["rows"].AsBsonArray.ToDictionary(row=>row["_id"].AsObjectId,row=>row["sha256"].AsString);Assert.Equal(270,expected.Count);
        expected.Add(ObjectId.Parse("6adcb54b4124e6c0d069971e"),"730E460A48781890EAF1CB3F2C15A274B8073FF5D912698CE7E18EACF1C830C4");
        expected.Add(ObjectId.Parse("6adcb54b4124e6c0d069971f"),"4AA68AF3D8EAFE8A7107F86ECEB7AC6954C40BE2BD89A0E3E6FDE6AD663B954B");
        expected.Add(ObjectId.Parse("6adcb54b4124e6c0d0699720"),"348F208CBEDF48CF7D9EB889DDF88B4226F6C6FF0BD739D4ECD03A3A343DE01F");
        var collection=database.GetCollection<BsonDocument>("Holon");var rows=await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();Assert.Equal(273,rows.Count);
        foreach(var row in rows){Assert.True(expected.TryGetValue(row["_id"].AsObjectId,out var hash));Assert.Equal(hash,Convert.ToHexString(SHA256.HashData(row.ToBson())));}
        var deletes=rows.Select(row=>(WriteModel<BsonDocument>)new DeleteOneModel<BsonDocument>(new BsonDocument{{"_id",row["_id"]},{"$expr",new BsonDocument("$eq",new BsonArray{"$$ROOT",new BsonDocument("$literal",row)})}})).ToArray();
        var removed=await collection.WithWriteConcern(WriteConcern.WMajority.With(journal:true)).BulkWriteAsync(deletes);Assert.True(removed.IsAcknowledged);Assert.Equal(273,removed.DeletedCount);Assert.Equal(0,await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var deletedJournal=await markers.DeleteOneAsync(new BsonDocument("$expr",new BsonDocument("$eq",new BsonArray{"$$ROOT",new BsonDocument("$literal",journal)})));Assert.True(deletedJournal.IsAcknowledged);Assert.Equal(1,deletedJournal.DeletedCount);
        foreach(var name in allowed)Assert.Equal(0,await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        await client.DropDatabaseAsync("cosmic_cql_fixture");using var remaining=await client.ListDatabaseNamesAsync();Assert.DoesNotContain("cosmic_cql_fixture",await remaining.ToListAsync());
    }

    public static IEnumerable<object[]> HistoricalCollectionCases(){foreach(var kind in Enumerable.Range(0,9))foreach(var sync in new[]{false,true})yield return new object[]{kind,sync};}

    [Theory]
    [MemberData(nameof(HistoricalCollectionCases))]
    public async Task HistoricalCollectionsUseArchivedPredicatesAndIndependentChildren(int kind,bool sync)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(kind==7?uri+"&appName=OwnedHistoricalSearchFault":uri,name);var complete=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var ancestor=await provider.SaveHolonAsync(new Holon{Id=Guid.NewGuid(),Version=9,VersionId=Guid.NewGuid(),HolonType=HolonType.Game,Name="anchor"},saveChildren:false);
            Assert.False(ancestor.IsError,ancestor.Message+ancestor.Exception);
            var oldModified=new DateTime(2026,10,23,0,0,0,DateTimeKind.Utc);var newModified=oldModified.AddDays(1);
            var root=new Holon{Id=Guid.NewGuid(),ParentHolonId=ancestor.Result.Id,Version=1,VersionId=Guid.NewGuid(),HolonType=HolonType.Park,Name="historical collection root",ModifiedDate=oldModified,
                MetaData=new(){["literal.$key"]="$old",["bool"]=true,["number"]=123,["large"]=9007199254740993L,["fraction"]=1.25d,
                    ["date"]=new DateTime(2026,10,24,0,0,0,DateTimeKind.Utc),["nullable"]=null,["compound"]=new[]{1,2}}};
            var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Version=7,VersionId=Guid.NewGuid(),HolonType=HolonType.Quest,Name="initial child"};
            root.Children=new List<IHolon>{child};
            var first=await provider.SaveHolonAsync(root,saveChildren:true);Assert.False(first.IsError,first.Message+first.Exception);oldModified=first.Result.ModifiedDate;
            var current=first.Result;current.PreviousVersionId=current.VersionId;current.Version=2;current.VersionId=Guid.NewGuid();current.Name="current collection root";
            current.MetaData["literal.$key"]="$new";current.MetaData["bool"]=false;current.ModifiedDate=newModified;
            var second=await provider.SaveHolonAsync(current,saveChildren:false);Assert.False(second.IsError,second.Message+second.Exception);
            var loadedChild=await provider.LoadHolonAsync(child.Id,loadChildren:false);Assert.NotNull(loadedChild.Result);
            loadedChild.Result.PreviousVersionId=loadedChild.Result.VersionId;loadedChild.Result.Version=8;loadedChild.Result.VersionId=Guid.NewGuid();
            var savedChild=await provider.SaveHolonAsync(loadedChild.Result,saveChildren:false);Assert.False(savedChild.IsError,savedChild.Message+savedChild.Exception);
            var native=database.GetCollection<BsonDocument>("Holon");
            async Task<string[]> Snapshot()=>(await native.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Select(row=>row.ToJson()).OrderBy(row=>row,StringComparer.Ordinal).ToArray();
            var before=await Snapshot();var parentKey=ancestor.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
            var pairs=kind==4?new Dictionary<string,string>{{"literal.$key","$old"},{"missing","ignored"}}:new Dictionary<string,string>{{"literal.$key","$old"},{"bool","True"}};
            var mode=kind==4?MetaKeyValuePairMatchMode.Any:MetaKeyValuePairMatchMode.All;
            var result=kind switch{
                0=>sync?provider.LoadAllHolons(HolonType.Park,loadChildrenFromProvider:true,version:1):await provider.LoadAllHolonsAsync(HolonType.Park,loadChildrenFromProvider:true,version:1),
                1=>sync?provider.LoadHolonsForParent(ancestor.Result.Id,HolonType.Park,loadChildrenFromProvider:true,version:1):await provider.LoadHolonsForParentAsync(ancestor.Result.Id,HolonType.Park,loadChildrenFromProvider:true,version:1),
                2=>sync?provider.LoadHolonsForParent(parentKey,HolonType.Park,loadChildrenFromProvider:true,version:1):await provider.LoadHolonsForParentAsync(parentKey,HolonType.Park,loadChildrenFromProvider:true,version:1),
                3=>sync?provider.LoadHolonsByMetaData("literal.$key","$old",HolonType.Park,loadChildrenFromProvider:true,version:1):await provider.LoadHolonsByMetaDataAsync("literal.$key","$old",HolonType.Park,loadChildrenFromProvider:true,version:1),
                _=>sync?provider.LoadHolonsByMetaData(pairs,mode,HolonType.Park,loadChildrenFromProvider:true,version:1):await provider.LoadHolonsByMetaDataAsync(pairs,mode,HolonType.Park,loadChildrenFromProvider:true,version:1)
            };
            Assert.False(result.IsError,result.Message+result.Exception);Assert.True(result.IsLoaded);Assert.Equal(1,result.LoadedCount);
            var selected=Assert.Single(result.Result);Assert.Equal("historical collection root",selected.Name);Assert.Equal(1,selected.Version);
            Assert.Equal(8,Assert.Single(selected.Children).Version);
            if(kind is 4 or 5)foreach(var selectedVersion in new[]{0,1})foreach(var clear in new[]{false,true}){
                var originalClient=provider.Database.MongoClient;var changes=0;
                var intent=new Dictionary<string,string>{{"literal.$key",selectedVersion==1?"$old":"$new"},{"bool",selectedVersion==1?"True":"False"}};
                provider.Database.MongoClient=SessionIntentBoundary.Wrap(originalClient,()=>{
                    changes++;if(clear){intent.Clear();intent.Add("missing","changed");}
                    else{intent["literal.$key"]="changed";intent["bool"]="changed";}
                });
                OASISResult<IEnumerable<IHolon>> stable;
                try{stable=sync?provider.LoadHolonsByMetaData(intent,mode,HolonType.Park,loadChildrenFromProvider:true,version:selectedVersion)
                    :await provider.LoadHolonsByMetaDataAsync(intent,mode,HolonType.Park,loadChildrenFromProvider:true,version:selectedVersion);}
                finally{provider.Database.MongoClient=originalClient;}
                Assert.Equal(1,changes);Assert.False(stable.IsError,stable.Message+stable.Exception);Assert.Equal(1,stable.LoadedCount);
                var stableRoot=Assert.Single(stable.Result);Assert.Equal(root.Id,stableRoot.Id);Assert.Equal(selectedVersion==1?1:2,stableRoot.Version);
                Assert.Equal(8,Assert.Single(stableRoot.Children).Version);
            }
            foreach(var pair in new Dictionary<string,string>{{"number","123"},{"large","9007199254740993"},{"fraction","1.25"},{"date","2026-10-24T00:00:00.000Z"},{"nullable",""},{"compound","[1,2]"}})
            {
                var latest=await provider.LoadHolonsByMetaDataAsync(pair.Key,pair.Value,HolonType.Park,loadChildren:false);
                var historical=await provider.LoadHolonsByMetaDataAsync(pair.Key,pair.Value,HolonType.Park,loadChildren:false,version:1);
                Assert.False(latest.IsError,latest.Message+latest.Exception);Assert.False(historical.IsError,historical.Message+historical.Exception);
                Assert.Equal(latest.Result.Select(row=>row.Id),historical.Result.Select(row=>row.Id));
            }
            var wrong=await provider.LoadHolonsByMetaDataAsync("literal.$key","$new",HolonType.Park,loadChildren:false,version:1);
            Assert.False(wrong.IsError,wrong.Message+wrong.Exception);Assert.Empty(wrong.Result);
            foreach(var searchCase in new[]{(Text:"historical collection root",Version:1,Value:"$old",Count:1),
                (Text:"current collection root",Version:1,Value:"$old",Count:0),(Text:"historical collection root",Version:0,Value:"$old",Count:0),
                (Text:"current collection root",Version:2,Value:"$new",Count:1),(Text:"historical collection root",Version:1,Value:"$new",Count:0)})
            {
                var query=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,ParentId=ancestor.Result.Id,
                    FilterByMetaData=new(){{"literal.$key",searchCase.Value}},MetaKeyValuePairMatchMode=MetaKeyValuePairMatchMode.All,
                    SearchGroups=new(){new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup{SearchQuery=searchCase.Text,SearchHolons=true,SearchAvatars=false,HolonType=HolonType.Park}}};
                if(kind==5)query.SearchGroups.Add(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchNumberGroup{
                    Number=searchCase.Version==0?2:searchCase.Version,NumberOperator=SearchOperatorType.EqualTo,
                    PreviousSearchGroupOperator=SearchParamGroupOperator.And,SearchHolons=true,SearchAvatars=false,HolonType=HolonType.Park});
                if(kind==3){query.SearchGroups[0].SearchHolons=false;query.SearchGroups[0].SearchAvatars=false;}
                var repositoryField=typeof(Provider).GetField("_searchRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                var originalRepository=(NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces.ISearchRepository)repositoryField.GetValue(provider)!;
                if(kind==0)repositoryField.SetValue(provider,new SearchIntentBoundary(originalRepository,()=>{
                    query.ParentId=Guid.NewGuid();query.FilterByMetaData["literal.$key"]="caller changed";
                    ((NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup)query.SearchGroups[0]).SearchQuery="caller changed";
                }));
                OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.Search.ISearchResults> found;
                try{found=sync?provider.Search(query,loadChildren:false,version:searchCase.Version):await provider.SearchAsync(query,loadChildren:false,version:searchCase.Version);}
                finally{repositoryField.SetValue(provider,originalRepository);}
                if(kind==0)Assert.Equal("caller changed",query.FilterByMetaData["literal.$key"]);
                Assert.False(found.IsError,found.Message+found.Exception);Assert.True(found.IsLoaded);Assert.Equal(searchCase.Count,found.LoadedCount);
                Assert.Equal(searchCase.Count,found.Result.SearchResultHolons.Count);
                if(searchCase.Count==1){Assert.Equal(root.Id,Assert.Single(found.Result.SearchResultHolons).Id);Assert.Equal(searchCase.Version,Assert.Single(found.Result.SearchResultHolons).Version);}
            }
            foreach(var nullGroups in new[]{false,true})foreach(var selectedVersion in new[]{0,1,999})
            {
                var metadataOnly=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,ParentId=ancestor.Result.Id,SearchGroups=new(),
                    FilterByMetaData=new(){{"literal.$key","$old"}},MetaKeyValuePairMatchMode=MetaKeyValuePairMatchMode.All};
                if(nullGroups)metadataOnly.SearchGroups=null;
                var found=sync?provider.Search(metadataOnly,loadChildren:false,version:selectedVersion):await provider.SearchAsync(metadataOnly,loadChildren:false,version:selectedVersion);
                Assert.False(found.IsError,found.Message+found.Exception);Assert.True(found.IsLoaded);
                Assert.Equal(selectedVersion==1?1:0,found.LoadedCount);
                if(selectedVersion==1)Assert.Equal(root.Id,Assert.Single(found.Result.SearchResultHolons).Id);
            }
            if(kind==0)foreach(var dateCase in new[]{(Version:1,Date:oldModified,Count:1),(Version:1,Date:newModified,Count:0),
                (Version:2,Date:newModified,Count:1),(Version:0,Date:oldModified,Count:0)})
            {
                var dateQuery=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,ParentId=ancestor.Result.Id,
                    SearchGroups=new(){new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup{SearchQuery="no-owned-record-matches-this",SearchHolons=true,HolonType=HolonType.Park},
                        new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchDateGroup{Date=dateCase.Date,DateOperator=SearchOperatorType.EqualTo,
                            PreviousSearchGroupOperator=SearchParamGroupOperator.Or,HolonType=HolonType.Park,
                            HolonSearchParams=new SearchHolonParams{ModifiedDate=true}}}};
                var found=sync?provider.Search(dateQuery,loadChildren:false,version:dateCase.Version):await provider.SearchAsync(dateQuery,loadChildren:false,version:dateCase.Version);
                Assert.False(found.IsError,found.Message+found.Exception);Assert.Equal(dateCase.Count,found.LoadedCount);
                if(dateCase.Count==1){Assert.Equal(root.Id,Assert.Single(found.Result.SearchResultHolons).Id);Assert.Equal(dateCase.Date,Assert.Single(found.Result.SearchResultHolons).ModifiedDate);}
            }
            if(kind==8)
            {
                var childQuery=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,
                    SearchGroups=new(){new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup{SearchQuery="historical collection root",HolonType=HolonType.Park,SearchHolons=true}}};
                foreach(var graph in new[]{(Load:false,Recursive:false,Depth:0),(Load:true,Recursive:false,Depth:0),(Load:true,Recursive:true,Depth:1)})
                {
                    var found=sync?provider.Search(childQuery,loadChildren:graph.Load,recursive:graph.Recursive,maxChildDepth:graph.Depth,version:1)
                        :await provider.SearchAsync(childQuery,loadChildren:graph.Load,recursive:graph.Recursive,maxChildDepth:graph.Depth,version:1);
                    Assert.False(found.IsError,found.Message+found.Exception);Assert.Equal(1,found.LoadedCount);var selectedRoot=Assert.Single(found.Result.SearchResultHolons);Assert.Equal(1,selectedRoot.Version);
                    if(graph.Load){var embedded=Assert.Single(selectedRoot.Children);Assert.Equal(child.Id,embedded.Id);Assert.Equal(7,embedded.Version);Assert.Empty(embedded.Children);}
                    else Assert.Empty(selectedRoot.Children);
                }
                // Search has no provider-linked-child option: current native child8 is independent.
                Assert.Equal(8,(await native.Find(new BsonDocument("HolonId",child.Id.ToString("D"))).SingleAsync())["Version"].AsInt32);
            }
            if(kind is 6 or 7)
            {
                var original=await native.Find(new BsonDocument("HolonId",root.Id.ToString("D"))).SingleAsync();
                var batch=new List<BsonDocument>();
                for(var index=0;index<270;index++)
                {
                    var row=original.DeepClone().AsBsonDocument;var publicId=Guid.NewGuid().ToString("D");var physical=ObjectId.GenerateNewId();
                    row["_id"]=physical;row["HolonId"]=publicId;row["Name"]="current batch root";row["Children"]=new BsonArray();
                    row["MetaData"]["batch-pad"]=new string('x',8192);
                    var archived=row["OasisHolonRevisionArchive"]["records"][0].AsBsonDocument;var old=archived["bson"].AsBsonDocument;
                    old["_id"]=physical;old["HolonId"]=publicId;old["Name"]="historical batch root";old["Children"]=new BsonArray();old["MetaData"]["batch-pad"]=new string('x',8192);
                    archived["sha256"]=Convert.ToHexString(SHA256.HashData(old.ToBson()));batch.Add(row);
                }
                var journal=new BsonDocument{{"kind","owned-historical-search-batch"},{"rows",new BsonArray(batch.Select(row=>new BsonDocument{{"_id",row["_id"]},{"sha256",Convert.ToHexString(SHA256.HashData(row.ToBson()))}}))}};
                var markers=database.GetCollection<BsonDocument>("CosmicOwnedFixture").WithWriteConcern(WriteConcern.WMajority.With(journal:true));
                await markers.InsertOneAsync(journal);await native.WithWriteConcern(WriteConcern.WMajority.With(journal:true)).InsertManyAsync(batch);
                output.WriteLine("Owned batch native bytes="+batch.Sum(row=>(long)row.ToBson().Length));
                var batchQuery=new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,
                    SearchGroups=new(){new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup{SearchQuery="historical batch root",HolonType=HolonType.Park,SearchHolons=true,SearchAvatars=false}}};
                var found=sync?provider.Search(batchQuery,loadChildren:false,version:1):await provider.SearchAsync(batchQuery,loadChildren:false,version:1);
                Assert.False(found.IsError,found.Message+found.Exception);Assert.Equal(270,found.LoadedCount);Assert.Equal(270,found.Result.NumberOfResults);
                Assert.All(found.Result.SearchResultHolons,row=>{Assert.Equal(1,row.Version);Assert.Equal("historical batch root",row.Name);});
                Assert.Equal(batch.Select(row=>row["HolonId"].AsString).OrderBy(value=>value),found.Result.SearchResultHolons.Select(row=>row.Id.ToString("D")).OrderBy(value=>value));
                if(kind==7)
                {
                    var admin=client.GetDatabase("admin");
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode",new BsonDocument("skip",1)},
                        {"data",new BsonDocument{{"failCommands",new BsonArray{"aggregate"}},{"appName","OwnedHistoricalSearchFault"},{"errorCode",2}}}});
                    try
                    {
                        var failed=sync?provider.Search(batchQuery,loadChildren:false,version:1):await provider.SearchAsync(batchQuery,loadChildren:false,version:1);
                        Assert.True(failed.IsError);Assert.False(failed.IsLoaded);Assert.Equal(0,failed.LoadedCount);Assert.Null(failed.Result);
                        Assert.Equal(2,Assert.IsType<MongoCommandException>(failed.Exception).Code);
                    }
                    finally{await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});}
                }
                var ids=new BsonArray(batch.Select(row=>row["_id"]));var stored=await native.Find(new BsonDocument("_id",new BsonDocument("$in",ids))).ToListAsync();Assert.Equal(270,stored.Count);
                foreach(var row in batch)Assert.Equal(row.ToBson(),Assert.Single(stored.Where(item=>item["_id"]==row["_id"])).ToBson());
                var deletes=batch.Select(row=>(WriteModel<BsonDocument>)new DeleteOneModel<BsonDocument>(new BsonDocument{{"_id",row["_id"]},{"$expr",new BsonDocument("$eq",new BsonArray{"$$ROOT",new BsonDocument("$literal",row)})}})).ToArray();
                var removed=await native.WithWriteConcern(WriteConcern.WMajority.With(journal:true)).BulkWriteAsync(deletes);Assert.True(removed.IsAcknowledged);Assert.Equal(270,removed.DeletedCount);
                Assert.Equal(0,await native.CountDocumentsAsync(new BsonDocument("_id",new BsonDocument("$in",ids))));
                var markerRemoved=await markers.DeleteOneAsync(new BsonDocument("$expr",new BsonDocument("$eq",new BsonArray{"$$ROOT",new BsonDocument("$literal",journal)})));Assert.True(markerRemoved.IsAcknowledged);Assert.Equal(1,markerRemoved.DeletedCount);
            }
            Assert.Equal(before,await Snapshot());complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_cql_fixture historical collection evidence for reviewed reconciliation.");
        }
    }

    [Theory]
    [InlineData(false,false,false)] [InlineData(false,false,true)]
    [InlineData(false,true,false)] [InlineData(false,true,true)]
    [InlineData(true,false,false)] [InlineData(true,false,true)]
    [InlineData(true,true,false)] [InlineData(true,true,true)]
    public async Task HistoricalPointReadSelectsRootButKeepsIndependentChildVersion(bool byKey,bool sync,bool providerChildren)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var root=new Holon{Id=Guid.NewGuid(),Version=1,VersionId=Guid.NewGuid(),Name="historical root"};
            var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Version=7,VersionId=Guid.NewGuid(),Name="initial child"};
            root.Children=new List<IHolon>{child};
            var first=await provider.SaveHolonAsync(root,saveChildren:true);
            Assert.False(first.IsError,first.Message+first.Exception);
            var current=first.Result;current.PreviousVersionId=current.VersionId;current.Version=2;current.VersionId=Guid.NewGuid();current.Name="current root";
            var second=await provider.SaveHolonAsync(current,saveChildren:false);
            Assert.False(second.IsError,second.Message+second.Exception);
            var storedChild=await provider.LoadHolonAsync(child.Id,loadChildren:false);
            Assert.False(storedChild.IsError,storedChild.Message+storedChild.Exception);Assert.NotNull(storedChild.Result);
            storedChild.Result.PreviousVersionId=storedChild.Result.VersionId;storedChild.Result.Version=8;storedChild.Result.VersionId=Guid.NewGuid();storedChild.Result.Name="independent current child";
            var savedChild=await provider.SaveHolonAsync(storedChild.Result,saveChildren:false);
            Assert.False(savedChild.IsError,savedChild.Message+savedChild.Exception);
            var native=database.GetCollection<BsonDocument>("Holon");
            async Task<string[]> Snapshot()=>(await native.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Select(row=>row.ToJson()).OrderBy(row=>row,StringComparer.Ordinal).ToArray();
            var before=await Snapshot();var key=current.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
            var loaded=byKey
                ?sync?provider.LoadHolon(key,loadChildren:providerChildren,loadChildrenFromProvider:providerChildren,version:1):await provider.LoadHolonAsync(key,loadChildren:providerChildren,loadChildrenFromProvider:providerChildren,version:1)
                :sync?provider.LoadHolon(root.Id,loadChildren:providerChildren,loadChildrenFromProvider:providerChildren,version:1):await provider.LoadHolonAsync(root.Id,loadChildren:providerChildren,loadChildrenFromProvider:providerChildren,version:1);
            Assert.False(loaded.IsError,loaded.Message+loaded.Exception);Assert.True(loaded.IsLoaded);Assert.Equal(1,loaded.LoadedCount);
            Assert.Equal("historical root",loaded.Result.Name);Assert.Equal(1,loaded.Result.Version);
            if(providerChildren){var selected=Assert.Single(loaded.Result.Children);Assert.Equal(8,selected.Version);Assert.Equal("independent current child",selected.Name);}
            else Assert.Empty(loaded.Result.Children);
            Assert.Equal(before,await Snapshot());complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_cql_fixture historical point-read evidence for reviewed reconciliation.");
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task HostedDomainMutationPreservesExistingArchiveAndUnknownFields(bool softDelete)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var owner=Guid.NewGuid();
            var first=await provider.SaveHolonAsync(new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,Version=1,VersionId=Guid.NewGuid(),Name="original"},saveChildren:false);
            Assert.False(first.IsError,first.Message+first.Exception);
            var updated=first.Result;updated.Version=2;updated.VersionId=Guid.NewGuid();updated.Name="saved replacement";
            var second=await provider.SaveHolonAsync(updated,saveChildren:false);
            Assert.False(second.IsError,second.Message+second.Exception);
            var native=database.GetCollection<BsonDocument>("Holon");
            var marked=await native.UpdateOneAsync(FilterDefinition<BsonDocument>.Empty,Builders<BsonDocument>.Update.Set("FixtureUnknown","retained"));
            Assert.True(marked.IsAcknowledged);Assert.Equal(1,marked.MatchedCount);
            var mutation=new HostedSyncFanOutItem{OperationId=Guid.NewGuid(),AvatarId=owner,EntityId=updated.Id,EntityType=HyperDriveEntityTypes.Holon,
                Kind=SyncOperationKind.Upsert,VersionId=Guid.NewGuid(),PayloadJson="{\"name\":\"domain replacement\"}"};
            var applied=await provider.ApplyDomainMutationAsync(mutation,default);
            Assert.False(applied.IsError,applied.Message+applied.Exception);
            var beforeReplay=Assert.Single(await native.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToBson();
            var replay=await provider.ApplyDomainMutationAsync(mutation,default);
            Assert.False(replay.IsError,replay.Message+replay.Exception);Assert.True(replay.Result.AlreadyApplied);
            Assert.Equal(beforeReplay,Assert.Single(await native.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToBson());
            if(softDelete)
            {
                mutation.OperationId=Guid.NewGuid();mutation.VersionId=Guid.NewGuid();mutation.Kind=SyncOperationKind.Delete;mutation.PayloadJson=null;
                var deleted=await provider.ApplyDomainMutationAsync(mutation,default);
                Assert.False(deleted.IsError,deleted.Message+deleted.Exception);
            }
            var exported=await provider.ExportAllAsync(-1);
            Assert.False(exported.IsError,exported.Message+exported.Exception);
            Assert.Equal(new[]{"original","saved replacement","domain replacement"},exported.Result.OrderBy(row=>row.Version).Select(row=>row.Name));
            var stored=Assert.Single(await native.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
            Assert.Equal("retained",stored["FixtureUnknown"].AsString);
            Assert.True(stored.Contains("CosmicCqlMembership"));
            Assert.Equal(softDelete?4:3,stored["Version"].AsInt32);
            complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_cql_fixture hosted-domain history evidence for reviewed reconciliation.");
        }
    }

    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task ReconcileReviewedHistoricalExportFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var client=new MongoClient(uri);var database=client.GetDatabase("cosmic_cql_fixture");
        var names=(await (await database.ListCollectionNamesAsync()).ToListAsync()).OrderBy(name=>name).ToArray();
        Assert.Equal(new[]{"Avatar","AvatarDetail","CosmicHolonOutbox","CosmicOwnedFixture","Holon"},names);
        foreach(var name in names.Where(name=>name!="Holon"))
            Assert.Equal(0,await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var collection=database.GetCollection<BsonDocument>("Holon");
        var row=Assert.Single(await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
        Assert.Equal("6adc622b670796152fa9f7b0",row["_id"].AsObjectId.ToString());
        Assert.Equal("57143547952E8227826C56F9EA15C2B849C8B6F0948B18CA7976E0D5AABF73EB",Convert.ToHexString(SHA256.HashData(row.ToBson())));
        var exact=new BsonDocument{{"_id",row["_id"]},{"$expr",new BsonDocument("$eq",new BsonArray{"$$ROOT",new BsonDocument("$literal",row)})}};
        var deleted=await collection.DeleteOneAsync(exact);
        Assert.True(deleted.IsAcknowledged);Assert.Equal(1,deleted.DeletedCount);
        foreach(var name in names)
            Assert.Equal(0,await database.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        await client.DropDatabaseAsync("cosmic_cql_fixture");
        output.WriteLine("Removed only reviewed hash-matched synthetic record and verified-empty fixture database; original inspection/failure reports retained.");
    }

    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task InspectPreservedHistoricalExportFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var database=new MongoClient(uri).GetDatabase("cosmic_cql_fixture");
        var collections=(await (await database.ListCollectionNamesAsync()).ToListAsync()).OrderBy(name=>name).ToArray();
        output.WriteLine("Collections: "+string.Join(",",collections));
        Assert.Contains("CosmicOwnedFixture",collections);
        Assert.Equal(0,await database.GetCollection<BsonDocument>("CosmicOwnedFixture").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var rows=await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        var row=Assert.Single(rows);
        Assert.Equal("current replacement",row["Name"].AsString);
        Assert.Equal(2,row["Version"].AsInt32);
        Assert.False(row.Contains("OasisHolonRevisionArchive"));
        output.WriteLine("Native ID: "+row["_id"]);
        output.WriteLine("BSON SHA256: "+Convert.ToHexString(SHA256.HashData(row.ToBson())));
        output.WriteLine("BSON bytes: "+row.ToBson().Length);
    }

    [Theory]
    [InlineData(0,false)] [InlineData(0,true)]
    [InlineData(1,false)] [InlineData(1,true)]
    [InlineData(2,false)] [InlineData(2,true)]
    [InlineData(3,false)] [InlineData(3,true)]
    public async Task HistoricalExportRetainsPreviouslySavedLogicalVersion(int selector,bool sync)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";
        var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);
        await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);
        var complete=false;
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var owner=new Avatar{Id=Guid.NewGuid(),Username="history-owner",Email="history-owner@example.invalid",Version=1};
            var savedOwner=await provider.SaveAvatarAsync(owner);
            Assert.False(savedOwner.IsError,savedOwner.Message+savedOwner.Exception);
            async Task<OASISResult<IEnumerable<IHolon>>> Export(int version)=>selector switch{
                0=>sync?provider.ExportAll(version):await provider.ExportAllAsync(version),
                1=>sync?provider.ExportAllDataForAvatarById(owner.Id,version):await provider.ExportAllDataForAvatarByIdAsync(owner.Id,version),
                2=>sync?provider.ExportAllDataForAvatarByUsername(owner.Username,version):await provider.ExportAllDataForAvatarByUsernameAsync(owner.Username,version),
                _=>sync?provider.ExportAllDataForAvatarByEmail(owner.Email,version):await provider.ExportAllDataForAvatarByEmailAsync(owner.Email,version)
            };
            var original=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner.Id,Version=1,VersionId=Guid.NewGuid(),Name="historical original"};
            var first=await provider.SaveHolonAsync(original,saveChildren:false);
            Assert.False(first.IsError,first.Message+first.Exception);
            var current=first.Result;
            current.PreviousVersionId=current.VersionId;
            current.VersionId=Guid.NewGuid();current.Version=2;current.Name="current replacement";
            var second=await provider.SaveHolonAsync(current,saveChildren:false);
            Assert.False(second.IsError,second.Message+second.Exception);
            var native=database.GetCollection<BsonDocument>("Holon");
            var beforeBackward=Assert.Single(await native.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToBson();
            current.Version=1;current.Name="rejected backwards replacement";
            var backward=await provider.SaveHolonAsync(current,saveChildren:false);
            Assert.True(backward.IsError);
            Assert.Equal(beforeBackward,Assert.Single(await native.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).ToBson());
            var latest=await Export(0);
            Assert.False(latest.IsError,latest.Message+latest.Exception);
            Assert.Equal("current replacement",Assert.Single(latest.Result).Name);
            var historical=await Export(1);
            Assert.False(historical.IsError,historical.Message+historical.Exception);
            var retained=Assert.Single(historical.Result);
            Assert.Equal("historical original",retained.Name);
            Assert.Equal(1,retained.Version);
            var all=await Export(-1);
            Assert.False(all.IsError,all.Message+all.Exception);
            Assert.Equal(new[]{1,2},all.Result.OrderBy(row=>row.Version).Select(row=>row.Version));
            complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_cql_fixture historical-export failure; do not rerun before reconciliation.");
        }
    }

    [Theory]
    [InlineData(0,false)] [InlineData(0,true)]
    [InlineData(1,false)] [InlineData(1,true)]
    [InlineData(2,false)] [InlineData(2,true)]
    [InlineData(3,false)] [InlineData(3,true)]
    public async Task CurrentExportsPreserveOwnerAndStoredPayload(int selector,bool sync)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";
        var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);
        await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);
        var complete=false;
        try
        {
            var activated=await provider.ActivateProviderAsync();
            Assert.False(activated.IsError,activated.Message);
            var owner=new Avatar{Id=Guid.NewGuid(),Username="export-owner",Email="export-owner@example.invalid",Version=1};
            var savedAvatar=await provider.SaveAvatarAsync(owner);
            Assert.False(savedAvatar.IsError,savedAvatar.Message+savedAvatar.Exception);
            var own=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner.Id,Name="owned export payload",Version=1,MetaData=new(){["export-marker"]="preserve-me"}};
            var foreign=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),Name="foreign export payload",Version=1};
            foreach(var row in new[]{own,foreign})
            {
                var saved=await provider.SaveHolonAsync(row,saveChildren:false);
                Assert.False(saved.IsError,saved.Message+saved.Exception);
            }
            var collection=database.GetCollection<BsonDocument>("Holon");
            async Task<string[]> Snapshot()=> (await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).Select(row=>row.ToJson()).OrderBy(row=>row,StringComparer.Ordinal).ToArray();
            var before=await Snapshot();
            var result=selector switch{
                0=>sync?provider.ExportAll():await provider.ExportAllAsync(),
                1=>sync?provider.ExportAllDataForAvatarById(owner.Id):await provider.ExportAllDataForAvatarByIdAsync(owner.Id),
                2=>sync?provider.ExportAllDataForAvatarByUsername(owner.Username):await provider.ExportAllDataForAvatarByUsernameAsync(owner.Username),
                _=>sync?provider.ExportAllDataForAvatarByEmail(owner.Email):await provider.ExportAllDataForAvatarByEmailAsync(owner.Email)
            };
            Assert.False(result.IsError,result.Message+result.Exception);
            var exported=result.Result.ToArray();
            Assert.True(result.IsLoaded);
            Assert.Equal(selector==0?2:1,exported.Length);
            Assert.Equal(exported.Length,result.LoadedCount);
            var owned=Assert.Single(exported.Where(row=>row.Id==own.Id));
            Assert.Equal(owner.Id,owned.CreatedByAvatarId);
            Assert.Equal("preserve-me",owned.MetaData["export-marker"].ToString());
            if(selector!=0)Assert.DoesNotContain(exported,row=>row.Id==foreign.Id);
            Assert.Equal(before,await Snapshot());
            complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_cql_fixture export evidence; reconcile before rerunning.");
        }
    }
}
