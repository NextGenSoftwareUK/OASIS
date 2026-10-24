using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Xunit;
using Xunit.Abstractions;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class MongoExportNativeTests(ITestOutputHelper output)
{
    [Fact]
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
