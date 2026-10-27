using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed partial class CqlProjectedAllAcceptanceTests
{
    // Transparent observation only: each call still executes the actual native adapter.
    private sealed class Observed(ICosmicQueryProjectedAllStorageProvider native) : ICosmicQueryProjectedAllStorageProvider
    {
        public ProviderType QueryProviderType => native.QueryProviderType;
        public CosmicQueryCapabilities QueryCapabilities => native.QueryCapabilities;
        public CosmicProjectionCapabilities ProjectionCapabilities => native.ProjectionCapabilities;
        public CosmicProjectionCompositionCapabilities CompositionCapabilities => native.CompositionCapabilities;
        public CosmicProjectionCompositionExecutionLimits? Seen;
        public int Calls;
        public Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicProjectedAllAsync<T, TResult>(CosmicProjectedAllPlan<T, TResult> plan,
            CosmicProjectionCompositionExecutionLimits limits, CancellationToken cancellationToken=default) where T:IHolon
        { Calls++; Seen=limits; return native.ExecuteCosmicProjectedAllAsync(plan,limits,cancellationToken); }
        public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T,TResult>(CosmicProjectionCompositionPlan<T,TResult> plan,
            CosmicProjectionCompositionExecutionLimits limits,CancellationToken cancellationToken=default) where T:IHolon
            => native.ExecuteCosmicProjectionCompositionAsync(plan,limits,cancellationToken);
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,int maximumResults=100,
            CancellationToken cancellationToken=default) where T:IHolon => throw new Exception("All cannot substitute entity rows.");
    }
    private static async Task<OASISResult<CosmicScalarExecution<bool>>> Read<T,TResult>(CosmicProjectedAllQuery<T,TResult> query,
        ICosmicQueryProjectedAllStorageProvider[] adapters,CosmicProjectionCompositionOptions? options=null) where T:IHolon
    {
        Assert.IsNotNull(query,"Builder failure must not become a vacuous true.");
        var plan=CosmicProjectedAllPlanner.Plan(query,adapters[0].QueryProviderType,adapters.Select(adapter=>
            new CosmicProjectionCompositionTarget(adapter.QueryProviderType,adapter.QueryCapabilities,adapter.ProjectionCapabilities,adapter.CompositionCapabilities)).ToArray(),options);
        Assert.IsFalse(plan.IsError,plan.Message);
        return await CosmicProjectedAllExecutor.ExecuteAsync(plan.Result,adapters);
    }

    [TestMethod]
    public Task NativeProjectedAllProtectedScopesCyclesAndPhysicalBudgets()=>VerifyFixture(reviewed:false);

    [TestMethod]
    public Task ReviewedProjectedAllInitialFixtureContinuesWithoutReplayingCreates()=>VerifyFixture(reviewed:true);

    [TestMethod]
    public Task NativeSourceRelationshipCompositionShapesAndConsumers()=>VerifyFixture(reviewed:false,sourceMatrix:true);

    [TestMethod]
    public Task NativeSourceAndProjectedAllDeveloperDemo()=>VerifyFixture(reviewed:false,demo:true);

    [TestMethod]
    public Task NativeStandardQueryableEntityRows()=>VerifyFixture(reviewed:false,linq:true);

    private static async Task VerifyFixture(bool reviewed, bool sourceMatrix=false, bool demo=false, bool linq=false)
    {
        var work=Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var reports=Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_RESULTS");
        Assert.IsFalse(string.IsNullOrWhiteSpace(reports),"The runner must supply this worktree's disposable results directory.");
        var originalPath=Path.Combine(work,"cql-v1-web4-fixture.json");
        var original=await File.ReadAllBytesAsync(originalPath);
        Assert.AreEqual("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5",Convert.ToHexString(SHA256.HashData(original)));
        var journal=Path.Combine(work,"cql-projected-all-acceptance-fixture.json");
        Guid[] ids;var states=new List<object>();
        if(reviewed)
        {
            var preserved=await File.ReadAllBytesAsync(journal);
            Assert.AreEqual("FE53AE55DF84AC35E5A7D0A6787CDC51BB7AC4B5CDFC4207164D38C4B300EA1F",Convert.ToHexString(SHA256.HashData(preserved)),"Only the inspected initial fixture may continue.");
            using var parsed=JsonDocument.Parse(preserved);var prior=parsed.RootElement;
            Assert.AreEqual("created-Neo4jOASIS",prior.GetProperty("stage").GetString());
            ids=prior.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();
            var created=prior.GetProperty("states").EnumerateArray().ToArray();Assert.AreEqual(3,created.Length);
            foreach(var state in created){Assert.IsTrue(state.GetProperty("created").GetBoolean());states.Add(state.Clone());}
        }
        else {Assert.IsFalse(File.Exists(journal),"Preserve/reconcile prior fixture; never replay or reset.");ids=Enumerable.Range(0,6).Select(_=>Guid.NewGuid()).ToArray();}
        Assert.AreEqual(6,ids.Length);Assert.AreEqual(6,ids.Distinct().Count());Assert.IsFalse(ids.Contains(Guid.Empty));
        var textIds=ids.Select(id=>id.ToString("D")).ToArray();
        var actor=Guid.NewGuid();var other=Guid.NewGuid();var checks=new List<object>();
        async Task Checkpoint(string stage)=>await File.WriteAllTextAsync(journal,JsonSerializer.Serialize(new{ids,actor,other,stage,states,reviewed,owner=nameof(NativeProjectedAllProtectedScopesCyclesAndPhysicalBudgets)}));
        if(!reviewed)await Checkpoint("before-preflight");
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var records=new MongoClient(uri).GetDatabase("cosmic_cql_v1_web4_storage").GetCollection<BsonDocument>("Holon");
        var mongoIds=new BsonDocument("HolonId",new BsonDocument("$in",new BsonArray(textIds)));
        if(reviewed)
        {
            var root=await records.Find(new BsonDocument("HolonId",textIds[0])).SingleAsync();
            var child=await records.Find(new BsonDocument("HolonId",textIds[2])).SingleAsync();
            actor=Guid.Parse(root["CreatedByAvatarId"].AsString);other=Guid.Parse(child["CreatedByAvatarId"].AsString);
            Assert.AreNotEqual(Guid.Empty,actor);Assert.AreNotEqual(actor,other);
        }
        await using var sql=new SqliteConnection("Data Source="+Path.Combine(work,"cql-v1-web4.sqlite"));await sql.OpenAsync();
        async Task<long> Sql(string statement,string? one=null,string? parent=null)
        {
            using var command=sql.CreateCommand();command.CommandText=statement;
            for(var index=0;index<ids.Length;index++)command.Parameters.AddWithValue("$id"+index,textIds[index]);
            if(one!=null)command.Parameters.AddWithValue("$one",one);if(parent!=null)command.Parameters.AddWithValue("$parent",parent);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        var sqlIds=string.Join(",",Enumerable.Range(0,ids.Length).Select(index=>"$id"+index));
        using var driver=GraphDatabase.Driver("bolt://127.0.0.1:17687",AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=driver.AsyncSession();
        async Task<long> NeoCount()=> (await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS total",new{ids=textIds})).SingleAsync())["total"].As<long>();
        var expected=reviewed?6L:0L;
        Assert.AreEqual(expected,await records.CountDocumentsAsync(mongoIds));
        Assert.AreEqual(expected,await Sql("SELECT count(*) FROM Holon WHERE Id IN ("+sqlIds+")"));Assert.AreEqual(expected,await NeoCount());
        var providers=new IOASISStorageProvider[]{new MongoProvider(uri,"cosmic_cql_v1_web4_storage"){IsVersionControlEnabled=false},
            new SqliteProvider("Data Source="+Path.Combine(work,"cql-v1-web4.sqlite")){IsVersionControlEnabled=false},
            new Neo4jOASIS("bolt://127.0.0.1:17687","neo4j","oasis-local-test-only"){IsVersionControlEnabled=false}};
        var active=new List<IOASISStorageProvider>();var complete=false;
        var rootId=ids[0];var cycleId=ids[5];
        CosmicQuery<Holon> Source(params ProviderType[] scope)=>CosmicQuery<Holon>.Create(CosmicQueryScope.Named(scope).Result).Where(row=>row.Id==rootId).Result;
        try
        {
            for(var index=0;index<providers.Length;index++)
            {
                var provider=providers[index];Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);active.Add(provider);
                if(reviewed)
                {
                    var retained=await provider.LoadHolonAsync(rootId,loadChildren:false);
                    Assert.IsFalse(retained.IsError,retained.Message+retained.Exception);Assert.AreEqual(actor,retained.Result.CreatedByAvatarId);
                    Assert.AreEqual(index+1,retained.Result.Version);Assert.AreEqual("protected source root",retained.Result.Name);
                    var cycleControl=await provider.LoadHolonAsync(cycleId,loadChildren:false);
                    Assert.IsFalse(cycleControl.IsError,cycleControl.Message+cycleControl.Exception);Assert.AreEqual(Guid.Empty,cycleControl.Result.ParentHolonId);
                    continue; // All creates are acknowledged already; never replay them.
                }
                var root=new Holon{Id=rootId,Name="protected source root",CreatedByAvatarId=actor,IsPublic=true,Version=index+1};
                root.MetaData=new(){["i64"]=9007199254740993L,["u64"]=ulong.MaxValue,["decimal"]=1.0000000000000000000000000001m,
                    ["double"]=1d+2.220446049250313e-16d,["float"]=1f+1.1920929e-7f,["guid"]=actor,
                    ["utc"]=new DateTime(2026,10,27,0,0,0,DateTimeKind.Utc).AddTicks(7),["text"]="Café 😀",["bool"]=true,
                    ["nullable"]=null!,["enum"]=HolonType.Quest,["int"]=-7};
                root.Children.Add(new Holon{Id=ids[1],ParentHolonId=rootId,Name="visible match",CreatedByAvatarId=actor,IsPublic=true,Version=1});
                var later=new Holon{Id=ids[2],ParentHolonId=rootId,Name="private branch",CreatedByAvatarId=other,Version=1};
                later.Children.Add(new Holon{Id=ids[3],ParentHolonId=later.Id,Name="grandchild one",CreatedByAvatarId=other,IsPublic=true,Version=1});
                later.Children.Add(new Holon{Id=ids[4],ParentHolonId=later.Id,Name="grandchild two",CreatedByAvatarId=other,IsPublic=true,Version=1});root.Children.Add(later);
                await Checkpoint("before-create-"+provider.ProviderName);
                var saved=await provider.SaveHolonAsync(root,saveChildren:true);Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
                var cycle=await provider.SaveHolonAsync(new Holon{Id=cycleId,Name="owned cycle",CreatedByAvatarId=actor,IsPublic=true,Version=1},saveChildren:false);
                Assert.IsFalse(cycle.IsError,cycle.Message+cycle.Exception);states.Add(new{provider=provider.ProviderName,created=true});await Checkpoint("created-"+provider.ProviderName);
            }
            var adapters=providers.Cast<ICosmicQueryProjectedAllStorageProvider>().ToArray();
            if(linq)
            {
                foreach(var adapter in adapters) await VerifyLinqRows(adapter,rootId,checks);
                Assert.AreEqual(21,checks.Count);complete=true;return;
            }
            if(demo)
            {
                foreach(var adapter in adapters)
                {
                    var manager=NativeManager(adapter);
                    using var output=new StringWriter();
                    var count=await CosmicCqlDemo.SourceAndProjectedAllDemo.RunAsync(manager,adapter,rootId,actor,output);
                    Assert.AreEqual(8,count);
                    await File.WriteAllTextAsync(Path.Combine(reports!,"cql-source-demo-"+adapter.QueryProviderType+".log"),output.ToString());
                    for(var index=0;index<count;index++)checks.Add(new{provider=adapter.QueryProviderType.ToString(),example=index+1});
                }
                Assert.AreEqual(24,checks.Count);complete=true;return;
            }
            if(sourceMatrix)
            {
                await VerifySourceMatrix(adapters, provider=>Source(provider), actor, checks);
                Assert.AreEqual(216,checks.Count); complete=true; return;
            }
            async Task Expect<TResult>(string label,CosmicProjectedAllQuery<Holon,TResult> query,ICosmicQueryProjectedAllStorageProvider adapter,string? error=null,bool value=true)
            {
                var result=await Read(query,new[]{adapter});
                if(error!=null){Assert.AreEqual(error,result.ErrorCode,label+": "+result.Message+result.Exception);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);}
                else {Assert.IsFalse(result.IsError,label+": "+result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(value,result.Result.Providers.Single().Value,label);}
                checks.Add(new{label,provider=adapter.QueryProviderType.ToString(),error,value=error==null?(bool?)value:null});
            }
            var tight=CosmicQueryTraversalOptions.Create(2,1).Result;var sufficient=CosmicQueryTraversalOptions.Create(2,4).Result;
            foreach(var adapter in adapters)
            {
                var source=Source(adapter.QueryProviderType);
                var errors=new[]{source.WhereAnyDescendant(child=>true,tight),
                    source.Where(row=>row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight)),
                    source.Where(row=>row.Name=="never" && CosmicQueryRelations.AnyDescendant(row,child=>true,tight)),
                    source.WhereAnyChild(child=>child.Name=="visible match" || CosmicQueryRelations.AnyDescendant(child,descendant=>true,tight))};
                foreach(var built in errors)
                {
                    Assert.IsFalse(built.IsError,built.Message);
                    await Expect("protected plain",built.Result.Select(row=>row.Name).All(value=>true).Result,adapter,"CQL_QUERY_LIMIT");
                    await Expect("protected selected empty",built.Result.Select(row=>row.Name).Take(0).All(value=>false).Result,adapter,"CQL_QUERY_LIMIT");
                    await Expect("protected source empty",built.Result.Take(0).Select(row=>row.Name).All(value=>false).Result,adapter,"CQL_QUERY_LIMIT");
                }
                var positive=source.WhereAnyDescendant(child=>child.Name=="grandchild one",sufficient).Select(row=>row.Name);
                await Expect("sufficient traversal",positive.All(value=>value=="protected source root").Result,adapter);
                await Expect("native false",positive.All(value=>value=="never").Result,adapter,value:false);
                var secured=source.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).WhereAnyDescendant(child=>true,tight);
                await Expect("hidden parent excludes branch",secured.Select(row=>row.Name).All(value=>value=="protected source root").Result,adapter);
                async Task Exact<T>(string key,T value)
                {
                    var selected=source.Select(row=>CosmicQueryMetadata.Value<T>(row,key)).Result;
                    var parameter=Expression.Parameter(typeof(T),"value");var literal=Expression.Constant(value,typeof(T));
                    var equal=Expression.Lambda<Func<T,bool>>(Expression.Equal(parameter,literal),parameter);
                    var different=Expression.Lambda<Func<T,bool>>(Expression.NotEqual(parameter,literal),parameter);
                    var query=selected.All(equal);Assert.IsFalse(query.IsError,query.Message);await Expect(key+" exact",query.Result,adapter);
                    query=selected.All(different);Assert.IsFalse(query.IsError,query.Message);await Expect(key+" unequal",query.Result,adapter,value:false);
                }
                await Exact("i64",9007199254740993L);await Exact("u64",ulong.MaxValue);await Exact("decimal",1.0000000000000000000000000001m);
                await Exact("double",1d+2.220446049250313e-16d);await Exact("float",1f+1.1920929e-7f);await Exact("guid",actor);
                await Exact("utc",new DateTime(2026,10,27,0,0,0,DateTimeKind.Utc).AddTicks(7));await Exact("text","Café 😀");
                await Exact("bool",true);await Exact<int?>("nullable",null);await Exact("enum",HolonType.Quest);await Exact("int",-7);
            }
            for(var rotation=0;rotation<3;rotation++)
            {
                var ordered=adapters.Skip(rotation).Concat(adapters.Take(rotation)).ToArray();
                async Task Budget(string label,CosmicProjectionCompositionOptions options,bool emptyLast=false,bool fails=false)
                {
                    var observed=ordered.Select(adapter=>new Observed(adapter)).ToArray();var scope=observed.Select(adapter=>adapter.QueryProviderType).ToArray();
                    var source=Source(scope);if(emptyLast){var excluded=Array.IndexOf(adapters,ordered[2])+1;source=source.Where(row=>row.Version!=excluded).Result;}
                    var query=source.Select(row=>row.Version).All(value=>value>0).Result;
                    var result=await Read(query,observed,options);
                    Assert.AreEqual(1,observed[0].Calls);Assert.AreEqual(1,observed[1].Calls);Assert.AreEqual(1,observed[2].Calls);
                    Assert.AreEqual(options.MaximumInputRows-2,observed[2].Seen!.MaximumInputRows);
                    Assert.AreEqual(options.MaximumInputPayloadBytes-160,observed[2].Seen!.MaximumInputPayloadBytes);
                    Assert.AreEqual(options.MaximumStageRows-2,observed[2].Seen!.MaximumStageRows);
                    Assert.AreEqual(options.MaximumProofPayloadBytes-96,observed[2].Seen!.MaximumProofPayloadBytes);
                    if(fails){Assert.AreEqual("CQL_QUERY_LIMIT",result.ErrorCode,label+result.Message+result.Exception);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);}
                    else {Assert.IsFalse(result.IsError,label+result.Message+result.Exception);Assert.AreEqual(3,result.Result.Providers.Count);Assert.IsTrue(result.Result.Providers.All(item=>item.Value));}
                    checks.Add(new{label,rotation,emptyLast,fails,order=scope.Select(provider=>provider.ToString()).ToArray()});
                }
                await Budget("exact shared bounds",new(3,240,3,144));
                await Budget("shared input rows",new(2,240,3,144),fails:true);
                await Budget("shared input bytes",new(3,160,3,144),fails:true);
                await Budget("shared stage work",new(3,240,2,144),fails:true);
                await Budget("shared proof bytes",new(3,240,3,96),fails:true);
                await Budget("zero remaining proves empty target",new(2,160,2,96),emptyLast:true);
            }
            foreach(var adapter in adapters)
            {
                await Checkpoint("before-cycle-"+adapter.QueryProviderType);
                var current=textIds[5];
                if(adapter.QueryProviderType==ProviderType.MongoDBOASIS)
                {
                    var projection=new BsonDocument{{"schema",1},{"owner",current},{"source",BsonNull.Value},{"declared",false},{"members",new BsonArray()}};
                    var patch=new BsonDocument("$set",new BsonDocument{{"ParentHolonId",current},{"ChildRoleManifestJson",BsonNull.Value},{"CosmicCqlMembership",projection}});
                    Assert.AreEqual(1L,(await records.UpdateOneAsync(new BsonDocument("HolonId",current),patch)).MatchedCount);
                }
                else if(adapter.QueryProviderType==ProviderType.SQLLiteDBOASIS)
                    Assert.AreEqual(1L,await Sql("UPDATE Holon SET ParentHolonId=$parent,PersistentValueJson=json_set(json_remove(PersistentValueJson,'$.ChildRoleManifestJson'),'$.ParentHolonId',$parent) WHERE Id=$one; SELECT changes();",current,current));
                else
                {
                    var stored=await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",new{id=current})).SingleAsync();
                    var canonical=StoredRecordJson.DeserializeStoredHolon(stored["snapshot"].As<string>());canonical.ParentHolonId=cycleId;((Holon)canonical).ChildRoleManifestJson=null;
                    await(await inspection.RunAsync("MATCH (h:Holon {Id:$id})-[p:HAS_CQL_PROJECTION]->(h) SET h.ParentHolonId=$id,h.SnapshotJson=$snapshot,p.f_ParentHolonId=$id,p.f_HasChildManifest=false,p.f_ChildIds=[] MERGE (h)-[:HAS_CHILD]->(h)",
                        new{id=current,snapshot=StoredRecordJson.Serialize(canonical)})).ConsumeAsync();
                }
                states.Add(new{provider=adapter.QueryProviderType.ToString(),cycle=true});await Checkpoint("cycle-installed-"+adapter.QueryProviderType);
                var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result).Where(row=>row.Id==cycleId).Result;
                await Expect("cycle scalar remains valid",source.Select(row=>row.Name).All(value=>value=="owned cycle").Result,adapter);
                await Expect("cycle unvisited empty source",source.Take(0).Select(row=>row.Name).All(value=>false).Result,adapter);
                var bounds=CosmicQueryTraversalOptions.Create(2,2).Result;
                var cycleQueries=new[]{source.WhereAnyDescendant(child=>true,bounds),
                    source.Where(row=>row.Id==cycleId || CosmicQueryRelations.AnyDescendant(row,child=>true,bounds)),
                    source.Where(row=>row.Name=="never" && CosmicQueryRelations.AnyDescendant(row,child=>true,bounds))};
                foreach(var built in cycleQueries)
                {
                    Assert.IsFalse(built.IsError,built.Message);
                    await Expect("cycle plain",built.Result.Select(row=>row.Name).All(value=>true).Result,adapter,"CQL_DATA_INVALID");
                    await Expect("cycle selected empty",built.Result.Select(row=>row.Name).Take(0).All(value=>false).Result,adapter,"CQL_DATA_INVALID");
                    await Expect("cycle source empty",built.Result.Take(0).Select(row=>row.Name).All(value=>false).Result,adapter,"CQL_DATA_INVALID");
                }
            }
            Assert.AreEqual(168,checks.Count);CollectionAssert.AreEqual(original,await File.ReadAllBytesAsync(originalPath));complete=true;
        }
        finally
        {
            try
            {
                if(complete)
                {
                    await Checkpoint("before-exact-cleanup");
                    Assert.AreEqual(6L,(await records.DeleteManyAsync(mongoIds)).DeletedCount);
                    Assert.AreEqual(6L,await Sql("DELETE FROM Holon WHERE Id IN ("+sqlIds+"); SELECT changes();"));
                    await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=textIds})).ConsumeAsync();
                    Assert.AreEqual(0L,await records.CountDocumentsAsync(mongoIds));Assert.AreEqual(0L,await Sql("SELECT count(*) FROM Holon WHERE Id IN ("+sqlIds+")"));Assert.AreEqual(0L,await NeoCount());
                    CollectionAssert.AreEqual(original,await File.ReadAllBytesAsync(originalPath));
                    await File.WriteAllTextAsync(Path.Combine(reports!,linq?"cql-linq-rows-result.json":demo?"cql-source-demo-result.json":sourceMatrix?"cql-source-composition-result.json":"cql-projected-all-acceptance-result.json"),JsonSerializer.Serialize(new{ids,checks=checks.Count,cases=checks,exactCleanupVerified=true,originalFixtureUnchanged=true}));
                    File.Delete(journal);
                }
            }
            finally {foreach(var provider in active)Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);}
        }
    }
}
