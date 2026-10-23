using System;
using System.Linq;
using System.Threading;
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
    private static CosmicQueryPlan<Holon> DescendantPlan(MongoGraphFixture fixture,int depth=8,int nodes=1000,
        CosmicQueryReadAccess? access=null,bool graph=false,bool skipRoot=false)
    {
        var query=CosmicQuery<Holon>.Create();
        if(access!=null)query=query.WithReadAccess(access).Result;
        var id=fixture.Root.Id;
        var filtered=query.Where(root=>root.Id==id).WhereAnyDescendant(
            child=>child.Name=="owned CQL graph grandchild",CosmicQueryTraversalOptions.Create(depth,nodes).Result);
        if(graph)filtered=filtered.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth:1).Result);
        filtered=filtered.OrderBy(root=>root.Name);
        if(skipRoot)filtered=filtered.Skip(1);
        filtered=filtered.Take(1);
        Assert.False(filtered.IsError,filtered.Message);
        var plan=CosmicQueryPlanner.Plan(filtered.Result,ProviderType.MongoDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
        Assert.False(plan.IsError,plan.Message);return plan.Result;
    }

    [Fact]
    public Task NativeDescendantCertifiesTheActualDepth32AndNodeBoundary()
        =>WithMongoGraph(fixture=>global::OASIS.ProviderTests.Fixtures.CqlDescendantAcceptance.VerifyAsync(fixture.Provider,fixture.Root),
            width:0,configure:global::OASIS.ProviderTests.Fixtures.CqlDescendantAcceptance.Configure);

    [Theory]
    [InlineData(1,false)] [InlineData(2,true)] [InlineData(32,true)]
    public Task NativeDescendantDepthUsesTheCompleteFlatNativeScope(int depth,bool matches)
        =>WithMongoGraph(async fixture=>
        {
            var result=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,depth,3));
            Assert.False(result.IsError,result.Message+result.Exception);Assert.Equal(matches?1:0,result.Result.Count);
            if(matches)Assert.Empty(result.Result.Single().Children);
        });

    [Fact]
    public Task NativeDescendantBudgetCannotBeHiddenByAnEarlierMatchAndConnectionRemainsReusable()
        =>WithMongoGraph(async fixture=>
        {
            var id=fixture.Root.Id;
            var query=CosmicQuery<Holon>.Create().Where(root=>root.Id==id)
                .WhereAnyDescendant(child=>child.HolonType==HolonType.Quest,CosmicQueryTraversalOptions.Create(2,2).Result).Take(1);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.MongoDBOASIS,
                new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            Assert.False(plan.IsError,plan.Message);
            var failed=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.Equal("CQL_QUERY_LIMIT",failed.ErrorCode);Assert.Null(failed.Result);Assert.False(failed.IsLoaded);
            var fresh=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,2,3));
            Assert.False(fresh.IsError,fresh.Message+fresh.Exception);Assert.Single(fresh.Result);
        });

    [Fact]
    public Task NativeDescendantNegativeMatchCannotHideAnotherScopeOverflowInEitherOrder()
        =>WithMongoGraph(async fixture=>
        {
            var id=fixture.Root.Id;
            var basis=CosmicQuery<Holon>.Create().Where(root=>root.Id==id).Result;
            var enough=CosmicQueryTraversalOptions.Create(2,3).Result;
            var tooSmall=CosmicQueryTraversalOptions.Create(2,2).Result;
            var first=basis.WhereAnyDescendant(child=>child.Name=="missing",enough)
                .WhereAnyDescendant(child=>true,tooSmall).Take(1);
            var reverse=basis.WhereAnyDescendant(child=>true,tooSmall)
                .WhereAnyDescendant(child=>child.Name=="missing",enough).Take(1);
            foreach(var query in new[]{first,reverse})
            {
                Assert.False(query.IsError,query.Message);
                var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.MongoDBOASIS,
                    new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
                Assert.False(plan.IsError,plan.Message);
                var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.Equal("CQL_QUERY_LIMIT",read.ErrorCode);Assert.Null(read.Result);Assert.False(read.IsLoaded);
            }
        });

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public Task NativeDescendantPrunesPrivateIntermediateEvenWhenItsGrandchildIsPublic(int visibility)
    {
        var actor=Guid.NewGuid();
        return WithMongoGraph(async fixture=>
        {
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,2,3,
                CosmicQueryReadAccess.OwnerOrPublic(actor).Result));
            Assert.False(read.IsError,read.Message+read.Exception);Assert.Equal(visibility==0?0:1,read.Result.Count);
        },configure:root=>{root.IsPublic=true;root.Children.First().IsPublic=visibility==1;
            root.Children.First().Children.Single().IsPublic=true;},actor:visibility==2?actor:null);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeDescendantPrunesUnlistedIntermediateBeforeCountingOrMatching(bool manifested)
        =>WithMongoGraph(async fixture=>
        {
            var orphan=new Holon{Id=Guid.NewGuid(),ParentHolonId=fixture.Root.Id,Name="unlisted intermediate"};
            var grandchild=new Holon{Id=Guid.NewGuid(),ParentHolonId=orphan.Id,Name="unlisted grandchild"};
            fixture.KnownOutcome=false;
            foreach(var node in new[]{orphan,grandchild})
            {
                var saved=await fixture.Provider.SaveHolonAsync(node,saveChildren:false);
                Assert.False(saved.IsError,saved.Message+saved.Exception);
            }
            fixture.KnownOutcome=true;var id=fixture.Root.Id;
            var query=CosmicQuery<Holon>.Create().Where(root=>root.Id==id).WhereAnyDescendant(
                child=>child.Name=="unlisted grandchild",CosmicQueryTraversalOptions.Create(2,5).Result);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.MongoDBOASIS,
                new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(read.IsError,read.Message+read.Exception);Assert.Equal(manifested?0:1,read.Result.Count);
        },manifest:manifested);

    [Fact]
    public Task NativeDescendantCycleSurvivesAnOtherwiseEmptyRootPage()
        =>WithMongoGraph(async fixture=>
        {
            fixture.KnownOutcome=false;
            await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId",fixture.Root.Id.ToString("D")),
                new BsonDocument("$set",new BsonDocument("ParentHolonId",fixture.Root.Id.ToString("D"))));
            fixture.KnownOutcome=true;
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,2,20,skipRoot:true));
            Assert.Equal("CQL_DATA_INVALID",read.ErrorCode);Assert.Null(read.Result);Assert.False(read.IsLoaded);
        },manifest:false);

    [Fact]
    public Task NativeDescendantIndependentClausesKeepExactInt64Semantics()
        =>WithMongoGraph(async fixture=>
        {
            var id=fixture.Root.Id;
            var query=CosmicQuery<Holon>.Create().Where(root=>root.Id==id)
                .WhereAnyDescendant(child=>CosmicQueryMetadata.Value<long>(child,"precision")==9007199254740993L,
                    CosmicQueryTraversalOptions.Create(2,3).Result)
                .WhereAnyDescendant(child=>child.HolonType==HolonType.Cafe,CosmicQueryTraversalOptions.Create(1,2).Result);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.MongoDBOASIS,
                new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            Assert.False(plan.IsError,plan.Message);
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(read.IsError,read.Message+read.Exception);Assert.Single(read.Result);
        },configure:root=>root.Children.First().Children.Single().MetaData["precision"]=9007199254740993L);

    [Fact]
    public Task NativeDescendantFilteringDoesNotImplyOrDeepenGraphOutput()
        =>WithMongoGraph(async fixture=>
        {
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,2,3,graph:true));
            Assert.False(read.IsError,read.Message+read.Exception);Assert.Equal(2,read.Result.Single().Children.Count());
            Assert.All(read.Result.Single().Children,child=>Assert.Empty(child.Children));
        });

    [Fact]
    public Task NativeDescendantRootIsNeverItsOwnBusinessMatch()
        =>WithMongoGraph(async fixture=>
        {
            var id=fixture.Root.Id;
            var query=CosmicQuery<Holon>.Create().Where(root=>root.Id==id)
                .WhereAnyDescendant(child=>child.Name=="owned CQL graph root",CosmicQueryTraversalOptions.Create(2,3).Result);
            var plan=CosmicQueryPlanner.Plan(query.Result,ProviderType.MongoDBOASIS,
                new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Provider.CqlCapabilities)});
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.False(read.IsError,read.Message+read.Exception);Assert.Empty(read.Result);
        });

    [Fact]
    public Task NativeDescendantEnvelopesCannotBeForgedByPersistedFieldNames()
        =>WithMongoGraph(async fixture=>
        {
            fixture.KnownOutcome=false;
            await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId",fixture.Root.Id.ToString("D")),
                new BsonDocument("$set",new BsonDocument {{"error",1},{"row",new BsonDocument("fake",true)},
                    {"frontier","persisted"},{"matched",false},{"outcomes",new BsonArray {1,2}}}));
            fixture.KnownOutcome=true;
            var read=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,2,3));
            Assert.False(read.IsError,read.Message+read.Exception);Assert.Equal(fixture.Root.Id,Assert.Single(read.Result).Id);
        });

    [Fact]
    public Task NativeDescendantFacetMemoryFailureIsTypedAndTheBudgetIsRestored()
        =>WithMongoGraph(async fixture=>
        {
            // Exclusive owned replica fixture: exercise the engine's actual
            // native guard without allocating a hundred-megabyte test dataset.
            const string setting="internalQueryFacetMaxOutputDocSizeBytes";
            var admin=fixture.Client.GetDatabase("admin");
            var original=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"getParameter",1},{setting,1}});
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"setParameter",1},{setting,1}});
                var failed=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,2,3));
                var cause=Assert.IsType<MongoCommandException>(failed.Exception);
                Assert.Equal(146,cause.Code);
                output.WriteLine("Native facet budget failure: "+cause.Code+" / "+failed.ErrorCode);
                Assert.Equal("CQL_QUERY_LIMIT",failed.ErrorCode);Assert.Null(failed.Result);Assert.False(failed.IsLoaded);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"setParameter",1},{setting,original[setting]}});
                var restored=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"getParameter",1},{setting,1}});
                Assert.Equal(original[setting],restored[setting]);
            }
            var fresh=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture,2,3));
            Assert.False(fresh.IsError,fresh.Message+fresh.Exception);Assert.Single(fresh.Result);
        });

    [Fact]
    public Task NativeDescendantSnapshotRetainsTheGrandchildAcrossAnAccessMutation()
        =>WithMongoGraph(async fixture=>
        {
            var plan=DescendantPlan(fixture,2,3,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result);
            var admin=fixture.Client.GetDatabase("admin");
            var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},
                {"mode",new BsonDocument("skip",1)},{"data",new BsonDocument {{"failCommands",new BsonArray {"aggregate"}},
                    {"appName","CosmicCqlGraph"},{"blockConnection",true},{"blockTimeMS",5000}}}});
            Task<NextGenSoftware.OASIS.Common.OASISResult<System.Collections.Generic.IReadOnlyList<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>>>? pending=null;
            try
            {
                pending=fixture.Provider.ExecuteCosmicQueryAsync(plan);
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"waitForFailPoint","failCommand"},
                    {"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",10000}});
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                fixture.KnownOutcome=false;
                await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId",fixture.Root.Children.First().Children.Single().Id.ToString("D")),
                    new BsonDocument("$set",new BsonDocument("IsPublic",false)));
                fixture.KnownOutcome=true;
                var old=await pending;Assert.False(old.IsError,old.Message+old.Exception);Assert.Single(old.Result);
                var fresh=await fixture.Provider.ExecuteCosmicQueryAsync(plan);
                Assert.False(fresh.IsError,fresh.Message+fresh.Exception);Assert.Empty(fresh.Result);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                if(pending!=null)await pending;
            }
        },configure:root=>{root.IsPublic=true;root.Children.First().IsPublic=true;root.Children.First().Children.Single().IsPublic=true;});

    [Fact]
    public Task NativeDescendantCancellationDuringTraversalPublishesNoRows()
        =>WithMongoGraph(async fixture=>
        {
            var admin=fixture.Client.GetDatabase("admin");
            var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},
                {"mode",new BsonDocument("skip",1)},{"data",new BsonDocument {{"failCommands",new BsonArray {"aggregate"}},
                    {"appName","CosmicCqlGraph"},{"blockConnection",true},{"blockTimeMS",5000}}}});
            using var cancel=new CancellationTokenSource();
            var pending=fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture),cancellationToken:cancel.Token);
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"waitForFailPoint","failCommand"},
                    {"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",10000}});
                cancel.Cancel();var read=await pending;
                Assert.Equal("CQL_QUERY_CANCELLED",read.ErrorCode);Assert.Null(read.Result);Assert.False(read.IsLoaded);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
                await pending;
            }
        });

    [Fact]
    public Task NativeDescendantDeadlineCoversValidationAndTraversalTogether()
        =>WithMongoGraph(async fixture=>
        {
            var admin=fixture.Client.GetDatabase("admin");
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                {"data",new BsonDocument {{"failCommands",new BsonArray {"aggregate"}},
                    {"appName","CosmicCqlGraph"},{"blockConnection",true},{"blockTimeMS",17000}}}});
            try
            {
                var clock=System.Diagnostics.Stopwatch.StartNew();
                var read=await fixture.Provider.ExecuteCosmicQueryAsync(DescendantPlan(fixture));
                Assert.Equal("CQL_QUERY_TIMEOUT",read.ErrorCode);Assert.Null(read.Result);Assert.False(read.IsLoaded);
                Assert.InRange(clock.Elapsed.TotalSeconds,25,40);
            }
            finally
            {await admin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});}
        });
}
