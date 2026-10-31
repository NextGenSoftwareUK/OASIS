using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private static CosmicProjectedAllPlan<Holon, TResult> ProjectedAllPlan<TResult>(
        CosmicProjectedAllQuery<Holon, TResult> query, CosmicProjectionCompositionOptions? options = null)
    {
        var result = CosmicProjectedAllPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicProjectionCompositionTarget(ProviderType.MongoDBOASIS, Provider.CqlCapabilities,
                Provider.CqlProjectionCapabilities, Provider.CqlCompositionCapabilities) }, options);
        Assert.False(result.IsError, result.Message); return result.Result;
    }
    private static async Task<bool> ProjectedAllRead<TResult>(MongoGraphFixture fixture, CosmicProjectedAllQuery<Holon, TResult> query)
    {
        var result = await Manager(fixture.Provider).ExecuteProjectedAllQueryAsync(ProjectedAllPlan(query), new[] { fixture.Provider });
        Assert.False(result.IsError, result.Message + result.Exception); Assert.True(result.IsLoaded);
        return Assert.Single(result.Result.Providers).Value;
    }

    [Fact]
    public Task NativeProjectedAllFindsTheCounterexampleBeyondThePublicRowPage()
        => WithMongoGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().OrderBy(row => CosmicQueryMetadata.Value<int>(row, "ordinal"))
                .Select(row => CosmicQueryMetadata.Value<int>(row, "ordinal"));
            Assert.False(await ProjectedAllRead(fixture, selected.All(value => value < 149).Result));
            Assert.True(await ProjectedAllRead(fixture, selected.Take(149).All(value => value < 149).Result));
        }, width: 149, configure: root =>
        {
            root.MetaData["ordinal"] = 0; var index = 1;
            foreach (var child in root.Children) child.MetaData["ordinal"] = index++;
        });

    [Fact]
    public Task NativeProjectedAllPreservesEverySourceAndSelectedWindow()
        => WithMongoGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().OrderBy(row => row.Name).Skip(1).Take(3)
                .Select(row => new { row.Name, row.Version }).Where(row => row.Name != "B")
                .OrderBy(row => row.Name).Skip(1).Take(1);
            Assert.True(await ProjectedAllRead(fixture, selected.All(row => row.Name == "D" && row.Version > 0).Result));
            Assert.False(await ProjectedAllRead(fixture, selected.All(row => row.Name == "C").Result));
        }, configure: root =>
        {
            root.Name = "A"; var children = root.Children.ToArray();
            children[0].Name = "B"; children[1].Name = "C"; children[0].Children.Single().Name = "D";
        });

    [Fact]
    public Task NativeProjectedAllProvesEmptySourceAndEmptySelectedSequence()
        => WithMongoGraph(async fixture =>
        {
            var empty = Guid.Empty;
            Assert.True(await ProjectedAllRead(fixture, CosmicQuery<Holon>.Create().Where(row => row.Id == empty)
                .Select(row => row.Name).All(value => false).Result));
            Assert.True(await ProjectedAllRead(fixture, CosmicQuery<Holon>.Create().Select(row => row.Name)
                .Take(0).All(value => false).Result));
        }, width: 0);

    [Fact]
    public Task NativeProjectedAllDoesNotConsumeAThirtyThirdSelectedStage()
        => WithMongoGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().Select(row => row.Version);
            for (var index = 0; index < 32; index++) selected = selected.Take(1);
            Assert.True(await ProjectedAllRead(fixture, selected.All(value => value > 0).Result));
        }, width: 0);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeProjectedAllCannotHideInvalidRequiredCellsBehindEmptyWindows(bool malformed)
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Select(row => CosmicQueryMetadata.Value<int>(row, "required"))
                .Take(0).All(value => true).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectedAllQueryAsync(ProjectedAllPlan(query), new[] { fixture.Provider });
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.Null(result.Result);
        }, width: 0, configure: root => { if (malformed) root.MetaData["required"] = "wrong-domain"; });

    [Theory]
    [InlineData(3, 100, false)] [InlineData(4, 3, false)] [InlineData(4, 4, true)]
    public Task NativeProjectedAllChecksCompleteInputAndTerminalStageWork(int input, int stage, bool succeeds)
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Select(row => row.Version).All(value => value > 0).Result;
            var plan = ProjectedAllPlan(query, new CosmicProjectionCompositionOptions(input, 10000, stage, 10000));
            var result = await Manager(fixture.Provider).ExecuteProjectedAllQueryAsync(plan, new[] { fixture.Provider });
            Assert.Equal(succeeds, !result.IsError);
            if (succeeds) Assert.True(Assert.Single(result.Result.Providers).Value);
            else { Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode); Assert.Null(result.Result); }
        });

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public Task NativeProjectedAllHonorsSixRemainingBudgetsAndAllowsFreshReuse(int exhausted)
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Select(row => row.Name).All(value => value != null).Result;
            var limits = new[] { 10, 10000, 100, 10000, 10, 10000 }; limits[exhausted] = 0;
            var result = await fixture.Provider.ExecuteCosmicProjectedAllAsync(ProjectedAllPlan(query),
                new CosmicProjectionCompositionExecutionLimits(limits[0],limits[1],limits[2],limits[3],limits[4],limits[5]));
            Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode); Assert.Null(result.Result);
            Assert.True(await ProjectedAllRead(fixture, query));
        }, width: 0);

    public sealed class ProjectedAllNeverConstructed
    {
        public ProjectedAllNeverConstructed() => throw new Exception("All must never construct selected DTOs.");
        public int Revision { get; set; }
    }
    [Fact]
    public Task NativeProjectedAllPreservesNullSemanticsWithoutConstructingDtos()
        => WithMongoGraph(async fixture =>
        {
            var selected = CosmicQuery<Holon>.Create().Select(row => CosmicQueryMetadata.Value<string>(row, "optional"));
            Assert.True(await ProjectedAllRead(fixture, selected.All(value => value == null || value == "coffee").Result));
            Assert.False(await ProjectedAllRead(fixture, selected.All(value => value != null).Result));
            var record = CosmicQuery<Holon>.Create().Select(row => new ProjectedAllNeverConstructed { Revision = row.Version });
            Assert.True(await ProjectedAllRead(fixture, record.All(row => row.Revision > 0).Result));
        }, width: 0, configure: root => root.MetaData["optional"] = null!);

    [Fact]
    public Task NativeProjectedAllPreCancellationCannotPublishAndAllowsReuse()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Select(row => row.Name).All(value => value != null).Result;
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var result = await Manager(fixture.Provider).ExecuteProjectedAllQueryAsync(ProjectedAllPlan(query), new[] { fixture.Provider }, cancellation.Token);
            Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode); Assert.Null(result.Result);
            Assert.True(await ProjectedAllRead(fixture, query));
        }, width: 0);

    [Fact]
    public Task NativeProjectedAllReductionSharesTheSourceSnapshotAcrossConcurrentWrite()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<Holon>.Create().Select(row => row.Name).All(value => value == "coffee").Result;
            var plan = ProjectedAllPlan(query); var admin = fixture.Client.GetDatabase("admin");
            var configured = await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip", 1) },
                { "data", new BsonDocument { { "failCommands", new BsonArray { "aggregate" } },
                    { "appName", "CosmicCqlGraph" }, { "blockConnection", true }, { "blockTimeMS", 3000 } } }
            });
            Task<OASISResult<CosmicScalarExecution<bool>>>? pending = null;
            try
            {
                pending = Manager(fixture.Provider).ExecuteProjectedAllQueryAsync(plan, new[] { fixture.Provider });
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "waitForFailPoint", "failCommand" },
                    { "timesEntered", configured["count"].ToInt64() + 1 }, { "maxTimeMS", 10000 } });
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                fixture.KnownOutcome = false;
                var changed = await fixture.Records.UpdateOneAsync(new BsonDocument("HolonId", fixture.Root.Id.ToString("D")),
                    new BsonDocument("$set", new BsonDocument("Name", "tea")));
                fixture.KnownOutcome = true; Assert.Equal(1, changed.ModifiedCount);
                var result = await pending; Assert.False(result.IsError, result.Message + result.Exception);
                Assert.True(Assert.Single(result.Result.Providers).Value);
                Assert.False(await ProjectedAllRead(fixture, query));
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }
        }, width: 0, configure: root => root.Name = "coffee");
}
