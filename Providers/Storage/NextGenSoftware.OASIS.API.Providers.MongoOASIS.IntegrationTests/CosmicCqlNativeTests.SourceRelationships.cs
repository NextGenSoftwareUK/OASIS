using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Fact]
    [Trait("Category","OwnedFixtureReconciliation")]
    public async Task ReviewedMongoSourceRelationshipFixtureHasNativeProtectedScopes()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        var state=Path.Combine(workspace,"LocalState/COSMIC/Neo4jCurrent");
        var journal=Path.Combine(state,"cql-projected-all-acceptance-fixture.json");
        var originalPath=Path.Combine(state,"cql-v1-web4-fixture.json");
        var original=await File.ReadAllBytesAsync(originalPath);var preserved=await File.ReadAllBytesAsync(journal);
        Assert.Equal("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5",Convert.ToHexString(SHA256.HashData(original)));
        Assert.Equal("FE53AE55DF84AC35E5A7D0A6787CDC51BB7AC4B5CDFC4207164D38C4B300EA1F",Convert.ToHexString(SHA256.HashData(preserved)));
        using var intent=JsonDocument.Parse(preserved);Assert.Equal("created-Neo4jOASIS",intent.RootElement.GetProperty("stage").GetString());
        var ids=intent.RootElement.GetProperty("ids").EnumerateArray().Select(value=>value.GetGuid()).ToArray();Assert.Equal(6,ids.Distinct().Count());
        var records=new MongoClient(uri).GetDatabase("cosmic_cql_v1_web4_storage").GetCollection<BsonDocument>("Holon");
        var filter=new BsonDocument("HolonId",new BsonDocument("$in",new BsonArray(ids.Select(id=>id.ToString("D")))));
        async Task<string> Fingerprint()
        {
            var rows=await records.Find(filter).Sort(new BsonDocument("HolonId",1)).ToListAsync();Assert.Equal(6,rows.Count);
            return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n",rows.Select(row=>Convert.ToHexString(SHA256.HashData(row.ToBson())))))));
        }
        var before=await Fingerprint();var rootId=ids[0];
        var root=await records.Find(new BsonDocument("HolonId",rootId.ToString("D"))).SingleAsync();var actor=Guid.Parse(root["CreatedByAvatarId"].AsString);
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result).Where(row=>row.Id==rootId).Result;
        var checks=new List<object>();var complete=false;
        var provider=new Provider(uri,"cosmic_cql_v1_web4_storage"){IsVersionControlEnabled=false};
        Assert.False((await provider.ActivateProviderAsync()).IsError);
        try
        {
            async Task Expect<T>(string label,CosmicProjectedAllQuery<Holon,T> query,bool value=true,string? error=null)
            {
                Assert.NotNull(query);var plan=CosmicProjectedAllPlanner.Plan(query,ProviderType.MongoDBOASIS,
                    new[]{new CosmicProjectionCompositionTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities,Provider.CqlProjectionCapabilities,Provider.CqlCompositionCapabilities)});
                Assert.False(plan.IsError,label+plan.Message);
                var result=await Manager(provider).ExecuteProjectedAllQueryAsync(plan.Result,new[]{provider});
                if(error!=null){Assert.Equal(error,result.ErrorCode);Assert.False(result.IsLoaded);Assert.Null(result.Result);}
                else{Assert.False(result.IsError,label+result.Message+result.Exception);Assert.True(result.IsLoaded);Assert.Equal(value,Assert.Single(result.Result.Providers).Value);}
                checks.Add(new{label,value=error==null?(bool?)value:null,error});
            }
            await Expect("root scalar true",source.Select(row=>row.Name).All(value=>value=="protected source root").Result);
            await Expect("root scalar false",source.Select(row=>row.Name).All(value=>false).Result,value:false);
            var tight=CosmicQueryTraversalOptions.Create(2,1).Result;var enough=CosmicQueryTraversalOptions.Create(2,4).Result;
            var inner=CosmicQueryTraversalOptions.Create(2,2).Result;var shallow=CosmicQueryTraversalOptions.Create(1,2).Result;
            var errors=new[]{source.WhereAnyDescendant(child=>true,tight),
                source.Where(row=>row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>true,tight)),
                source.Where(row=>row.Name=="never" && CosmicQueryRelations.AnyDescendant(row,child=>true,tight)),
                source.WhereAnyChild(child=>child.Name=="visible match" || CosmicQueryRelations.AnyDescendant(child,descendant=>true,tight))};
            foreach(var query in errors)
            {
                Assert.False(query.IsError,query.Message);
                await Expect("protected plain",query.Result.Select(row=>row.Name).All(value=>true).Result,error:"CQL_QUERY_LIMIT");
                await Expect("protected selected empty",query.Result.Select(row=>row.Name).Take(0).All(value=>false).Result,error:"CQL_QUERY_LIMIT");
                await Expect("protected source empty",query.Result.Take(0).Select(row=>row.Name).All(value=>false).Result,error:"CQL_QUERY_LIMIT");
            }
            var positive=source.WhereAnyDescendant(child=>child.Name=="grandchild one",enough).Select(row=>row.Name);
            await Expect("sufficient traversal",positive.All(value=>value=="protected source root").Result);
            await Expect("sufficient false",positive.All(value=>false).Result,value:false);
            var secured=source.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
            await Expect("hidden branch excluded",secured.WhereAnyDescendant(child=>true,tight).Select(row=>row.Name).All(value=>false).Result,value:false);
            var matches=new[]{source.Where(row=>row.Children.Any(child=>child.Children.Any(grandchild=>grandchild.Name=="grandchild one"))),
                source.WhereAnyDescendant(child=>child.Children.Any(grandchild=>grandchild.Name=="grandchild one"),shallow),
                source.WhereAnyDescendant(child=>CosmicQueryRelations.AnyDescendant(child,grandchild=>grandchild.Name=="grandchild one",inner),shallow),
                source.WhereAnyChild(child=>CosmicQueryRelations.AnyDescendant(child,grandchild=>grandchild.Name=="grandchild one",inner)),
                source.Where(row=>row.Name=="protected source root" || CosmicQueryRelations.AnyDescendant(row,child=>child.Name=="never",enough)),
                source.Where(row=>!row.Children.Any(child=>child.Name=="missing")),
                source.WhereAnyDescendant(child=>!child.Children.Any(grandchild=>grandchild.Name=="grandchild one"),shallow)};
            foreach(var query in matches){Assert.False(query.IsError,query.Message);await Expect("composed match",query.Result.Select(row=>row.Name).All(value=>false).Result,value:false);}
            await Expect("hidden intermediate parent",secured.WhereAnyChild(child=>CosmicQueryRelations.AnyDescendant(child,grandchild=>grandchild.Name=="grandchild one",inner)).Select(row=>row.Name).All(value=>false).Result);
            await Expect("compound negative source",source.Where(row=>row.Name=="never" && CosmicQueryRelations.AnyDescendant(row,child=>true,enough)).Select(row=>row.Name).All(value=>false).Result);
            async Task Exact<T>(string key,T value)
            {
                var selected=source.Select(row=>CosmicQueryMetadata.Value<T>(row,key)).Result;Assert.NotNull(selected);
                var parameter=Expression.Parameter(typeof(T),"value");var literal=Expression.Constant(value,typeof(T));
                var equal=selected.All(Expression.Lambda<Func<T,bool>>(Expression.Equal(parameter,literal),parameter));
                Assert.False(equal.IsError,equal.Message);await Expect(key+" exact",equal.Result);
                var unequal=selected.All(Expression.Lambda<Func<T,bool>>(Expression.NotEqual(parameter,literal),parameter));
                Assert.False(unequal.IsError,unequal.Message);await Expect(key+" unequal",unequal.Result,value:false);
            }
            await Exact("i64",9007199254740993L);await Exact("u64",ulong.MaxValue);await Exact("decimal",1.0000000000000000000000000001m);
            await Exact("double",1d+2.220446049250313e-16d);await Exact("float",1f+1.1920929e-7f);await Exact("guid",actor);
            await Exact("utc",new DateTime(2026,10,27,0,0,0,DateTimeKind.Utc).AddTicks(7));await Exact("text","Café 😀");
            await Exact("bool",true);await Exact<int?>("nullable",null);await Exact("enum",HolonType.Quest);await Exact("int",-7);
            var shared=source.Where(row=>row.Name=="protected source root"
                || CosmicQueryRelations.AnyDescendant(row,child=>child.Name=="missing",enough)).Result;
            var rowsPlan=CosmicQueryPlanner.Plan(shared,ProviderType.MongoDBOASIS,
                new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            Assert.False(rowsPlan.IsError,rowsPlan.Message);
            var rows=await provider.ExecuteCosmicQueryAsync(rowsPlan.Result);
            Assert.False(rows.IsError,rows.Message+rows.Exception);Assert.True(rows.IsLoaded);Assert.Equal(rootId,Assert.Single(rows.Result).Id);
            checks.Add(new{label="shared row source"});
            var countPlan=CosmicScalarPlanner.Plan(shared.LongCount().Result,ProviderType.MongoDBOASIS,
                new[]{new CosmicScalarTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities,Provider.CqlScalarCapabilities)});
            Assert.False(countPlan.IsError,countPlan.Message);
            var count=await provider.ExecuteCosmicScalarAsync(countPlan.Result);
            Assert.False(count.IsError,count.Message+count.Exception);Assert.True(count.IsLoaded);Assert.Equal(1L,count.Result);
            checks.Add(new{label="shared scalar source"});
            var projectionPlan=CosmicProjectionPlanner.Plan(shared.Select(row=>row.Name).Result,ProviderType.MongoDBOASIS,
                new[]{new CosmicProjectionTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities,Provider.CqlProjectionCapabilities)});
            Assert.False(projectionPlan.IsError,projectionPlan.Message);
            var projection=await Manager(provider).ExecuteProjectionQueryAsync(projectionPlan.Result,new[]{provider});
            Assert.False(projection.IsError,projection.Message+projection.Exception);Assert.True(projection.IsLoaded);
            Assert.Equal("protected source root",Assert.Single(Assert.Single(projection.Result.Providers).Rows));
            checks.Add(new{label="shared detached projection source"});
            Assert.Equal(53,checks.Count);complete=true;
        }
        finally
        {
            Assert.False((await provider.DeActivateProviderAsync()).IsError);
            var after=await Fingerprint();Assert.Equal(before,after);
            Assert.Equal(preserved,await File.ReadAllBytesAsync(journal));Assert.Equal(original,await File.ReadAllBytesAsync(originalPath));
            var path=Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/mongo-source-relationships-summary.json");
            await File.WriteAllTextAsync(path,JsonSerializer.Serialize(new{complete,checksPassed=checks.Count,checks,fixtureFingerprint=after,fixtureUnchanged=true,journalsUnchanged=true,createsReplayed=0}));
        }
    }
}
