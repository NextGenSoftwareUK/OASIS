using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private sealed class MongoGraphFixture
    {
        public Provider Provider=null!;
        public IMongoDatabase Database=null!;
        public IMongoClient Client=null!;
        public Holon Root=null!;
        public bool KnownOutcome;
        public IMongoCollection<BsonDocument> Records=>Database.GetCollection<BsonDocument>("Holon");
    }

    private async Task WithMongoGraph(Func<MongoGraphFixture,Task> verify,bool manifest=true,int width=2,bool largeChild=false,Action<Holon>? configure=null)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-cql-environment.lock"),
            FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_cql_fixture";
        var client=new MongoClient(uri);
        using var names=await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);
        await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri+"&appName=CosmicCqlGraph",name);
        var fixture=new MongoGraphFixture{Provider=provider,Database=database,Client=client,
            Root=new Holon{Id=Guid.NewGuid(),Name="owned CQL graph root"}};
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            for(var i=0;i<width;i++)
                fixture.Root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=fixture.Root.Id,
                    Name="owned CQL graph child "+i,HolonType=i==0?HolonType.Quest:HolonType.Cafe,
                    Description=largeChild&&i==0?new string('x',10000):"child before"});
            if(width==2)
            {
                var quest=fixture.Root.Children.First();
                quest.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=quest.Id,
                    HolonType=HolonType.Quest,Name="owned CQL graph grandchild"});
            }
            configure?.Invoke(fixture.Root);
            var nodes=new List<IHolon>{fixture.Root};
            for(var index=0;index<nodes.Count;index++)nodes.AddRange(nodes[index].Children);
            if(manifest)
            {
                // Assigned fixture IDs deliberately declare create-only preconditions.
                var expected=nodes.ToDictionary(node=>node.Id,_=>(Guid?)null);
                var seeded=await Manager(provider).SaveHolonAsync(fixture.Root,Guid.NewGuid(),
                    new CosmicWriteOptions(Guid.NewGuid(),expected),saveChildren:true,providerType:ProviderType.MongoDBOASIS);
                Assert.False(seeded.IsError,seeded.Message+seeded.Exception);
            }
            else
                foreach(var node in nodes)
                {
                    var seeded=await provider.SaveHolonAsync(node,saveChildren:false);
                    Assert.False(seeded.IsError,seeded.Message+seeded.Exception);
                }
            // Later query assertion failures are safe to clean up: all fixture
            // writes are acknowledged and queries themselves cannot mutate it.
            fixture.KnownOutcome=true;
            await verify(fixture);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
            if(fixture.KnownOutcome)await client.DropDatabaseAsync(name);
            else output.WriteLine("Preserved owned graph fixture after an unknown/unacknowledged write; inspect before reuse.");
        }
    }

    [Theory]
    [InlineData(true,true)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(false,false)]
    public Task NativeGraphAccessFiltersBeforeChildBudget(bool visible,bool manifested)
        =>WithMongoGraph(async fixture=>
        {
            var policy=CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
            var plan=MongoGraphPlan(fixture.Root,CosmicQueryGraphOptions.Create(maximumNodes:visible?2:1).Result,policy);
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan);
            Assert.False(read.IsError,read.Message+read.Exception);
            var root=Assert.Single(read.Result);
            Assert.Equal(visible?1:0,root.Children.Count());
            Assert.All(root.Children,child=>{Assert.True(child.IsPublic);Assert.Empty(child.Children);});
            if(manifested)Assert.Equal(2,Core.Serialization.HolonChildRoleManifestJson.Deserialize(((Holon)root).ChildRoleManifestJson).Roles.Single().ChildIds.Count);
        },manifest:manifested,configure:root=>{root.IsPublic=true;root.Children.First().IsPublic=visible;});

    private static CosmicQuery<Holon> BindNativeGraphPolicy(CosmicQuery<Holon> query,CosmicQueryReadAccess policy)
    {
        var bound=query.WithReadAccess(policy);
        if(bound.IsError)throw new InvalidOperationException(bound.Message);
        return bound.Result;
    }

    private static CosmicQueryPlan<Holon> MongoGraphPlan(Holon root,CosmicQueryGraphOptions options,CosmicQueryReadAccess? access=null)
    {
        var id=root.Id;
        var query=CosmicQuery<Holon>.Create().Where(row=>row.Id==id).WithChildren(options);
        Assert.False(query.IsError,query.Message);
        var plan=CosmicQueryPlanner.Plan(access==null ? query.Result : BindNativeGraphPolicy(query.Result,access),ProviderType.MongoDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
        Assert.False(plan.IsError,plan.Message);
        return plan.Result;
    }

    [Theory]
    [InlineData(true,0,true)] [InlineData(true,1,false)] [InlineData(true,2,true)]
    [InlineData(false,0,false)] [InlineData(false,2,false)]
    public Task NativeCqlGraphRespectsDepthAndRecursion(bool recursive,int depth,bool grandchildren)
        =>WithMongoGraph(async fixture=>
        {
            var options=CosmicQueryGraphOptions.Create(recursive:recursive,maxChildDepth:depth).Result;
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,options));
            Assert.False(read.IsError,read.Message+read.Exception);
            var root=Assert.Single(read.Result);
            Assert.Equal(fixture.Root.Children.Select(child=>child.Id),root.Children.Select(child=>child.Id));
            var quest=Assert.Single(root.Children.Where(child=>child.HolonType==HolonType.Quest));
            Assert.Equal(grandchildren?1:0,quest.Children.Count());
        });

    [Theory]
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public Task NativeCqlGraphPartialViewsCannotReplaceUnseenChildren(bool manifest,bool accessBound)
        =>WithMongoGraph(async fixture=>
        {
            var manager=Manager(fixture.Provider);
            var policy=accessBound?CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result:null;
            var shape=CosmicQueryGraphOptions.Create(maxChildDepth:1,childType:accessBound?HolonType.All:HolonType.Quest).Result;
            var baseline=await fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,CosmicQueryGraphOptions.Create().Result));
            if(baseline.IsError)throw new InvalidOperationException(baseline.Message);
            var childState=baseline.Result.Single().Children.ToDictionary(child=>child.Id,
                child=>(child.Version,CosmicConcurrency.GetProviderRevisionId(child,ProviderType.MongoDBOASIS)));
            var plan=MongoGraphPlan(fixture.Root,shape,policy);
            ICosmicQueryStorageProvider adapter=fixture.Provider;
            var read=await manager.ExecuteQueryAsync(plan,new[]{adapter});
            Assert.False(read.IsError,read.Message+read.Exception);
            var root=Assert.Single(Assert.Single(read.Result.Providers).Rows);
            Assert.Equal(HolonType.Quest,Assert.Single(root.Children).HolonType);
            root.AcceptChanges();
            root.Name+=" edited";
            var version=root.Version;
            var rejected=await manager.SaveHolonAsync(root,Guid.NewGuid(),saveChildren:true,providerType:ProviderType.MongoDBOASIS);
            Assert.True(rejected.IsError);
            Assert.False(rejected.IsSaved);
            Assert.Equal(version,root.Version);
            fixture.KnownOutcome=false;
            var saved=await manager.SaveHolonAsync(root,Guid.NewGuid(),saveChildren:false,providerType:ProviderType.MongoDBOASIS);
            fixture.KnownOutcome=!saved.IsError;
            Assert.False(saved.IsError,saved.Message+saved.Exception);
            var fresh=await fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,CosmicQueryGraphOptions.Create().Result));
            Assert.False(fresh.IsError,fresh.Message+fresh.Exception);
            Assert.Equal(2,Assert.Single(fresh.Result).Children.Count());
            foreach(var child in fresh.Result.Single().Children)
                Assert.Equal(childState[child.Id],(child.Version,CosmicConcurrency.GetProviderRevisionId(child,ProviderType.MongoDBOASIS)));
            Assert.Equal(version+1,fresh.Result.Single().Version);
        },manifest:manifest,configure:root=>{root.IsPublic=true;root.Children.First().IsPublic=true;});

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeCqlGraphRejectsBudgetsWithoutLeavingASnapshotOwner(bool payload)
        =>WithMongoGraph(async fixture=>
        {
            var options=CosmicQueryGraphOptions.Create(maximumNodes:payload?100:2,maximumPayloadBytes:payload?8000:32000).Result;
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,options));
            Assert.Equal("CQL_QUERY_LIMIT",read.ErrorCode);
            Assert.Null(read.Result);Assert.False(read.IsLoaded);
            var leaf=CosmicQuery<Holon>.Create().Where(row=>row.Name=="owned CQL graph root");
            var plan=CosmicQueryPlanner.Plan(leaf.Result,ProviderType.MongoDBOASIS,
                new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            var recovered=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(recovered.IsError,recovered.Message+recovered.Exception);Assert.Single(recovered.Result);
        },largeChild:payload);

    [Fact]
    public Task NativeCqlGraphBatchesMembershipBeyondOneLiteralList()
        =>WithMongoGraph(async fixture=>
        {
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,
                CosmicQueryGraphOptions.Create(maximumNodes:151).Result));
            Assert.False(read.IsError,read.Message+read.Exception);
            Assert.Equal(fixture.Root.Children.Select(child=>child.Id),Assert.Single(read.Result).Children.Select(child=>child.Id));
        },width:150);

    [Fact]
    public Task NativeCqlGraphPagesRootsBeforeExpandingChildren()
        =>WithMongoGraph(async fixture=>
        {
            fixture.KnownOutcome=false;
            var other=new Holon{Id=Guid.NewGuid(),Name="zz owned CQL graph root"};
            var saved=await fixture.Provider.SaveHolonAsync(other,saveChildren:false);
            fixture.KnownOutcome=!saved.IsError;
            Assert.False(saved.IsError,saved.Message+saved.Exception);
            var ids=new[]{fixture.Root.Id,other.Id};
            var query=CosmicQuery<Holon>.Create().Where(row=>ids.Contains(row.Id))
                .OrderBy(row=>row.Name).Take(1).WithChildren(CosmicQueryGraphOptions.Create(maximumNodes:4).Result);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.MongoDBOASIS,
                new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            Assert.False(plan.IsError,plan.Message);
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(read.IsError,read.Message+read.Exception);
            Assert.Equal(fixture.Root.Id,Assert.Single(read.Result).Id);
        });

    [Fact]
    public Task NativeCqlGraphRejectsMissingPersistedMembers()
        =>WithMongoGraph(async fixture=>
        {
            fixture.KnownOutcome=false;
            await fixture.Records.DeleteOneAsync(new BsonDocument("HolonId",fixture.Root.Children.First().Id.ToString("D")));
            fixture.KnownOutcome=true;
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,CosmicQueryGraphOptions.Create().Result));
            Assert.Equal("CQL_DATA_INVALID",read.ErrorCode);Assert.Null(read.Result);
        });

    [Fact]
    public Task NativeCqlGraphRejectsParentLinkCycles()
        =>WithMongoGraph(async fixture=>
        {
            fixture.KnownOutcome=false;
            await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId",fixture.Root.Id.ToString("D")),
                new BsonDocument("$set",new BsonDocument("ParentHolonId",fixture.Root.Children.First().Id.ToString("D"))));
            fixture.KnownOutcome=true;
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,CosmicQueryGraphOptions.Create().Result));
            Assert.Equal("CQL_DATA_INVALID",read.ErrorCode);Assert.Null(read.Result);
        },manifest:false);

    [Fact]
    public Task NativeCqlGraphRetainsTheRootSnapshotAcrossAConcurrentChildWrite()
        =>WithMongoGraph(async fixture=>
        {
            var admin=fixture.Client.GetDatabase("admin");
            var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},
                {"mode",new BsonDocument("skip",1)},{"data",new BsonDocument{{"failCommands",new BsonArray{"aggregate"}},
                    {"appName","CosmicCqlGraph"},{"blockConnection",true},{"blockTimeMS",5000}}}});
            Task<NextGenSoftware.OASIS.Common.OASISResult<IReadOnlyList<IHolon>>>? pending=null;
            try
            {
                pending=fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result));
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"waitForFailPoint","failCommand"},
                    {"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",10000}});
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});
                fixture.KnownOutcome=false;
                await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId",fixture.Root.Children.First().Id.ToString("D")),
                    new BsonDocument("$set",new BsonDocument("Description","child after")));
                fixture.KnownOutcome=true;
                var read=await pending;
                Assert.False(read.IsError,read.Message+read.Exception);
                Assert.Equal("child before",Assert.Single(read.Result).Children.First().Description);
                var current=await fixture.Records.Find(new BsonDocument("HolonId",fixture.Root.Children.First().Id.ToString("D"))).SingleAsync();
                Assert.Equal("child after",current["Description"].AsString);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});
                if(pending!=null)await pending;
            }
        });

    [Fact]
    public Task NativeCqlGraphDeadlineCoversTheWholeExpansionRatherThanEachCommand()
        =>WithMongoGraph(async fixture=>
        {
            var admin=fixture.Client.GetDatabase("admin");
            // Each native command is individually shorter than the 30-second
            // contract. Two commands together must exhaust the one read owner.
            var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                {"data",new BsonDocument{{"failCommands",new BsonArray{"aggregate"}},{"appName","CosmicCqlGraph"},
                    {"blockConnection",true},{"blockTimeMS",17000}}}});
            BsonDocument? disabled=null;
            try
            {
                var read=await fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,CosmicQueryGraphOptions.Create().Result));
                Assert.Equal("CQL_QUERY_TIMEOUT",read.ErrorCode);
                Assert.Null(read.Result);Assert.False(read.IsLoaded);
            }
            finally
            {
                disabled=await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});
            }
            Assert.True(disabled["count"].ToInt64()>=configured["count"].ToInt64()+2,
                "The cumulative deadline must be observed after both root and descendant native commands actually start.");
        });

    [Fact]
    public Task NativeCqlGraphCancelsDuringNativeReadWithoutPublishingRows()
        =>WithMongoGraph(async fixture=>
        {
            var admin=fixture.Client.GetDatabase("admin");
            var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                {"data",new BsonDocument{{"failCommands",new BsonArray{"aggregate"}},{"appName","CosmicCqlGraph"},
                    {"blockConnection",true},{"blockTimeMS",3000}}}});
            using var cancellation=new CancellationTokenSource();
            Task<NextGenSoftware.OASIS.Common.OASISResult<IReadOnlyList<IHolon>>>? pending=null;
            try
            {
                pending=fixture.Provider.ExecuteCosmicQueryAsync(MongoGraphPlan(fixture.Root,CosmicQueryGraphOptions.Create().Result),
                    cancellationToken:cancellation.Token);
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"waitForFailPoint","failCommand"},
                    {"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",10000}});
                cancellation.Cancel();
                var read=await pending;
                Assert.Equal("CQL_QUERY_CANCELLED",read.ErrorCode);Assert.Null(read.Result);Assert.False(read.IsLoaded);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});
                if(pending!=null)await pending;
            }
        });
}
