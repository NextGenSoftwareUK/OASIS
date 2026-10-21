using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public class MongoLifecycleFixtureReconciliation
{
    [Fact,Trait("Category","OwnedFixtureReconciliation")]
    public async Task ArchiveExactReviewedTombstonesAndReceiptsBeforeRemoval()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        Assert.Equal("CAE708CA8C2EE5782FB9D216B250492EEAEA0CDA91E078AA69AC7AA8BEB659F6",Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root,"TestResults/CosmicMongoCurrent/mongo-lifecycle-original-single-contract.trx")))));
        var cases=new[]{("cosmic_lifecycle_summary_fixture","04b19402-d1f2-4dd6-850f-dabd8eba8f0b","7072df4d-4a66-4a9b-99c0-cf374c4b8883","2d8c1a12-e841-477d-83af-7a499b8ce167",false),
            ("cosmic_lifecycle_history_fixture","a2028778-cac9-4a2c-afd0-40feb2719c50","9c4f389f-14a3-4ced-aa95-2180cf921566","7c2e9145-5ad7-4c0a-a395-50aa873ffd18",true)};
        var client=new MongoClient(uri);var settings=new JsonWriterSettings{OutputMode=JsonOutputMode.CanonicalExtendedJson};
        foreach(var (name,id,actor,creation,history) in cases)
        {
            var db=client.GetDatabase(name).WithReadConcern(ReadConcern.Majority).WithReadPreference(ReadPreference.Primary);
            using var names=await db.ListCollectionNamesAsync();var observed=new Dictionary<string,BsonDocument[]>();
            foreach(var collection in await names.ToListAsync())
            {
                var docs=(await db.GetCollection<BsonDocument>(collection).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).OrderBy(doc=>doc["_id"].ToJson(),StringComparer.Ordinal).ToArray();
                if(docs.Length==0)continue;
                Assert.Contains(collection,new[]{"Holon","CosmicOwnedFixture","CosmicWriteReceipt","CosmicOperationFence"});observed.Add(collection,docs);
            }
            Assert.Equal(4,observed.Count);var marker=Assert.Single(observed["CosmicOwnedFixture"]);
            Assert.Equal("owned lifecycle v1",marker["_id"].AsString);Assert.Equal(id,marker["root"].AsString);Assert.Equal(actor,marker["actor"].AsString);Assert.Equal(creation,marker["creation"].AsString);Assert.Equal(history,marker["history"].AsBoolean);
            var native=BsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon>(Assert.Single(observed["Holon"]));
            Assert.Equal(Guid.Parse(id),native.HolonId);Assert.Equal("owned lifecycle root",native.Name);Assert.Equal(2,native.Version);Assert.False(native.IsActive);Assert.Equal(actor,native.DeletedByAvatarId);Assert.NotEqual(DateTime.MinValue,native.DeletedDate);
            Assert.Equal(2,observed["CosmicWriteReceipt"].Length);var acknowledgements=new Dictionary<ConditionalHolonOperation,NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>();
            foreach(var receipt in observed["CosmicWriteReceipt"])
            {
                var packet=JObject.Parse(receipt["ResultJson"].AsString);Assert.Equal(4,packet.Value<int>("format"));var delta=HolonCommittedDelta.ReadStored(packet.Value<string>("delta"));
                Assert.Equal(receipt["_id"].AsString,delta.OperationId.ToString());Assert.Equal(Guid.Parse(actor),delta.ActorId);Assert.Equal(ProviderType.MongoDBOASIS,delta.Authority);Assert.Equal(history,delta.ValuesRetained);
                var ack=Assert.Single(ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString));Assert.Equal(Guid.Parse(id),ack.Id);Assert.Empty(ack.Children);acknowledgements.Add(delta.Operation,ack);
                if(delta.Operation==ConditionalHolonOperation.Save)Assert.Equal(creation,receipt["_id"].AsString);
            }
            var prior=acknowledgements[ConditionalHolonOperation.Save];var soft=acknowledgements[ConditionalHolonOperation.SoftDelete];Assert.Equal(1,prior.Version);Assert.Equal(prior.VersionId,soft.PreviousVersionId);
            Assert.Equal(native.VersionId,soft.VersionId);Assert.Equal(native.DeletedDate,soft.DeletedDate);Assert.Equal(native.ProviderRevisionId,CosmicConcurrency.GetProviderRevisionId(soft,ProviderType.MongoDBOASIS));
            Assert.Equal(2,observed["CosmicOperationFence"].Length);Assert.Equal(observed["CosmicWriteReceipt"].Select(doc=>doc["_id"].AsString).OrderBy(x=>x),observed["CosmicOperationFence"].Select(doc=>doc["_id"].AsString).OrderBy(x=>x));
            foreach(var fence in observed["CosmicOperationFence"])Assert.Equal(1L,fence["Generation"].ToInt64());
            var archive=Path.Combine(root,"TestResults/SubscriptionUsageCurrent/"+name+"-reviewed.json");Assert.False(File.Exists(archive));
            using(var output=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {await System.Text.Json.JsonSerializer.SerializeAsync(output,observed.ToDictionary(pair=>pair.Key,pair=>pair.Value.Select(doc=>doc.ToJson(settings)).ToArray()));output.Flush(true);}
            foreach(var pair in observed)
            {
                var current=(await db.GetCollection<BsonDocument>(pair.Key).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync()).OrderBy(doc=>doc["_id"].ToJson(),StringComparer.Ordinal).ToArray();
                Assert.Equal(pair.Value.Select(doc=>doc.ToJson(settings)),current.Select(doc=>doc.ToJson(settings)));
            }
            await client.DropDatabaseAsync(name);
        }
    }
}
