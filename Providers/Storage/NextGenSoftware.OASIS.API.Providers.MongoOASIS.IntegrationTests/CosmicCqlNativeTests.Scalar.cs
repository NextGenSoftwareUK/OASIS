using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private static CosmicScalarPlan<T, TResult> ScalarPlan<T, TResult>(CosmicScalarQuery<T, TResult> query)
        where T : Core.Interfaces.IHolon
    {
        var plan = CosmicScalarPlanner.Plan(query, Core.Enums.ProviderType.MongoDBOASIS, new[]
        {
            new CosmicScalarTarget(Core.Enums.ProviderType.MongoDBOASIS, Provider.CqlCapabilities, Provider.CqlScalarCapabilities)
        });
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    public sealed class ScalarMustNotConstruct : Holon
    {
        public ScalarMustNotConstruct() => throw new InvalidOperationException("A scalar must not construct a tracked entity.");
        [Core.CustomAttrbiutes.CustomOASISProperty] public int Rating { get; set; }
    }

    [Theory]
    [InlineData(0, -1, 4)] [InlineData(1, 2, 2)] [InlineData(3, 100, 1)]
    [InlineData(9, 2, 0)] [InlineData(0, 0, 0)]
    public Task NativeScalarWindowsAndTypedSourcesNeverHydrateOrTruncateEntities(int skip, int take, int expected)
        => WithMongoGraph(async fixture =>
        {
            var source = CosmicQuery<ScalarMustNotConstruct>.Create()
                .Where(row => row.Name.StartsWith("owned CQL graph", StringComparison.Ordinal))
                .OrderByDescending(row => row.Name).Skip(skip).Result;
            if (take >= 0) source = source.Take(take).Result;
            var count = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Count().Result));
            Assert.False(count.IsError, count.Message + count.Exception); Assert.True(count.IsLoaded); Assert.Equal(expected, count.Result);
            var wide = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.LongCount().Result));
            Assert.False(wide.IsError, wide.Message + wide.Exception); Assert.Equal((long)expected, wide.Result);
            var any = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Any().Result));
            Assert.False(any.IsError, any.Message + any.Exception); Assert.Equal(expected != 0, any.Result);
            var managed = await Manager(fixture.Provider).ExecuteScalarQueryAsync(ScalarPlan(source.LongCount().Result),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(managed.IsError, managed.Message + managed.Exception);
            Assert.Equal((long)expected, managed.Result.Providers.Single().Value);
        }, configure: root => root.MetaData["Rating"] = "not an integer");

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeScalarAccessPrecedesParticipation(bool isPublic)
    {
        var actor = Guid.NewGuid();
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var owner = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
                .Where(row => row.Id == id).Count().Result;
            var own = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(owner));
            Assert.False(own.IsError, own.Message + own.Exception); Assert.Equal(1, own.Result);
            var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
                .Where(row => row.Id == id).Result;
            var foreign = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.LongCount().Result));
            Assert.False(foreign.IsError, foreign.Message + foreign.Exception); Assert.Equal(isPublic ? 1L : 0L, foreign.Result);
            var any = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Any().Result));
            Assert.False(any.IsError, any.Message + any.Exception); Assert.Equal(isPublic, any.Result);
        }, actor: actor, configure: root => root.IsPublic = isPublic);
    }

    [Fact]
    public Task NativeScalarInputIsNotLimitedByRowOrGraphPublicationBudgets()
        => WithMongoGraph(async fixture =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(row => row.Name.StartsWith("owned CQL graph child", StringComparison.Ordinal)).LongCount().Result;
            var count = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source));
            Assert.False(count.IsError, count.Message + count.Exception); Assert.Equal(137L, count.Result);
        }, width: 137);

    [Fact]
    public Task NativeScalarAnyCannotHideDescendantLimitsAndCanReadAgain()
        => WithMongoGraph(async fixture =>
        {
            var source = DescendantPlan(fixture, 2, 2).Query;
            var limited = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Any().Result));
            Assert.Equal("CQL_QUERY_LIMIT", limited.ErrorCode); Assert.False(limited.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(DescendantPlan(fixture, 2, 3).Query.LongCount().Result));
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception); Assert.Equal(1L, fresh.Result);
        });

    [Fact]
    public Task NativeScalarEmptySourceHasExactZeroAndFalseResults()
        => WithMongoGraph(async fixture =>
        {
            var source = CosmicQuery<Holon>.Create().Where(row => row.Name == "absent scalar source").Result;
            var count = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Count().Result));
            Assert.False(count.IsError, count.Message + count.Exception); Assert.Equal(0, count.Result); Assert.True(count.IsLoaded);
            var any = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source.Any().Result));
            Assert.False(any.IsError, any.Message + any.Exception); Assert.False(any.Result);
        }, width: 0);

    [Fact]
    public Task NativeScalarPreCancelledEmptyWindowDoesNotPublishSuccess()
        => WithMongoGraph(async fixture =>
        {
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var source = CosmicQuery<Holon>.Create().Take(0).LongCount().Result;
            var failed = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source), cancellation.Token);
            Assert.Equal("CQL_QUERY_CANCELLED", failed.ErrorCode); Assert.False(failed.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(ScalarPlan(source));
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception); Assert.Equal(0L, fresh.Result);
        }, width: 0);

    [Fact]
    public Task NativeScalarMembershipAndCountShareTheSnapshotAcrossReparenting()
        => WithMongoGraph(async fixture =>
        {
            var plan = ScalarPlan(DescendantPlan(fixture, 2, 3).Query.LongCount().Result);
            var child = await fixture.Provider.LoadHolonAsync(fixture.Root.Children.First().Id, loadChildren: false);
            Assert.False(child.IsError, child.Message + child.Exception);
            var admin = fixture.Client.GetDatabase("admin");
            var configured = await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip", 1) },
                { "data", new BsonDocument { { "failCommands", new BsonArray { "aggregate" } },
                    { "appName", "CosmicCqlGraph" }, { "blockConnection", true }, { "blockTimeMS", 5000 } } }
            });
            Task<NextGenSoftware.OASIS.Common.OASISResult<long>>? pending = null;
            try
            {
                pending = fixture.Provider.ExecuteCosmicScalarAsync(plan);
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "waitForFailPoint", "failCommand" },
                    { "timesEntered", configured["count"].ToInt64() + 1 }, { "maxTimeMS", 10000 } });
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                child.Result.ParentHolonId = Guid.NewGuid();
                fixture.KnownOutcome = false;
                var changed = await fixture.Provider.SaveHolonAsync(child.Result, saveChildren: false);
                fixture.KnownOutcome = !changed.IsError;
                Assert.False(changed.IsError, changed.Message + changed.Exception);
                var old = await pending;
                Assert.False(old.IsError, old.Message + old.Exception); Assert.Equal(1L, old.Result);
                var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
                Assert.False(fresh.IsError, fresh.Message + fresh.Exception); Assert.Equal(0L, fresh.Result);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }
        });
}
