using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeVirtualUtcDatePartsShareFullHolonRules(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeSourceJourney:VerifyVirtualUtcDatePartsJourney,
            nativeJourneySuffix:"-utc-virtual-date-parts-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyVirtualUtcDatePartsJourney(
        IOASISStorageProvider provider,string work,string database,MongoClient? mongo,
        Dictionary<string,long> timings,List<Guid> created)
    {
        var reader = (INativeDataProjectionQueryProvider)provider;
        var mp = provider as MongoProvider;
        var sp = provider as SqliteProvider;
        var np = provider as Neo4jOASIS;
        var priorMongo = mp?.NativeImportDatabaseName;
        var priorSqlite = sp?.NativeImportConnectionString;
        var priorLabels = np?.NativeImportLabels;
        var nativeDb = database + "_utc";
        Assert.IsTrue(nativeDb.Length <= 63, "Owned Mongo database names must fit the native contract.");
        var sqlitePath = Path.GetFullPath(Path.Combine(work, "utc-native.sqlite"));
        var label = "DatePartsNative_" + Guid.NewGuid().ToString("N");
        var sourceName = np == null ? "items" : label;
        var ownedMongo = false; var ownedSqlite = false; var ownedNeo = false;
        using var graph = np == null ? null : GraphDatabase.Driver(
            "bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = graph?.AsyncSession();
        var beforeDate=new DateTime(2024,12,31,23,59,59,DateTimeKind.Utc).AddTicks(9999999);
        var afterDate=new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc);
        var rows=new[]{
            new Dictionary<string,object?>{["key"]="before",["observed"]=beforeDate.ToString("O")},
            new Dictionary<string,object?>{["key"]="after",["observed"]=afterDate.ToString("O")},
            new Dictionary<string,object?>{["key"]="missing",["observed"]=null},
            new Dictionary<string,object?>{["key"]="bad",["observed"]="not-a-date"}
        };
        Func<Task<byte[][]>> fingerprint;
        try
        {
            if (mp != null)
            {
                Assert.IsNotNull(mongo);
                using (var names = await mongo.ListDatabaseNamesAsync())
                    Assert.IsFalse((await names.ToListAsync()).Contains(nativeDb));
                ownedMongo = true;
                mp.NativeImportDatabaseName = nativeDb;
                var collection = mongo.GetDatabase(nativeDb).GetCollection<BsonDocument>("items");
                await collection.InsertManyAsync(rows.Select(row => {
                    var doc = new BsonDocument { { "_id", (string)row["key"]! }, { "key", (string)row["key"]! } };
                    foreach (var field in new[] { "observed" })
                        doc.Add(field, row[field] == null ? BsonNull.Value : BsonValue.Create(row[field]));
                    return doc;
                }));
                fingerprint = async () => (await collection.Find(FilterDefinition<BsonDocument>.Empty)
                    .Sort(new BsonDocument("_id", 1)).ToListAsync()).Select(row => row.ToBson()).ToArray();
            }
            else if (sp != null)
            {
                Assert.AreEqual(Path.GetFullPath(work), Path.GetDirectoryName(sqlitePath));
                Assert.IsFalse(File.Exists(sqlitePath));
                ownedSqlite = true;
                var connection = "Data Source=" + sqlitePath + ";Pooling=False";
                await using (var native = new SqliteConnection(connection))
                {
                    await native.OpenAsync();
                    await using var create = native.CreateCommand();
                    create.CommandText = "CREATE TABLE items(id TEXT PRIMARY KEY,observed)";
                    await create.ExecuteNonQueryAsync();
                    foreach (var row in rows)
                    {
                        await using var insert = native.CreateCommand();
                        insert.CommandText = "INSERT INTO items(id,observed) VALUES($key,$observed)";
                        insert.Parameters.AddWithValue("$key", row["key"]!);
                        foreach (var field in new[] { "observed" })
                            insert.Parameters.AddWithValue("$" + field, row[field] ?? DBNull.Value);
                        await insert.ExecuteNonQueryAsync();
                    }
                }
                sp.NativeImportConnectionString = connection;
                fingerprint = async () => {
                    await using var native = new SqliteConnection(connection);
                    await native.OpenAsync();
                    await using var command = native.CreateCommand();
                    command.CommandText = "SELECT id,typeof(observed),observed FROM items ORDER BY id";
                    await using var data = await command.ExecuteReaderAsync();
                    var result = new List<byte[]>();
                    while (await data.ReadAsync())
                        result.Add(JsonSerializer.SerializeToUtf8Bytes(new {
                            key = data.GetString(0), kind = data.GetString(1),
                            observed = data.IsDBNull(2) ? null : data.GetValue(2)
                        }));
                    return result.ToArray();
                };
            }
            else
            {
                Assert.IsNotNull(np); Assert.IsNotNull(session);
                var absent = await (await session.RunAsync($"MATCH (n:{label}) RETURN count(n) AS total")).SingleAsync();
                Assert.AreEqual(0L, absent["total"].As<long>());
                ownedNeo = true;
                await (await session.RunAsync($"UNWIND $rows AS row CREATE (n:{label}) SET n = row", new { rows })).ConsumeAsync();
                np.NativeImportLabels = label;
                fingerprint = async () => (await (await session.RunAsync(
                    $"MATCH (n:{label}) RETURN n.key AS key,n.observed AS observed ORDER BY n.key")).ToListAsync())
                    .Select(row => JsonSerializer.SerializeToUtf8Bytes(new {
                        key = row["key"].As<string>(), observed = row["observed"]
                    })).ToArray();
            }
            var before=await fingerprint();Assert.AreEqual(4,before.Length);
            async Task<T> Timed<T>(string name,Func<Task<T>> action)
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();
                try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}
            }
            var actor=Guid.NewGuid();
            var mapping=new VirtualHolonMapping(Guid.NewGuid(),1,actor,reader.NativeDataProviderType,
                sourceName,VirtualHolonMapping.SandboxConnectionName,reader.DescribeVirtualQueryConnection(null),
                new Dictionary<string,string>{["observedAlias"]="observed",["keyAlias"]=sp!=null?"id":"key"});
            var functions=new[]{CqlV1Function.UtcYear,CqlV1Function.UtcMonth,CqlV1Function.UtcDay,
                CqlV1Function.UtcDayOfYear,CqlV1Function.UtcHour,CqlV1Function.UtcMinute,CqlV1Function.UtcSecond};
            var aliases=new[]{"Year","Month","Day","DayOfYear","Hour","Minute","Second"};
            var columns=new List<CqlProjectionV1Column>{new(){Field="Id",Alias="Id"}};
            for(var index=0;index<functions.Length;index++)
                columns.Add(new(){MetadataPath=new[]{"observedAlias"},ValueType="DateTime",
                    Alias=aliases[index],Nullable=true,Function=functions[index]});
            var request=new CqlProjectionV1Request{VirtualMappingId=mapping.Id,OrderBy="Id",Take=100,
                Filters=new[]{new CqlV1Filter{MetadataPath=new[]{"keyAlias"},Scalar=CqlV1Scalar.String,
                    Operator=CqlV1Operator.NotEqual,Value=JsonSerializer.SerializeToElement("bad")}},
                Columns=columns.ToArray()};
            var compiled=CqlV1Compiler.CompileVirtualProjection(request);
            var filters=new[]{new NativeRecordFilter("keyAlias",CosmicQueryComparisonOperator.NotEqual,"bad")};
            var projected=await Timed("virtual-components",()=>mapping.QueryProjectionAsync(
                actor,reader,null,filters,compiled.Columns,100));
            Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);
            Assert.AreEqual(3,projected.Result.Count);
            var values=projected.Result.Select(row=>compiled.Select(row)).ToArray();
            Assert.AreEqual(1,values.Count(row=>row[1] is null));
            foreach(var stamp in new[]{beforeDate,afterDate})
            {
                var value=values.Single(row=>row[1] is int year && year==stamp.Year);
                var expected=new[]{stamp.Year,stamp.Month,stamp.Day,stamp.DayOfYear,stamp.Hour,stamp.Minute,stamp.Second};
                for(var index=0;index<expected.Length;index++)Assert.AreEqual(expected[index],value[index+1]);
            }
            foreach(var row in projected.Result)
                Assert.IsNull((await provider.LoadHolonAsync(row.Id)).Result,"Projection must not import native records.");
            var original=await Timed("snapshot-read",()=>mapping.QueryAsync(actor,reader,null,
                new[]{new NativeRecordFilter("keyAlias",CosmicQueryComparisonOperator.Equal,"before")},
                new[]{"keyAlias","observedAlias"},1));
            Assert.IsFalse(original.IsError,original.Message+original.Exception);
            var snapshot=original.Result.Single().PrepareSnapshot(actor);
            snapshot.MetaData["observedAlias"]=new DateTime(2001,1,1,0,0,0,DateTimeKind.Utc);
            snapshot.IsNewHolon=true;
            var important=new Holon{Id=Guid.NewGuid(),Name="owned important date",CreatedByAvatarId=actor,
                IsPublic=false,IsActive=true,IsNewHolon=true,MetaData=new(){
                    ["keyAlias"]="owned",["observedAlias"]=new DateTime(2030,12,31,23,59,59,DateTimeKind.Utc).AddTicks(9999999)}};
            var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
                HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
            var registry=new ProviderManager(null,dna){
                IsAutoLoadBalanceEnabled=false,IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false};
            var manager=new HolonManager(provider,dna,registry);
            using var ownedSession=CosmicSession.Create(manager,(ICosmicQueryStorageProvider)provider,
                CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
            foreach(var model in new[]{snapshot,important})
            {created.Add(model.Id);Assert.IsFalse(ownedSession.Add(model).IsError);}
            var saved=await Timed("save-full",()=>ownedSession.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
            var adapter=(ICosmicQueryProjectionStorageProvider)provider;
            // Use the production mixed-route owner: exclude this mapping's
            // stale cached values before evaluating the stored projection.
            var query=CqlV1Compiler.CompileProjection(request,actor,adapter.QueryProviderType,
                CosmicQueryReadAccess.OwnerOrPublic(actor).Result,CqlVirtualProjection.MaximumRows,mapping.Id);
            var planned=CosmicProjectionPlanner.Plan(query,adapter.QueryProviderType,new[]{
                new CosmicProjectionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,adapter.ProjectionCapabilities)});
            Assert.IsFalse(planned.IsError,planned.Message);
            var full=await Timed("full-components",()=>manager.ExecuteProjectionQueryAsync(
                planned.Result,new[]{(ICosmicQueryStorageProvider)adapter}));
            Assert.IsFalse(full.IsError,full.Message+full.Exception);
            var materialized=full.Result.Providers.Single().Rows;
            Assert.AreEqual(1,materialized.Count);
            Assert.IsFalse(materialized.Any(row=>(Guid)row.Values[0]==snapshot.Id));
            var snapshotQuery=CqlV1Compiler.CompileSnapshotIdentityProjection(actor,adapter.QueryProviderType,
                query.Source.ReadAccess,mapping.Id,projected.Result.Select(row=>row.Id).ToArray());
            var snapshotPlan=CosmicProjectionPlanner.Plan(snapshotQuery,adapter.QueryProviderType,new[]{
                new CosmicProjectionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,adapter.ProjectionCapabilities)});
            Assert.IsFalse(snapshotPlan.IsError,snapshotPlan.Message);
            var snapshotProof=await Timed("snapshot-identities",()=>manager.ExecuteProjectionQueryAsync(
                snapshotPlan.Result,new[]{(ICosmicQueryStorageProvider)adapter}));
            Assert.IsFalse(snapshotProof.IsError,snapshotProof.Message+snapshotProof.Exception);
            var snapshotIds=snapshotProof.Result.Providers.Single().Rows.Select(row=>(Guid)row.Values[0]).ToHashSet();
            Assert.AreEqual(1,snapshotIds.Count);Assert.IsTrue(snapshotIds.Contains(snapshot.Id));
            var mixed=await Timed("mixed-components",()=>Task.FromResult(CosmicMixedProjectionResults.Merge(
                query,actor,mapping.Id,materialized,projected.Result,
                new[]{new CosmicMixedProjectionOrdering("Year",true),new CosmicMixedProjectionOrdering("Id")},0,10)));
            Assert.IsFalse(mixed.IsError,mixed.Message+mixed.Exception);Assert.AreEqual(4,mixed.Result.Count);
            var cached=mixed.Result.Single(row=>row.Id==snapshot.Id);
            Assert.IsTrue(cached.AlsoMaterialized || snapshotIds.Contains(cached.Id));
            Assert.IsNotNull(cached.Virtual);
            Assert.AreEqual(2024,cached.Record.Values[1]);
            Assert.AreEqual(important.Id,mixed.Result[0].Id);Assert.IsNull(mixed.Result[0].Virtual);
            Assert.AreEqual(2030,mixed.Result[0].Record.Values[1]);
            foreach(var row in projected.Result.Where(row=>row.Id!=snapshot.Id))
                Assert.IsNull((await provider.LoadHolonAsync(row.Id)).Result,"Virtual-only rows must remain unimported.");
            var invalid=await Timed("invalid-native",()=>mapping.QueryProjectionAsync(actor,reader,null,
                new[]{new NativeRecordFilter("keyAlias",CosmicQueryComparisonOperator.Equal,"bad")},compiled.Columns,100));
            Assert.IsTrue(invalid.IsError);Assert.IsNull(invalid.Result);
            var denied=await Timed("denied-native",()=>mapping.QueryProjectionAsync(Guid.NewGuid(),reader,null,
                filters,compiled.Columns,100));
            Assert.AreEqual("VIRTUAL_HOLON_FORBIDDEN",denied.ErrorCode);
            var after=await fingerprint();Assert.AreEqual(before.Length,after.Length);
            for(var index=0;index<before.Length;index++)CollectionAssert.AreEqual(before[index],after[index]);
            return new NativeFixtureJourneyProof(5,2,new Dictionary<string,bool>{
                ["nativeDatePartsVerified"]=true,["mixedFullAndVirtualVerified"]=true,
                ["staleSnapshotReconciled"]=true,["missingPreserved"]=true,["invalidDomainRefused"]=true,
                ["foreignActorRefused"]=true,["rawSourcesPreserved"]=true,["virtualOnlyNotImported"]=true});
        }
        finally
        {
            if (mp != null) mp.NativeImportDatabaseName = priorMongo;
            if (sp != null) sp.NativeImportConnectionString = priorSqlite;
            if (np != null) np.NativeImportLabels = priorLabels;
            if (ownedMongo)
            {
                Assert.IsNotNull(mongo);
                Assert.IsTrue(nativeDb.StartsWith("cosmic_distinct_", StringComparison.Ordinal));
                await mongo.DropDatabaseAsync(nativeDb);
                using var names = await mongo.ListDatabaseNamesAsync();
                Assert.IsFalse((await names.ToListAsync()).Contains(nativeDb));
            }
            if (ownedSqlite)
            {
                Assert.AreEqual(Path.GetFullPath(work), Path.GetDirectoryName(sqlitePath));
                foreach (var file in new[] { sqlitePath, sqlitePath + "-wal", sqlitePath + "-shm" })
                {
                    if (File.Exists(file)) File.Delete(file);
                    Assert.IsFalse(File.Exists(file));
                }
            }
            if (ownedNeo)
            {
                Assert.IsNotNull(session);
                Assert.IsTrue(label.StartsWith("DatePartsNative_", StringComparison.Ordinal));
                await (await session.RunAsync($"MATCH (n:{label}) DETACH DELETE n")).ConsumeAsync();
                var remaining = await (await session.RunAsync($"MATCH (n:{label}) RETURN count(n) AS total")).SingleAsync();
                Assert.AreEqual(0L, remaining["total"].As<long>());
            }
        }
    }
}
