using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;
using NativeHelper=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class CosmicCqlFixtureReconciliationTests
{
    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task ReviewedCanonicalMembershipPreparationFixtureMatchesInspectionBeforeCleanup()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var path=Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-membership-preparation-inspection.json");
        var bytes=await File.ReadAllBytesAsync(path);
        Assert.Equal("776F992DE3B7FFFD1B502BA9360989FC08FD5FD57A0CAE99A7AD3322394F9A88",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
        using var inspected=JsonDocument.Parse(bytes);
        var client=new MongoClient(uri);var db=client.GetDatabase("cosmic_cql_fixture");
        var names=(await(await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(name=>name).ToArray();
        Assert.Equal(inspected.RootElement.EnumerateArray().Select(entry=>entry.GetProperty("collection").GetString()).ToArray(),names);
        foreach(var entry in inspected.RootElement.EnumerateArray())
        {
            var name=entry.GetProperty("collection").GetString()!;
            var rows=await db.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            Assert.Equal(entry.GetProperty("count").GetInt64(),rows.Count);
            foreach(var raw in entry.GetProperty("rows").EnumerateArray())
            {
                var expected=BsonDocument.Parse(raw.GetString());
                Assert.Equal(expected,Assert.Single(rows.Where(row=>row["_id"]==expected["_id"])));
            }
        }
        // The reviewed observation contains exactly the four owned fixture nodes,
        // empty auxiliary collections, and no conditional write or outbox operation.
        await File.WriteAllTextAsync(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-membership-preparation-cleanup.json"),
            JsonSerializer.Serialize(new{inspectionSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)),records=4,operations=0}));
        await client.DropDatabaseAsync("cosmic_cql_fixture");
    }

    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task InspectCanonicalMembershipPreparationFixtureWithoutCleanup()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var db=new MongoClient(uri).GetDatabase("cosmic_cql_fixture");
        var inventory=new List<object>();
        foreach(var name in (await(await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(value=>value))
        {
            var rows=await db.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Limit(20).ToListAsync();
            var count=await db.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            Assert.True(count<=20,"Preserve an unexpectedly large fixture.");
            inventory.Add(new{collection=name,count,rows=rows.Select(row=>row.ToJson()).ToArray()});
        }
        await File.WriteAllTextAsync(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-membership-preparation-inspection.json"),
            JsonSerializer.Serialize(inventory));
    }

    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task IndependentlyReconcileOwnedCqlFixtureBeforeCleanup()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";var client=new MongoClient(uri);
        using var databases=await client.ListDatabaseNamesAsync();Assert.Contains(name,await databases.ToListAsync());
        var database=client.GetDatabase(name).WithReadConcern(ReadConcern.Majority).WithReadPreference(ReadPreference.Primary);
        using var names=await database.ListCollectionNamesAsync();var collections=await names.ToListAsync();
        Assert.Contains("CosmicOwnedFixture",collections);
        var rowCount=await database.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        if(rowCount!=0)
        {
            if(rowCount==14)
            {
                await InspectResourceFixture(client,database,collections,workspace);
                return;
            }
            var comparisonFixture=await database.GetCollection<BsonDocument>("Holon").Find(new BsonDocument("HolonId","14cfbaa0-335a-49f4-aa7f-f7fb5278a080")).SingleOrDefaultAsync();
            if(comparisonFixture!=null && comparisonFixture["MetaData"] is BsonDocument comparisonMetadata && comparisonMetadata.Contains("guid.reference"))
            {
                await InspectFieldComparisonFixture(client,database,collections,workspace);
                return;
            }
            await InspectTypedCompilerFixture(client,database,collections,workspace);
            return;
        }
        // ActivateMongoAsync creates public-identity and primary-outbox indexes before any save.
        var allowed=new[]{"CosmicOwnedFixture","Holon","Avatar","AvatarDetail","SearchData","CosmicHolonOutbox"};var observed=new Dictionary<string,long>();
        foreach(var collection in collections.OrderBy(value=>value,StringComparer.Ordinal))
        {
            Assert.Contains(collection,allowed);
            var count=await database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            observed.Add(collection,count);Assert.Equal(0,count);
        }
        var evidence=Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-cql-oversized-seed-reconciliation.json");
        await File.WriteAllTextAsync(evidence,JsonSerializer.Serialize(new{Database=name,ReadConcern="majority",Collections=observed,
            Reason="Original native code 388 seed transaction rolled back; no rows, receipts or preparations committed."}));
        // Only the exact empty owned fixture is removed, after every collection was checked.
        await client.DropDatabaseAsync(name);
    }

    private static async Task InspectResourceFixture(MongoClient client,IMongoDatabase database,List<string> collections,string workspace)
    {
        var report=Path.Combine(workspace,"TestResults/CosmicMongoCurrent/cql-timeout-original-failure.trx");
        Assert.Equal("A513E8EB09F79280FCE35E2477C3041A66DDEA67E6F1A5D4BE133266D5CC4A3B",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(report))));
        var images=new SortedDictionary<string,BsonDocument[]>(StringComparer.Ordinal);
        foreach(var name in collections)
        {
            Assert.Contains(name,new[]{"CosmicOwnedFixture","Holon","Avatar","AvatarDetail","SearchData","CosmicHolonOutbox"});
            images.Add(name,(await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id",1)).ToListAsync()).ToArray());
            if(name!="Holon")Assert.Empty(images[name]);
        }
        var rows=images["Holon"];Assert.Equal(14,rows.Length);
        var expected=Enumerable.Range(0,9).Select(i=>"owned CQL bounded payload "+i)
            .Concat(Enumerable.Range(9,5).Select(i=>"owned CQL bounded sort "+i)).OrderBy(name=>name,StringComparer.Ordinal);
        Assert.Equal(expected,rows.Select(row=>row["Name"].AsString).OrderBy(name=>name,StringComparer.Ordinal));
        Assert.Equal(14,rows.Select(row=>Guid.Parse(row["HolonId"].AsString)).Distinct().Count());
        foreach(var row in rows)
        {
            Assert.Equal(Guid.Empty.ToString("D"),row["ParentHolonId"].AsString);
            Assert.True(row["Children"].IsBsonNull);Assert.Empty(row["ChildrenIds"].AsBsonArray);
            Assert.True(row["IsActive"].AsBoolean);Assert.False(row["IsPublic"].AsBoolean);
            Assert.Null(row["Description"].IsBsonNull?null:row["Description"].AsString);
            var entry=Assert.Single(row["MetaData"].AsBsonDocument);
            Assert.Equal("payload",entry.Name);Assert.Equal(8*1024*1024,entry.Value.AsString.Length);
            Assert.Equal(-1,entry.Value.AsString.AsSpan().IndexOfAnyExcept('x'));
            Assert.Equal(Guid.Empty.ToString("D"),row["CreatedByAvatarId"].AsString);
            Assert.Equal(Guid.Empty.ToString("D"),row["VersionId"].AsString);
            Assert.True(Guid.TryParse(row["ProviderRevisionId"].AsString,out var revision) && revision!=Guid.Empty);
            Assert.Equal(Guid.Empty.ToString("D"),row["PreviousVersionId"].AsString);
            Assert.Equal(DateTime.MinValue.Ticks,row["DeletedDate"].ToUniversalTime().Ticks);
            Assert.Equal(Guid.Empty.ToString("D"),row["DeletedByAvatarId"].AsString);
        }
        string Hash(BsonDocument row)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(row.ToBson()));
        var inventory=images.ToDictionary(pair=>pair.Key,pair=>pair.Value.Select(row=>new{Id=row["_id"].ToString(),Sha256=Hash(row)}).ToArray());
        await File.WriteAllTextAsync(Path.Combine(workspace,"TestResults/CosmicMongoCurrent/cql-timeout-fixture-reviewed.json"),
            JsonSerializer.Serialize(new{Database="cosmic_cql_fixture",Rows=14,Payload="Exactly8MiBASCIIxInEachRow",Collections=inventory}));
        using var names=await database.ListCollectionNamesAsync();
        Assert.Equal(images.Keys,(await names.ToListAsync()).OrderBy(name=>name,StringComparer.Ordinal));
        foreach(var pair in images)
        {
            var current=await database.GetCollection<BsonDocument>(pair.Key).Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id",1)).ToListAsync();
            Assert.Equal(pair.Value.Select(Hash),current.Select(Hash));
        }
        Assert.Equal("cosmic_cql_fixture",database.DatabaseNamespace.DatabaseName);
        await client.DropDatabaseAsync("cosmic_cql_fixture");
    }

    private static async Task InspectFieldComparisonFixture(MongoClient client,IMongoDatabase database,List<string> collections,string workspace)
    {
        Assert.Equal("24C02F7E6DF2F3F49A9B79C279696AE31893B6B14F167B50E2ECA7A956C15766",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(
                Path.Combine(workspace,"TestResults/CosmicMongoCurrent/cql-field-original-failure.trx")))));
        var snapshots=new SortedDictionary<string,BsonArray>(StringComparer.Ordinal);
        foreach(var name in collections)
            snapshots.Add(name,new BsonArray(await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id",1)).ToListAsync()));
        Assert.Equal(5,snapshots["Holon"].Count);
        foreach(var pair in snapshots.Where(pair=>pair.Key!="Holon")) Assert.Empty(pair.Value);
        var rows=snapshots["Holon"].Select(value=>value.AsBsonDocument).ToArray();
        var root=Assert.Single(rows.Where(row=>row["HolonId"]=="14cfbaa0-335a-49f4-aa7f-f7fb5278a080"));
        Assert.Equal(2,root["Version"].AsInt32);
        Assert.Equal("aabbccdd-1234-5678-9abc-def012345678",root["MetaData"]["guid.probe"].AsString);
        Assert.Equal(new BsonBinaryData(Guid.Parse("aabbccdd-1234-5678-9abc-def012345678"),GuidRepresentation.Standard),root["MetaData"]["guid.reference"]);
        BsonDocument Field(string name)=>new("$getField",new BsonDocument{{"field",name},{"input","$MetaData"}});
        var projection=new BsonDocument("$project",new BsonDocument {
            {"_id",0},{"text",new BsonDocument("$replaceAll",new BsonDocument{{"input",Field("guid.probe")},{"find","-"},{"replacement",""}})},
            {"hex",new BsonDocument("$convert",new BsonDocument{{"input",Field("guid.reference")},{"to","string"},{"format","hex"}})} });
        var diagnostic=Assert.Single(await database.GetCollection<BsonDocument>("Holon").Aggregate<BsonDocument>(new[]{
            new BsonDocument("$match",new BsonDocument("HolonId",root["HolonId"])),projection}).ToListAsync());
        var archive=new BsonDocument{{"collections",new BsonDocument(snapshots.Select(pair=>new BsonElement(pair.Key,pair.Value)))},{"diagnostic",diagnostic}};
        var archivePath=Path.Combine(workspace,"TestResults/CosmicMongoCurrent/cql-field-observed-fixture.json");
        var reviewed=await File.ReadAllBytesAsync(archivePath);
        Assert.Equal("03E832A270EA915589676E21B84B38A7DECABF40D9C7B6F462CB3E92013CED01",
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(reviewed)));
        var expected=BsonDocument.Parse(System.Text.Encoding.UTF8.GetString(reviewed));
        Assert.Equal(expected.ToBson(),archive.ToBson());
        Assert.Equal(diagnostic["text"].AsString,diagnostic["hex"].AsString,ignoreCase:true);
        Assert.NotEqual(diagnostic["text"].AsString,diagnostic["hex"].AsString);
        using var currentNames=await database.ListCollectionNamesAsync();
        Assert.Equal(snapshots.Keys,(await currentNames.ToListAsync()).OrderBy(name=>name,StringComparer.Ordinal));
        foreach(var pair in snapshots)
        {
            var current=new BsonArray(await database.GetCollection<BsonDocument>(pair.Key).Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id",1)).ToListAsync());
            Assert.Equal(new BsonDocument("rows",pair.Value).ToBson(),new BsonDocument("rows",current).ToBson());
        }
        Assert.Equal("cosmic_cql_fixture",database.DatabaseNamespace.DatabaseName);
        await client.DropDatabaseAsync("cosmic_cql_fixture");
        await File.WriteAllTextAsync(Path.Combine(workspace,"TestResults/CosmicMongoCurrent/cql-field-fixture-reviewed.json"),
            JsonSerializer.Serialize(new{Database="cosmic_cql_fixture",ArchiveSha256="03E832A270EA915589676E21B84B38A7DECABF40D9C7B6F462CB3E92013CED01",Rows=5,Removed=true}));
    }

    private static async Task InspectTypedCompilerFixture(MongoClient client,IMongoDatabase database,List<string> collections,string workspace)
    {
        var rows=await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Equal(5,rows.Count);
        var native=rows.Select(row=>BsonSerializer.Deserialize<NativeHolon>(row.ToBson())).ToArray();
        await File.WriteAllTextAsync(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-cql-typed-observed.json"),
            StoredRecordJson.Serialize(native.Select(row=>new{row.HolonId,row.Name,row.Description,row.Version,row.VersionId,
                row.PreviousVersionId,row.ParentHolonId,row.ChildrenIds,row.MetaData}).ToArray()));
        Assert.Equal(5,native.Select(row=>row.HolonId).Distinct().Count());Assert.All(native,row=>Assert.NotEqual(Guid.Empty,row.HolonId));
        var a=Assert.Single(native.Where(row=>row.Name=="$where.coffee😀\0end"));
        var b=Assert.Single(native.Where(row=>row.Name=="tea"));var c=Assert.Single(native.Where(row=>row.Name=="coffee"));
        var d=Assert.Single(native.Where(row=>row.Name==null));var child=Assert.Single(native.Where(row=>row.Name=="owned unselected child"));
        Assert.Contains(a.Version,new[]{2,3});var edited=a.Version==3;
        Assert.Equal(3,b.Version);Assert.Equal(4,c.Version);Assert.Equal(1,d.Version);Assert.Equal(1,child.Version);
        Assert.Equal(edited?"owned CQL conditional edit":"espresso%_",a.Description);Assert.Equal(a.HolonId,child.ParentHolonId);
        // ChildrenIds is an independently declared DTO value; actual native graph reads use ParentHolonId.
        Assert.Empty(a.ChildrenIds);Assert.Equal(child.HolonId,Assert.Single(native.Where(row=>row.ParentHolonId==a.HolonId)).HolonId);
        foreach(var root in new[]{b,c,d}){Assert.Empty(root.ChildrenIds);Assert.Equal(Guid.Empty,root.ParentHolonId);Assert.Null(root.Description);}
        Assert.Empty(child.ChildrenIds);Assert.Empty(child.MetaData);Assert.Null(child.Description);
        void Metadata(NativeHolon row,Dictionary<string,object> expected)=>Assert.True(StoredRecordValueEquality.Equivalent(
            StoredRecordJson.CaptureRuntimeToken(expected),StoredRecordJson.CaptureRuntimeToken(row.MetaData)),"Full owned metadata differs for "+row.Name
                +" expected="+StoredRecordJson.Serialize(expected)+" actual="+StoredRecordJson.Serialize(row.MetaData));
        var expectedA=new Dictionary<string,object>{["stats"]=new Dictionary<string,object>{{"score",7}},["a.b"]="literal",["$tag"]="coffee",["nullable"]=null,
            ["number"]=9007199254740993L,["flag"]=true,["optional"]=7,["Rating"]=7,["Name"]="forged metadata name"};
        if(edited){expectedA["CreatedByAvatarId"]=Guid.Empty.ToString("D");expectedA["Active"]="1";}
        Metadata(a,expectedA);
        Metadata(b,new(){["stats"]=new Dictionary<string,object>{{"score","7"}},["a"]=new Dictionary<string,object>{{"b","nested"}},["flag"]=false,["Rating"]=8});
        Metadata(c,new(){["stats"]=new Dictionary<string,object>{{"score",8}},["nullable"]="value",["number"]=9L,["optional"]="7",["Rating"]=9});
        Metadata(d,new(){["stats"]=new object[]{new Dictionary<string,object>{{"score",7}}},["nullable"]=null,["optional"]=null});
        if(!edited)
        {
            var rawAllowed=new[]{"CosmicOwnedFixture","Holon","Avatar","AvatarDetail","SearchData","CosmicHolonOutbox"};
            foreach(var collection in collections)
            {
                Assert.Contains(collection,rawAllowed);
                if(collection!="Holon")Assert.Equal(0,await database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            }
            await File.WriteAllTextAsync(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-cql-prewrite-semantics-reconciliation.json"),
                JsonSerializer.Serialize(new{Database=database.DatabaseNamespace.DatabaseName,ReadConcern="majority",Stage="Exact raw version-2 graph before any conditional mutation",
                    Rows=rows.Select(row=>new{Id=row["HolonId"].AsString,Sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(row.ToBson()))}).ToArray()}));
            await client.DropDatabaseAsync("cosmic_cql_fixture");return;
        }
        var receipt=Assert.Single(await database.GetCollection<BsonDocument>("CosmicWriteReceipt").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
        var acknowledgement=Assert.Single(ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString));
        using var envelope=JsonDocument.Parse(receipt["ResultJson"].AsString);
        Assert.Equal(receipt["_id"].AsString,envelope.RootElement.GetProperty("operation").GetString());
        Assert.Equal(receipt["Fingerprint"].AsString,envelope.RootElement.GetProperty("intent").GetString());
        var persisted=NativeHelper.ConvertMongoEntityToOASISHolon(a,mapChildren:false);
        Assert.Equal(a.HolonId,acknowledgement.Id);Assert.Equal(a.Version,acknowledgement.Version);Assert.Equal(a.VersionId,acknowledgement.VersionId);
        Assert.Equal(persisted.PreviousVersionId,acknowledgement.PreviousVersionId);
        Assert.Equal(persisted.CreatedDate,acknowledgement.CreatedDate);Assert.Equal(persisted.ModifiedDate,acknowledgement.ModifiedDate);
        Assert.Equal(persisted.CreatedByAvatarId,acknowledgement.CreatedByAvatarId);Assert.Equal(persisted.ModifiedByAvatarId,acknowledgement.ModifiedByAvatarId);
        Assert.Equal(CosmicConcurrency.GetProviderRevisionId(persisted,ProviderType.MongoDBOASIS),CosmicConcurrency.GetProviderRevisionId(acknowledgement,ProviderType.MongoDBOASIS));
        Assert.True(StoredRecordValueEquality.Equivalent(StoredRecordJson.CaptureRuntimeToken(a.MetaData),StoredRecordJson.CaptureRuntimeToken(acknowledgement.MetaData)));
        var fence=Assert.Single(await database.GetCollection<BsonDocument>("CosmicOperationFence").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync());
        Assert.Equal(receipt["_id"],fence["_id"]);Assert.Equal(1,fence["Generation"].ToInt64());Assert.Equal(BsonType.DateTime,fence["CreatedAtUtc"].BsonType);
        var allowed=new[]{"CosmicOwnedFixture","Holon","Avatar","AvatarDetail","SearchData","CosmicHolonOutbox","CosmicWriteReceipt","CosmicOperationFence"};
        foreach(var collection in collections)
        {
            Assert.Contains(collection,allowed);
            if(collection is "Holon" or "CosmicWriteReceipt" or "CosmicOperationFence")continue;
            Assert.Equal(0,await database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        var evidence=Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-cql-typed-compiler-reconciliation.json");
        await File.WriteAllTextAsync(evidence,JsonSerializer.Serialize(new{Database=database.DatabaseNamespace.DatabaseName,ReadConcern="majority",
            ReceiptOperation=receipt["_id"].AsString,Root=a.HolonId,Child=child.HolonId,Rows=rows.Select(row=>new{
                Id=row["HolonId"].AsString,Sha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(row.ToBson()))}).ToArray(),
            Reason="Exact synthetic CQL graph and committed version-3 root receipt verified before cleanup; no mutation replay."}));
        await client.DropDatabaseAsync("cosmic_cql_fixture");
    }
}
