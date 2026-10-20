using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MongoDB.Driver;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;
public sealed class CosmicTypedQueryTests(Xunit.Abstractions.ITestOutputHelper output)
{
    public sealed class NativeRoleMember:Holon { }
    public sealed class NativeRoleOwner:Holon
    {
        public List<NativeRoleMember> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,members=>Components=members.Cast<NativeRoleMember>().ToList())}).ToArray();
        public override IReadOnlyList<HolonChildRoleBinding> GetChildRoleBindings()=>base.GetChildRoleBindings()
            .Concat(new[]{HolonChildRoleBinding.Create<NativeRoleMember>(nameof(Components),members=>Components=members.ToList(),
                source=>Core.Helpers.Mapper<IHolon,NativeRoleMember>.MapBaseHolonProperties(source))}).ToArray();
    }

    [Theory]
    [InlineData(false,"all")] [InlineData(true,"all")]
    [InlineData(false,"search")] [InlineData(true,"search")]
    [InlineData(false,"metadata")] [InlineData(true,"metadata")]
    [InlineData(false,"pairs")] [InlineData(true,"pairs")]
    [InlineData(false,"parent")] [InlineData(true,"parent")]
    [InlineData(false,"parent-key")] [InlineData(true,"parent-key")]
    public async Task TypedManagerQueriesRestoreRolesAndConditionallyResave(bool synchronous,string route)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string databaseName="cosmic_typed_query_fixture";
        var client=new MongoDB.Driver.MongoClient(uri);
        using var databases=await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(databaseName,await databases.ToListAsync());
        var provider=new Provider(uri,databaseName);var other=new Provider(uri,databaseName);var complete=false;
        await client.GetDatabase(databaseName).CreateCollectionAsync("CosmicOwnedFixture");
        try
        {
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        Assert.False((await other.ActivateProviderAsync()).IsError);
        var writer=Manager(provider);var reader=Manager(other);var actor=Guid.NewGuid();
        var parent=new Holon{Name="query parent"};
        const string parentKey="111111111111111111111111";
        parent.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]=parentKey;
        var root=new NativeRoleOwner{Name="typed query owner",HolonType=HolonType.Quest};
        root.MetaData["typed-kind"]="query-owner";
        root.Components.Add(new NativeRoleMember{Name="query member"});
        parent.Children.Add(root);
        var saved=await writer.SaveHolonAsync(parent,actor,new CosmicWriteOptions(Guid.NewGuid()),providerType:ProviderType.MongoDBOASIS);
        Assert.False(saved.IsError,saved.Message+saved.Exception);
        var originalVersion=root.VersionId;
        var pairs=new Dictionary<string,string>{{"typed-kind","query-owner"}};
        var loaded=route switch
        {
            "search"=>synchronous ? reader.SearchHolons<NativeRoleOwner>("typed query owner",actor,searchOnlyForCurrentAvatar:false,holonType:HolonType.Quest,providerType:ProviderType.MongoDBOASIS)
                :await reader.SearchHolonsAsync<NativeRoleOwner>("typed query owner",actor,searchOnlyForCurrentAvatar:false,holonType:HolonType.Quest,providerType:ProviderType.MongoDBOASIS),
            "all"=>synchronous ? reader.LoadAllHolons<NativeRoleOwner>(holonType:HolonType.Quest,cache:false,providerType:ProviderType.MongoDBOASIS)
                :await reader.LoadAllHolonsAsync<NativeRoleOwner>(holonType:HolonType.Quest,providerType:ProviderType.MongoDBOASIS),
            "metadata"=>synchronous ? reader.LoadHolonsByMetaData<NativeRoleOwner>("typed-kind","query-owner",providerType:ProviderType.MongoDBOASIS)
                :await reader.LoadHolonsByMetaDataAsync<NativeRoleOwner>("typed-kind","query-owner",providerType:ProviderType.MongoDBOASIS),
            "pairs"=>synchronous ? reader.LoadHolonsByMetaData<NativeRoleOwner>(pairs,MetaKeyValuePairMatchMode.All,providerType:ProviderType.MongoDBOASIS)
                :await reader.LoadHolonsByMetaDataAsync<NativeRoleOwner>(pairs,MetaKeyValuePairMatchMode.All,providerType:ProviderType.MongoDBOASIS),
            "parent"=>synchronous ? reader.LoadHolonsForParent<NativeRoleOwner>(parent.Id,providerType:ProviderType.MongoDBOASIS)
                :await reader.LoadHolonsForParentAsync<NativeRoleOwner>(parent.Id,providerType:ProviderType.MongoDBOASIS),
            _=>synchronous ? reader.LoadHolonsForParent<NativeRoleOwner>(parentKey,providerType:ProviderType.MongoDBOASIS)
                :await reader.LoadHolonsForParentAsync<NativeRoleOwner>(parentKey,providerType:ProviderType.MongoDBOASIS)
        };
        Assert.False(loaded.IsError,loaded.Message+loaded.Exception);
        var result=Assert.Single(loaded.Result);
        Assert.Equal(root.Id,result.Id);Assert.Empty(result.Children);Assert.Equal(originalVersion,result.VersionId);
        Assert.Equal("query member",Assert.Single(result.Components).Name);
        Assert.False(result.HasHolonChanged());
        result.Components[0].Name="query member edited";
        var resaved=await reader.SaveHolonAsync(result,actor,new CosmicWriteOptions(Guid.NewGuid()),providerType:ProviderType.MongoDBOASIS);
        Assert.False(resaved.IsError,resaved.Message+resaved.Exception);
        Assert.Equal(originalVersion,result.PreviousVersionId);
        var independentlyLoaded=await writer.LoadHolonAsync<NativeRoleOwner>(root.Id,providerType:ProviderType.MongoDBOASIS);
        Assert.False(independentlyLoaded.IsError,independentlyLoaded.Message+independentlyLoaded.Exception);
        Assert.Equal("query member edited",Assert.Single(independentlyLoaded.Result.Components).Name);
        var rowsCollection=client.GetDatabase(databaseName).GetCollection<MongoDB.Bson.BsonDocument>("Holon");
        var component=Assert.Single(independentlyLoaded.Result.Components);
        var childFilter=new MongoDB.Bson.BsonDocument("HolonId",component.Id.ToString("D"));
        var childBefore=(await rowsCollection.Find(childFilter).SingleAsync()).ToJson();
        var observedManifest=independentlyLoaded.Result.ChildRoleManifestJson;
        Assert.NotNull(observedManifest);
        // Direct root-only input deliberately has no role observation or loaded members.
        var scalarOnly=new Holon {Id=root.Id,ParentHolonId=parent.Id,Name="scalar-only owner"};
        var condition=new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.MongoDBOASIS,
            new[]{new HolonWriteCondition(root.Id,CosmicConcurrency.GetProviderRevisionId(independentlyLoaded.Result,ProviderType.MongoDBOASIS))});
        var request=new ConditionalHolonWriteRequest(condition,ConditionalHolonOperation.Save,new[]{scalarOnly},saveChildren:false);
        var partial=synchronous ? other.ExecuteConditionalHolonWrite(request):await other.ExecuteConditionalHolonWriteAsync(request);
        Assert.False(partial.IsError,partial.Message+partial.Exception);
        var partialRoot=Assert.IsType<Holon>(Assert.Single(partial.Result));
        Assert.Empty(partialRoot.Children);Assert.Equal(observedManifest,partialRoot.ChildRoleManifestJson);
        var replay=synchronous ? provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.False(replay.IsError,replay.Message+replay.Exception);
        var replayRoot=Assert.IsType<Holon>(Assert.Single(replay.Result));
        Assert.Equal(partialRoot.VersionId,replayRoot.VersionId);Assert.Equal(observedManifest,replayRoot.ChildRoleManifestJson);
        Assert.Equal(childBefore,(await rowsCollection.Find(childFilter).SingleAsync()).ToJson());
        var afterPartial=await writer.LoadHolonAsync<NativeRoleOwner>(root.Id,providerType:ProviderType.MongoDBOASIS);
        Assert.False(afterPartial.IsError,afterPartial.Message+afterPartial.Exception);
        Assert.Equal(observedManifest,afterPartial.Result.ChildRoleManifestJson);
        Assert.Equal("query member edited",Assert.Single(afterPartial.Result.Components).Name);
        // Emulate an older stored record with no role observation, without changing its revision.
        var rootFilter=new BsonDocument("HolonId",root.Id.ToString("D"));
        var cleared=await rowsCollection.UpdateOneAsync(rootFilter,Builders<BsonDocument>.Update.Unset("ChildRoleManifestJson"));
        Assert.Equal(1,cleared.ModifiedCount);
        scalarOnly.ChildRoleManifestJson=observedManifest;
        var legacyCondition=new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.MongoDBOASIS,
            new[]{new HolonWriteCondition(root.Id,CosmicConcurrency.GetProviderRevisionId(partialRoot,ProviderType.MongoDBOASIS))});
        var legacyRequest=new ConditionalHolonWriteRequest(legacyCondition,ConditionalHolonOperation.Save,new[]{scalarOnly},saveChildren:false);
        var legacy=synchronous ? provider.ExecuteConditionalHolonWrite(legacyRequest):await provider.ExecuteConditionalHolonWriteAsync(legacyRequest);
        Assert.False(legacy.IsError,legacy.Message+legacy.Exception);
        Assert.Null(Assert.IsType<Holon>(Assert.Single(legacy.Result)).ChildRoleManifestJson);
        Assert.False((await rowsCollection.Find(rootFilter).SingleAsync()).Contains("ChildRoleManifestJson"));
        Assert.Equal(childBefore,(await rowsCollection.Find(childFilter).SingleAsync()).ToJson());
        complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();await other.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task ReconcileExactEmptyPreSubmissionTypedQueryFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_typed_query_fixture";
        var client=new MongoClient(uri);var database=client.GetDatabase(name);
        using var cursor=await database.ListCollectionNamesAsync();var collections=await cursor.ToListAsync();
        Assert.Contains("CosmicOwnedFixture",collections);
        // Only the exact fixture created by the captured pre-submission failure may be removed.
        // A nonempty collection preserves the database for a different reconciliation.
        foreach(var collection in collections)
        {
            var count=await database.GetCollection<MongoDB.Bson.BsonDocument>(collection)
                .WithReadConcern(ReadConcern.Majority).CountDocumentsAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty);
            output.WriteLine($"Exact fixture {name}/{collection}: majority-visible count={count}");
            Assert.Equal(0,count);
        }
        await client.DropDatabaseAsync(name);
        using var remaining=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await remaining.ToListAsync());
    }

    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task ReconcileExactCommittedTypedQueryFixture()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_typed_query_fixture";
        var client=new MongoClient(uri);var database=client.GetDatabase(name);
        using var cursor=await database.ListCollectionNamesAsync();var collections=await cursor.ToListAsync();
        Assert.Contains("CosmicOwnedFixture",collections);
        var receipt=Assert.Single(await database.GetCollection<MongoDB.Bson.BsonDocument>("CosmicWriteReceipt")
            .WithReadConcern(ReadConcern.Majority).Find(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty).ToListAsync());
        var roots=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString);
        var graph=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.HolonGraphWritePlan.Create(roots,true,true,0,0);
        Assert.Equal(3,graph.Nodes.Count);
        var expected=graph.Nodes.ToDictionary(node=>node.Snapshot.Id,node=>node.Snapshot);
        Assert.Equal(new[]{"query member","query parent","typed query owner"},expected.Values.Select(row=>row.Name).OrderBy(value=>value).ToArray());
        var rows=await database.GetCollection<MongoDB.Bson.BsonDocument>("Holon").WithReadConcern(ReadConcern.Majority)
            .Find(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty).ToListAsync();
        Assert.Equal(3,rows.Count);
        foreach(var row in rows)
        {
            var native=MongoDB.Bson.Serialization.BsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>(row);
            var value=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper.ConvertMongoEntityToOASISHolon(native,false);
            Assert.True(expected.TryGetValue(value.Id,out var acknowledged));
            Assert.Equal(acknowledged!.Name,value.Name);Assert.Equal(acknowledged.VersionId,value.VersionId);
            Assert.Equal(acknowledged.ParentHolonId,value.ParentHolonId);
            Assert.Equal(CosmicConcurrency.GetProviderRevisionId(acknowledged,ProviderType.MongoDBOASIS),CosmicConcurrency.GetProviderRevisionId(value,ProviderType.MongoDBOASIS));
            output.WriteLine($"Owned record {value.Id}: SHA256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(MongoDB.Bson.BsonExtensionMethods.ToBson(row)))}");
        }
        foreach(var collection in collections.Where(value=>value!="Holon" && value!="CosmicWriteReceipt"))
            Assert.Equal(0,await database.GetCollection<MongoDB.Bson.BsonDocument>(collection).WithReadConcern(ReadConcern.Majority)
                .CountDocumentsAsync(FilterDefinition<MongoDB.Bson.BsonDocument>.Empty));
        output.WriteLine("Owned receipt SHA256="+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(MongoDB.Bson.BsonExtensionMethods.ToBson(receipt))));
        await client.DropDatabaseAsync(name);
        using var remaining=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await remaining.ToListAsync());
    }

    private static HolonManager Manager(Provider provider)
    {
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}}};
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var providers=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        providers.RegisterProvider(provider);
        return new HolonManager(null,dna,providers);
    }
}
