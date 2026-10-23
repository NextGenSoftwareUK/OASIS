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
    public Task NativeMinMaxInputIsNotTruncatedByGraphPublicationBudget()
        => WithMongoGraph(async fixture =>
        {
            var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result;
            var source = CosmicQuery<Holon>.Create().WithChildren(graph)
                .Where(row => row.Name.StartsWith("owned CQL graph child", StringComparison.Ordinal)).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => h.Version).Result));
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(fixture.Root.Children.Min(child => child.Version), result.Result);
        }, width: 137);

    [Fact]
    public Task NativeMinMaxCannotHideTraversalFailureOutsideWindow()
        => WithMongoGraph(async fixture =>
        {
            var source = DescendantPlan(fixture, 2, 2, skipRoot: true).Query;
            var failed = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => h.Version).Result));
            Assert.Equal("CQL_QUERY_LIMIT", failed.ErrorCode); Assert.False(failed.IsLoaded);
            var fresh = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(DescendantPlan(fixture, 2, 3).Query.Min(h => h.Version).Result));
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception); Assert.Equal(fixture.Root.Version, fresh.Result);
        });

    [Fact]
    public Task NativeMinMaxMembershipAndValueShareSnapshotAcrossReparenting()
        => WithMongoGraph(async fixture =>
        {
            var plan = MinMaxPlan(DescendantPlan(fixture, 2, 3).Query.Min(h => h.Version).Result);
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
                Assert.Equal("CQL_EMPTY_SEQUENCE", fresh.ErrorCode); Assert.False(fresh.IsLoaded);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }
        });

    [Fact]
    public Task NativeMinMaxCancellationDuringActualAggregateReusesOwner()
        => WithMongoGraph(async fixture =>
        {
            var plan = MinMaxPlan(DescendantPlan(fixture, 2, 3).Query.Min(h => h.Version).Result);
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

    [Theory]
    [InlineData(false, false)] [InlineData(true, true)] [InlineData(false, true)]
    public Task NativeMinMaxGuidComparisonNormalizesPersistedUuidRepresentations(bool rootLegacy, bool childLegacy)
        => WithMongoGraph(async fixture =>
        {
            var high = Guid.Parse("01000000-0000-0000-0000-000000000000");
            var low = Guid.Parse("00000001-0000-0000-0000-000000000000");
            var child = fixture.Root.Children.First();
            foreach (var item in new[] { (fixture.Root.Id, high, rootLegacy), (child.Id, low, childLegacy) })
            {
                fixture.KnownOutcome = false;
                var changed = await fixture.Records.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("HolonId", item.Item1.ToString("D")),
                    Builders<BsonDocument>.Update.Set("MetaData", new BsonDocument("code", new BsonBinaryData(item.Item2,
                        item.Item3 ? GuidRepresentation.CSharpLegacy : GuidRepresentation.Standard))));
                fixture.KnownOutcome = changed.IsAcknowledged; Assert.Equal(1, changed.ModifiedCount);
            }
            var ids = new[] { fixture.Root.Id, child.Id };
            var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
            var min = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<Guid>(h, "code")).Result));
            var max = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Max(h => CosmicQueryMetadata.Value<Guid>(h, "code")).Result));
            Assert.False(min.IsError, min.Message + min.Exception); Assert.Equal(low, min.Result);
            Assert.False(max.IsError, max.Message + max.Exception); Assert.Equal(high, max.Result);
        });

    [Fact]
    public Task NativeMinMaxRejectsAmbiguousUtcPrecisionCompanions()
    {
        var instant = new DateTime(638500000000000007L, DateTimeKind.Utc);
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id; fixture.KnownOutcome = false;
            var changed = await fixture.Records.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("HolonId", id.ToString("D")),
                Builders<BsonDocument>.Update.Set("MetaDataExactUtcTicks", new BsonArray
                { new BsonDocument { { "Path", "/instant" }, { "Ticks", instant.Ticks } },
                  new BsonDocument { { "Path", "/instant" }, { "Ticks", instant.Ticks + 1 } } }));
            fixture.KnownOutcome = changed.IsAcknowledged; Assert.Equal(1, changed.ModifiedCount);
            var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<DateTime>(h, "instant")).Result));
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.False(result.IsLoaded);
        }, configure: root => root.MetaData["instant"] = instant);
    }

    [Fact]
    public Task NativeMinMaxRejectsDuplicateMetadataInsteadOfChoosingFirst()
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id; fixture.KnownOutcome = false;
            var changed = await fixture.Records.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("HolonId", id.ToString("D")),
                Builders<BsonDocument>.Update.Set("MetaData", new BsonArray { new BsonArray { "score", 1L }, new BsonArray { "score", 2L } }));
            fixture.KnownOutcome = changed.IsAcknowledged; Assert.Equal(1, changed.ModifiedCount);
            var source = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Result;
            var result = await fixture.Provider.ExecuteCosmicScalarAsync(MinMaxPlan(source.Min(h => CosmicQueryMetadata.Value<long>(h, "score")).Result));
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.False(result.IsLoaded);
        });
}
