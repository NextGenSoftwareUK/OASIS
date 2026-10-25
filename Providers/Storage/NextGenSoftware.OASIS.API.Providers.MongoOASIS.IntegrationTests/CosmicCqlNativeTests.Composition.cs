using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private static CosmicProjectionCompositionPlan<Holon, TResult> CompositionPlan<TResult>(CosmicProjectionQuery<Holon, TResult> query)
    {
        var plan = CosmicProjectionCompositionPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicProjectionCompositionTarget(ProviderType.MongoDBOASIS, Provider.CqlCapabilities,
                Provider.CqlProjectionCapabilities, Provider.CqlCompositionCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task NativeCompositionPreservesSourceWindowAndSelectedStageOrder(bool filterFirst)
        => WithMongoGraph(async fixture =>
        {
            var source = CosmicQuery<Holon>.Create().OrderBy(h => h.Name).Skip(1).Take(3)
                .Select(h => new { h.Name, h.Version });
            var query = (filterFirst
                ? source.Where(row => row.Name != "B").Take(1)
                : source.Take(1).Where(row => row.Name != "B")).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
                CompositionPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var rows = result.Result.Providers.Single().Rows;
            Assert.Equal(filterFirst ? 1 : 0, rows.Count);
            if (filterFirst) Assert.Equal("C", rows.Single().Name);
        }, configure: root =>
        {
            root.Name = "A";
            var children = root.Children.ToArray();
            children[0].Name = "B"; children[1].Name = "C";
            children[0].Children.Single().Name = "D";
        });

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public Task NativeCompositionEnforcesSixRemainingBudgetsAndFreshReuse(int exhausted)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .Select(h => h.Name).Take(1).Result;
            var plan = CompositionPlan(query);
            var limits = new[] { 10, 10000, 100, 10000, 10, 10000 };
            limits[exhausted] = 0;
            var failed = await fixture.Provider.ExecuteCosmicProjectionCompositionAsync(plan,
                new CosmicProjectionCompositionExecutionLimits(limits[0], limits[1], limits[2], limits[3], limits[4], limits[5]));
            Assert.True(failed.IsError, failed.Message);
            Assert.Equal("CQL_QUERY_LIMIT", failed.ErrorCode);
            Assert.Null(failed.Result);
            var fresh = await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(plan, new[] { fixture.Provider });
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
            Assert.Single(fresh.Result.Providers.Single().Rows);
        });

    [Fact]
    public Task NativeCompositionSortRetainsPriorRankOnRepeatedKeys()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().OrderByDescending(h => h.Name)
                .Select(h => new { h.Name, h.Version })
                .OrderBy(row => row.Version).ThenByDescending(row => row.Version).Skip(1).Take(2).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
                CompositionPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(new[] { "C", "B" }, result.Result.Providers.Single().Rows.Select(row => row.Name));
        }, configure: root =>
        {
            root.Name = "A";
            var children = root.Children.ToArray();
            children[0].Name = "B"; children[1].Name = "C";
            children[0].Children.Single().Name = "D";
        });

    [Fact]
    public Task NativeCompositionPreCancellationDoesNotPublishAndAllowsReuse()
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => h.Name).Take(1).Result;
            var plan = CompositionPlan(query);
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var failed = await fixture.Provider.ExecuteCosmicProjectionCompositionAsync(plan,
                new CosmicProjectionCompositionExecutionLimits(10, 10000, 100, 10000, 10, 10000), cancellation.Token);
            Assert.True(failed.IsError); Assert.Equal("CQL_QUERY_CANCELLED", failed.ErrorCode); Assert.Null(failed.Result);
            var fresh = await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(plan, new[] { fixture.Provider });
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
        });

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    [InlineData(8)] [InlineData(9)] [InlineData(10)] [InlineData(11)]
    public Task NativeCompositionPreservesExactSelectedDomains(int domain) => domain switch
    {
        0 => ExactCompositionCell(9007199254740993L),
        1 => ExactCompositionCell(ulong.MaxValue),
        2 => ExactCompositionCell(123456789.123456789123456789m),
        3 => ExactCompositionCell(1.0000000000000002d),
        4 => ExactCompositionCell(1.0000001f),
        5 => ExactCompositionCell(new DateTime(638900000000000001, DateTimeKind.Utc)),
        6 => ExactCompositionCell(Guid.Parse("a0000000-0000-0000-0000-000000000001")),
        7 => ExactCompositionCell(true),
        8 => ExactCompositionCell("coffee' OR 1=1 --"),
        9 => ExactCompositionCell(int.MaxValue),
        10 => ExactCompositionCell((long?)null),
        _ => ExactCompositionCell(ProviderType.MongoDBOASIS)
    };

    private Task ExactCompositionCell<T>(T value) => WithMongoGraph(async fixture =>
    {
        var id = fixture.Root.Id;
        var accepted = new[] { value };
        var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
            .Select(h => new { Value = CosmicQueryMetadata.Value<T>(h, "value") })
            .Where(row => accepted.Contains(row.Value)).OrderBy(row => row.Value).Take(1).Result;
        var result = await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
            CompositionPlan(query), new[] { fixture.Provider });
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal(value, result.Result.Providers.Single().Rows.Single().Value);
    }, width: 0, configure: root => root.MetaData["value"] = value!);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeCompositionZeroTakeCannotHideRequiredSourceErrors(bool malformed)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id)
                .Select(h => CosmicQueryMetadata.Value<int>(h, "required")).Take(0).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
                CompositionPlan(query), new[] { fixture.Provider });
            Assert.True(result.IsError); Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.Null(result.Result);
        }, width: 0, configure: root => { if (malformed) root.MetaData["required"] = "wrong-domain"; });

    [Fact]
    public Task NativeCompositionKeepsSourceAndStagesInOneSnapshotAcrossConcurrentWrite()
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => h.Name)
                .Where(name => name == "coffee").Result;
            var plan = CompositionPlan(query);
            var admin = fixture.Client.GetDatabase("admin");
            var configured = await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip", 1) },
                { "data", new BsonDocument { { "failCommands", new BsonArray { "aggregate" } },
                    { "appName", "CosmicCqlGraph" }, { "blockConnection", true }, { "blockTimeMS", 5000 } } }
            });
            Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicProjectionExecution<string>>>? pending = null;
            try
            {
                pending = Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(plan, new[] { fixture.Provider });
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "waitForFailPoint", "failCommand" },
                    { "timesEntered", configured["count"].ToInt64() + 1 }, { "maxTimeMS", 10000 } });
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                fixture.KnownOutcome = false;
                var changed = await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId", id.ToString("D")),
                    new BsonDocument("$set", new BsonDocument("Name", "tea")));
                fixture.KnownOutcome = true; Assert.Equal(1, changed.ModifiedCount);
                var read = await pending;
                Assert.False(read.IsError, read.Message + read.Exception);
                Assert.Equal("coffee", read.Result.Providers.Single().Rows.Single());
                var fresh = await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(plan, new[] { fixture.Provider });
                Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
                Assert.Empty(fresh.Result.Providers.Single().Rows);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }
        }, width: 0, configure: root => root.Name = "coffee");

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public Task NativeCompositionRelationshipMatchingHonorsChildVisibility(bool descendants, bool publicMatch)
    {
        var actor = Guid.NewGuid();
        var other = Guid.NewGuid();
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var publicId = fixture.Root.Children.Last().Id;
            fixture.KnownOutcome = false;
            var hidden = await fixture.Records.UpdateManyAsync(new BsonDocument("HolonId", new BsonDocument("$ne", id.ToString("D"))),
                new BsonDocument("$set", new BsonDocument { { "CreatedByAvatarId", other.ToString("D") }, { "IsPublic", false } }));
            fixture.KnownOutcome = true;
            Assert.Equal(3, hidden.MatchedCount);
            fixture.KnownOutcome = false;
            var published = await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId", publicId.ToString("D")),
                new BsonDocument("$set", new BsonDocument("IsPublic", true)));
            fixture.KnownOutcome = true;
            Assert.Equal(1, published.MatchedCount);

            var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(h => h.Id == id).Result;
            var label = publicMatch ? "public" : "secret";
            var related = descendants ? source.WhereAnyDescendant(child => child.Name == label, CosmicQueryTraversalOptions.Create(2, 10).Result)
                : source.WhereAnyChild(child => child.Name == label);
            var query = related.Select(h => h.Name).Take(1).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
                CompositionPlan(query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(publicMatch ? 1 : 0, result.Result.Providers.Single().Rows.Count);

            // Root paging also applies after visibility and before result stages.
            var visibleChild = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(h => h.ParentHolonId == id).OrderBy(h => h.Name).Take(1).Select(h => h.Id).Take(1).Result;
            var childRead = await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(
                CompositionPlan(visibleChild), new[] { fixture.Provider });
            Assert.False(childRead.IsError, childRead.Message + childRead.Exception);
            Assert.Equal(publicId, childRead.Result.Providers.Single().Rows.Single());
        }, actor: actor, configure: root =>
        {
            root.Children.First().Name = "secret";
            root.Children.Last().Name = "public";
            root.Children.First().Children.Single().Name = "secret";
        });
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeCompositionInterruptsOutstandingIoAndUsesOneDeadline(bool expire)=>WithMongoGraph(async fixture=>
    {
        var id=fixture.Root.Id;var source=CosmicQuery<Holon>.Create().Where(h=>h.Id==id).Result;
        var query=source.Select(h=>h.Name).Take(1).Where(name=>name!=null).Result;var plan=CompositionPlan(query);
        var budgets=new System.Collections.Concurrent.ConcurrentQueue<long>();
        var settings=MongoClientSettings.FromConnectionString(fixture.Provider.ConnectionString);
        settings.MaxConnectionPoolSize=30;
        settings.ClusterConfigurator=cluster=>cluster.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(command=>
        {
            if(command.CommandName=="aggregate")budgets.Enqueue(command.Command["maxTimeMS"].ToInt64());
        });
        var monitored=new MongoClient(settings);
        fixture.Provider.Database.MongoClient=monitored;
        fixture.Provider.Database.MongoDB=monitored.GetDatabase(fixture.Provider.DBName);
        var admin=fixture.Client.GetDatabase("admin");
        var configured=await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
            {"data",new BsonDocument{{"failCommands",new BsonArray{"aggregate"}},{"appName","CosmicCqlGraph"},
                {"blockConnection",true},{"blockTimeMS",expire?11000:3000}}}});
        using var cancellation=new CancellationTokenSource();
        Task<NextGenSoftware.OASIS.Common.OASISResult<CosmicProjectionCompositionPacket>>? pending=null;BsonDocument? disabled=null;
        try
        {
            pending=fixture.Provider.ExecuteCosmicProjectionCompositionAsync(plan,new CosmicProjectionCompositionExecutionLimits(10,10000,100,10000,10,10000),cancellation.Token);
            if(!expire)
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"waitForFailPoint","failCommand"},
                    {"timesEntered",configured["count"].ToInt64()+1},{"maxTimeMS",10000}});
                cancellation.Cancel();
            }
            var read=await pending;Assert.Equal(expire?"CQL_QUERY_TIMEOUT":"CQL_QUERY_CANCELLED",read.ErrorCode);
            Assert.Null(read.Result);Assert.False(read.IsLoaded);
        }
        finally
        {
            disabled=await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});
            if(pending!=null)await pending;
        }
        if(expire)
        {
            // MongoDB may enforce its native time budget before a third command
            // starts. Observe the actual wire budget rather than assuming when
            // its clock expires: a fresh per-command allowance would fail this.
            var observed=budgets.ToArray();
            output.WriteLine("Native composition aggregate maxTimeMS budgets: "+string.Join(",",observed));
            Assert.True(observed.Length>=2);
            Assert.All(observed,value=>Assert.InRange(value,1L,30000L));
            for(var index=1;index<observed.Length;index++)Assert.True(observed[index]<observed[index-1]);
            Assert.True(disabled["count"].ToInt64()>=configured["count"].ToInt64()+2);
        }
        var fresh=await Manager(fixture.Provider).ExecuteProjectionCompositionQueryAsync(plan,new[]{fixture.Provider});
        Assert.False(fresh.IsError,fresh.Message+fresh.Exception);Assert.Single(fresh.Result.Providers.Single().Rows);
    },width:0);
}
