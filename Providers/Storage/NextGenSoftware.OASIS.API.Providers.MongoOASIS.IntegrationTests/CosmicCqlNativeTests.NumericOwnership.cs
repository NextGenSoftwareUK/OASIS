using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Fact]
    public Task NativeNumericInputIsNotTruncatedByGraphPublicationBudget()
        => WithMongoGraph(async fixture =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(row => row.Name.StartsWith("owned CQL graph child", StringComparison.Ordinal)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => h.Version).Result));
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(fixture.Root.Children.Sum(child => child.Version), result.Result);
        }, width: 137);

    [Fact]
    public Task NativeNumericCannotHideTraversalFailureOutsideWindow()
        => WithMongoGraph(async fixture =>
        {
            var source = DescendantPlan(fixture, 2, 2, skipRoot: true).Query;
            var failed = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(source.Sum(h => h.Version).Result));
            Assert.Equal("CQL_QUERY_LIMIT", failed.ErrorCode); Assert.False(failed.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(NumericPlan(DescendantPlan(fixture, 2, 3).Query.Sum(h => h.Version).Result));
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception); Assert.Equal(fixture.Root.Version, fresh.Result);
        });

    [Fact]
    public Task NativeNumericMembershipAndValueShareSnapshotAcrossReparenting()
        => WithMongoGraph(async fixture =>
        {
            var plan = NumericPlan(DescendantPlan(fixture, 2, 3).Query.Sum(h => h.Version).Result);
            var child = await fixture.Provider.LoadHolonAsync(fixture.Root.Children.First().Id, loadChildren: false);
            Assert.False(child.IsError, child.Message + child.Exception);
            var admin = fixture.Client.GetDatabase("admin");
            var configured = await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip", 1) },
                { "data", new BsonDocument { { "failCommands", new BsonArray { "aggregate" } },
                    { "appName", "CosmicCqlGraph" }, { "blockConnection", true }, { "blockTimeMS", 5000 } } }
            });
            Task<NextGenSoftware.OASIS.Common.OASISResult<int>>? pending = null;
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
                Assert.False(old.IsError, old.Message + old.Exception); Assert.Equal(fixture.Root.Version, old.Result);
                var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
                Assert.False(fresh.IsError, fresh.Message + fresh.Exception); Assert.True(fresh.IsLoaded); Assert.Equal(0, fresh.Result);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }
        });

    [Fact]
    public Task NativeNumericCancellationDuringActualAggregateReusesOwner()
        => WithMongoGraph(async fixture =>
        {
            var plan = NumericPlan(DescendantPlan(fixture, 2, 3).Query.Sum(h => h.Version).Result);
            var admin = fixture.Client.GetDatabase("admin");
            var configured = await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            { { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip", 1) },
                { "data", new BsonDocument { { "failCommands", new BsonArray { "aggregate" } }, { "appName", "CosmicCqlGraph" },
                    { "blockConnection", true }, { "blockTimeMS", 5000 } } } });
            using var cancellation = new CancellationTokenSource();
            Task<NextGenSoftware.OASIS.Common.OASISResult<int>>? pending = null;
            try
            {
                pending = fixture.Provider.ExecuteCosmicScalarAsync(plan, cancellationToken: cancellation.Token);
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "waitForFailPoint", "failCommand" },
                    { "timesEntered", configured["count"].ToInt64() + 1 }, { "maxTimeMS", 10000 } });
                cancellation.Cancel(); var failed = await pending;
                Assert.Equal("CQL_QUERY_CANCELLED", failed.ErrorCode); Assert.False(failed.IsLoaded);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(plan);
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
        });

}
