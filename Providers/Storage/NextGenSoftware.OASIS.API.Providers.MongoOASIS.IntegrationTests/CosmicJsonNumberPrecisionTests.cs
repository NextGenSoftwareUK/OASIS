using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeJsonMetadataRetainsExactNumericValuesAndRejectsUnrepresentableWrites(bool synchronous)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_conditional_fixture";var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;
        const string exact="1.000000000000000000000000001";
        using var json=JsonDocument.Parse("{\"value\":"+exact+",\"small\":1e-28}");
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var root=new Holon{Id=Guid.NewGuid(),Name="owned exact JSON metadata",MetaData=new(){["tree"]=json.RootElement.Clone(),["raw"]=new JRaw(exact),["literal"]=exact}};
            var saved=synchronous?provider.SaveHolon(root,saveChildren:false):await provider.SaveHolonAsync(root,saveChildren:false);
            Assert.False(saved.IsError,saved.Message+saved.Exception);
            var rows=database.GetCollection<BsonDocument>("Holon");
            var native=await rows.Find(new BsonDocument("HolonId",root.Id.ToString("D"))).SingleAsync();
            Assert.Equal(BsonType.Decimal128,native["MetaData"]["tree"]["value"].BsonType);
            Assert.Equal(BsonType.Decimal128,native["MetaData"]["tree"]["small"].BsonType);
            Assert.Equal(BsonType.Decimal128,native["MetaData"]["raw"].BsonType);
            Assert.Equal(exact,native["MetaData"]["literal"].AsString);
            var loaded=await provider.LoadHolonAsync(root.Id,loadChildren:false);Assert.False(loaded.IsError,loaded.Message+loaded.Exception);
            using var serialized=JsonDocument.Parse(StoredRecordJson.Serialize(loaded.Result.MetaData));
            Assert.True(StoredRecordValueEquality.Equivalent(new JRaw(exact),new JRaw(serialized.RootElement.GetProperty("tree").GetProperty("value").GetRawText())));
            Assert.True(StoredRecordValueEquality.Equivalent(new JRaw(exact),new JRaw(serialized.RootElement.GetProperty("raw").GetRawText())));
            var snapshot=native.ToBson();
            using var tiny=JsonDocument.Parse("1e-1000");
            var rejected=new Holon{Id=Guid.NewGuid(),Name="owned rejected underflow",MetaData=new(){["number"]=tiny.RootElement.Clone()}};
            var failed=synchronous?provider.SaveHolon(rejected,saveChildren:false):await provider.SaveHolonAsync(rejected,saveChildren:false);
            Assert.True(failed.IsError);Assert.False(failed.IsSaved);
            Assert.Equal(0,await rows.CountDocumentsAsync(new BsonDocument("HolonId",rejected.Id.ToString("D"))));
            Assert.Equal(snapshot,(await rows.Find(new BsonDocument("HolonId",root.Id.ToString("D"))).SingleAsync()).ToBson());
            complete=true;
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(complete)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned cosmic_conditional_fixture numeric precision evidence for independent reconciliation.");
        }
    }
}
