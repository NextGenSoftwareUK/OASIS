using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using Xunit;
using Xunit.Abstractions;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class MongoExportNativeTests(ITestOutputHelper output)
{
    [Fact]
    public async Task HistoricalExportRetainsPreviouslySavedLogicalVersion()
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
            var original=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),Version=1,VersionId=Guid.NewGuid(),Name="historical original"};
            var first=await provider.SaveHolonAsync(original,saveChildren:false);
            Assert.False(first.IsError,first.Message+first.Exception);
            var current=first.Result;
            current.PreviousVersionId=current.VersionId;
            current.VersionId=Guid.NewGuid();current.Version=2;current.Name="current replacement";
            var second=await provider.SaveHolonAsync(current,saveChildren:false);
            Assert.False(second.IsError,second.Message+second.Exception);
            var latest=await provider.ExportAllAsync();
            Assert.False(latest.IsError,latest.Message+latest.Exception);
            Assert.Equal("current replacement",Assert.Single(latest.Result).Name);
            var historical=await provider.ExportAllAsync(1);
            Assert.False(historical.IsError,historical.Message+historical.Exception);
            var retained=Assert.Single(historical.Result);
            Assert.Equal("historical original",retained.Name);
            Assert.Equal(1,retained.Version);
            var all=await provider.ExportAllAsync(-1);
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
