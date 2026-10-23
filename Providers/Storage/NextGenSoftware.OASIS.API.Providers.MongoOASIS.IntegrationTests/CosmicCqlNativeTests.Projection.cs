using System;
using System.Linq;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    private static CosmicProjectionPlan<T, TResult> ProjectionPlan<T, TResult>(Provider provider,
        CosmicProjectionQuery<T, TResult> query) where T : IHolon
    {
        var adapter = (ICosmicQueryProjectionStorageProvider)provider;
        var plan = CosmicProjectionPlanner.Plan(query, ProviderType.MongoDBOASIS, new[]
        { new CosmicProjectionTarget(ProviderType.MongoDBOASIS, adapter.QueryCapabilities, adapter.ProjectionCapabilities) });
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    [Fact]
    public Task NativeProjectionPagesColumnsWithoutHydratingEntitiesOrUnselectedMetadata()
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => h.Id == id)
                .OrderBy(h => h.Name).Take(1).Select(h => new { h.Name, Stock = CosmicQueryMetadata.Value<long>(h, "stock") }).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query),
                new ICosmicQueryStorageProvider[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal("owned CQL graph root", result.Result.Providers.Single().Rows.Single().Name);
            Assert.Equal(9007199254740993L, result.Result.Providers.Single().Rows.Single().Stock);
        }, configure: root => { root.MetaData["stock"] = 9007199254740993L; root.MetaData["Rating"] = "invalid unselected integer"; });

    [Fact]
    public Task NativeProjectionRetainsExactGuidUtcTicksBooleanAndNullDomains()
    {
        var code = Guid.NewGuid(); var instant = new DateTime(638500000000000007L, DateTimeKind.Utc);
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => new
            {
                Code = CosmicQueryMetadata.Value<Guid>(h, "code"),
                Instant = CosmicQueryMetadata.Value<DateTime>(h, "instant"),
                Enabled = CosmicQueryMetadata.Value<bool>(h, "enabled"),
                Missing = CosmicQueryMetadata.Value<long?>(h, "missing"),
                Nothing = CosmicQueryMetadata.Value<long?>(h, "nothing")
            }).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.Equal(code, row.Code); Assert.Equal(instant, row.Instant); Assert.Equal(DateTimeKind.Utc, row.Instant.Kind);
            Assert.True(row.Enabled); Assert.Null(row.Missing); Assert.Null(row.Nothing);
        }, configure: root => { root.MetaData["code"] = code; root.MetaData["instant"] = instant; root.MetaData["enabled"] = true; root.MetaData["nothing"] = null; });
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public Task NativeProjectionRejectsWrongAndMissingRequiredDomains(bool missing)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<long>(h, "stock")).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.True(result.IsError); Assert.False(result.IsLoaded); Assert.Null(result.Result);
        }, configure: root => { if (!missing) root.MetaData["stock"] = "7"; });

    [Fact]
    public Task NativeProjectionVisibilityAndOutputBudgetsApplyBeforePublication()
    {
        var actor = Guid.NewGuid();
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
                .Where(h => h.Id == id).Take(1).Select(h => h.Name).Result;
            var hidden = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.False(hidden.IsError, hidden.Message + hidden.Exception); Assert.Empty(hidden.Result.Providers.Single().Rows);
            var visible = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => h.Name).Result;
            var limited = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, visible), new[] { fixture.Provider }, maximumPayloadBytes: 2);
            Assert.True(limited.IsError); Assert.Null(limited.Result);
        }, actor: actor);
    }
    [Fact]
    public Task NativeProjectionGraphOutputCapDoesNotTruncate137Rows()
        => WithMongoGraph(async fixture =>
        {
            var query = CosmicQuery<ScalarMustNotConstruct>.Create().WithChildren(CosmicQueryGraphOptions.Create(
                CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result)
                .Where(h => h.Name.StartsWith("owned CQL graph", StringComparison.Ordinal)).Select(h => h.Name).Result;
            var plan = ProjectionPlan(fixture.Provider, query);
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(plan, new[] { fixture.Provider }, maximumResults: 200);
            Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(137, result.Result.Providers.Single().Rows.Count);
            var limited = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(plan, new[] { fixture.Provider }, maximumResults: 100);
            Assert.Equal("CQL_QUERY_LIMIT", limited.ErrorCode); Assert.Null(limited.Result);
        }, width: 136);

    [Fact]
    public Task NativeProjectionTraversalFailureCannotBeHiddenByPaging()
        => WithMongoGraph(async fixture =>
        {
            var query = DescendantPlan(fixture, 2, 1).Query.Take(1).Select(h => h.Name).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode); Assert.False(result.IsLoaded); Assert.Null(result.Result);
            var fresh = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider,
                DescendantPlan(fixture, 2, 3).Query.Select(h => h.Name).Result), new[] { fixture.Provider });
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
        });

    [Fact]
    public Task NativeProjectionMembershipAndSelectedCellsShareSnapshotDuringReparent()
        => WithMongoGraph(async fixture =>
        {
            var plan = ProjectionPlan(fixture.Provider, DescendantPlan(fixture, 2, 3).Query.Select(h => h.Name).Result);
            var child = await fixture.Provider.LoadHolonAsync(fixture.Root.Children.First().Id, loadChildren: false);
            Assert.False(child.IsError, child.Message);
            var admin = fixture.Client.GetDatabase("admin");
            var configured = await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            { { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip", 1) },
                { "data", new BsonDocument { { "failCommands", new BsonArray { "aggregate" } }, { "appName", "CosmicCqlGraph" },
                    { "blockConnection", true }, { "blockTimeMS", 5000 } } } });
            Task<NextGenSoftware.OASIS.Common.OASISResult<IReadOnlyList<CosmicProjectionRow>>>? pending = null;
            try
            {
                pending = ((ICosmicQueryProjectionStorageProvider)fixture.Provider).ExecuteCosmicProjectionAsync(plan);
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "waitForFailPoint", "failCommand" },
                    { "timesEntered", configured["count"].ToInt64() + 1 }, { "maxTimeMS", 10000 } });
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                child.Result.ParentHolonId = Guid.NewGuid(); fixture.KnownOutcome = false;
                var changed = await fixture.Provider.SaveHolonAsync(child.Result, saveChildren: false);
                fixture.KnownOutcome = !changed.IsError; Assert.False(changed.IsError, changed.Message + changed.Exception);
                var old = await pending;
                Assert.False(old.IsError, old.Message + old.Exception); Assert.Equal(fixture.Root.Name, old.Result.Single().Values.Single());
                var fresh = await ((ICosmicQueryProjectionStorageProvider)fixture.Provider).ExecuteCosmicProjectionAsync(plan);
                Assert.False(fresh.IsError, fresh.Message + fresh.Exception); Assert.Empty(fresh.Result);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }
        });

    [Fact]
    public Task NativeProjectionCancellationDuringActualAggregatePublishesNoRowsAndReusesOwner()
        => WithMongoGraph(async fixture =>
        {
            var plan = ProjectionPlan(fixture.Provider, DescendantPlan(fixture, 2, 3).Query.Select(h => h.Name).Result);
            var admin = fixture.Client.GetDatabase("admin");
            var configured = await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            { { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip", 1) },
                { "data", new BsonDocument { { "failCommands", new BsonArray { "aggregate" } }, { "appName", "CosmicCqlGraph" },
                    { "blockConnection", true }, { "blockTimeMS", 5000 } } } });
            using var cancellation = new CancellationTokenSource();
            Task<NextGenSoftware.OASIS.Common.OASISResult<IReadOnlyList<CosmicProjectionRow>>>? pending = null;
            try
            {
                pending = ((ICosmicQueryProjectionStorageProvider)fixture.Provider).ExecuteCosmicProjectionAsync(plan, cancellationToken: cancellation.Token);
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "waitForFailPoint", "failCommand" },
                    { "timesEntered", configured["count"].ToInt64() + 1 }, { "maxTimeMS", 10000 } });
                cancellation.Cancel(); var failed = await pending;
                Assert.Equal("CQL_QUERY_CANCELLED", failed.ErrorCode); Assert.Null(failed.Result); Assert.False(failed.IsLoaded);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }
            var fresh = await ((ICosmicQueryProjectionStorageProvider)fixture.Provider).ExecuteCosmicProjectionAsync(plan);
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
        });

    [Fact]
    public Task NativeProjectionNestedMetadataAndDuplicateDictionaryPathsAreExplicit()
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var filter = Builders<BsonDocument>.Filter.Eq("HolonId", id.ToString("D"));
            fixture.KnownOutcome = false;
            var changed = await fixture.Records.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("MetaData",
                new BsonDocument("location", new BsonDocument("city", "London"))));
            fixture.KnownOutcome = changed.IsAcknowledged; Assert.Equal(1, changed.ModifiedCount);
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<string>(h, "location", "city")).Result;
            var plan = ProjectionPlan(fixture.Provider, query);
            var good = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(plan, new[] { fixture.Provider });
            Assert.False(good.IsError, good.Message + good.Exception); Assert.Equal("London", good.Result.Providers.Single().Rows.Single());
            fixture.KnownOutcome = false;
            changed = await fixture.Records.UpdateOneAsync(filter, Builders<BsonDocument>.Update.Set("MetaData", new BsonArray
            { new BsonArray { "location", new BsonDocument("city", "London") }, new BsonArray { "location", new BsonDocument("city", "Paris") } }));
            fixture.KnownOutcome = changed.IsAcknowledged; Assert.Equal(1, changed.ModifiedCount);
            var ambiguous = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(plan, new[] { fixture.Provider });
            Assert.Equal("CQL_DATA_INVALID", ambiguous.ErrorCode); Assert.Null(ambiguous.Result);
        });

    [Fact]
    public Task NativeProjectionCanonicalColumnsRetainTheirExactClrDomains()
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => new
            { h.Id, h.Name, h.Description, h.ParentHolonId, h.VersionId, h.PreviousVersionId, h.Version, h.HolonType,
                h.CreatedByAvatarId, h.ModifiedByAvatarId, h.IsActive, h.IsPublic }).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception);
            var row = result.Result.Providers.Single().Rows.Single();
            Assert.Equal(id, row.Id); Assert.Equal(fixture.Root.Name, row.Name); Assert.Equal(fixture.Root.Description, row.Description);
            Assert.Equal(fixture.Root.ParentHolonId, row.ParentHolonId); Assert.Equal(fixture.Root.VersionId, row.VersionId);
            Assert.Equal(fixture.Root.PreviousVersionId, row.PreviousVersionId); Assert.Equal(fixture.Root.Version, row.Version);
            Assert.Equal(fixture.Root.HolonType, row.HolonType); Assert.Equal(fixture.Root.CreatedByAvatarId, row.CreatedByAvatarId);
            Assert.Equal(fixture.Root.ModifiedByAvatarId, row.ModifiedByAvatarId); Assert.Equal(fixture.Root.IsActive, row.IsActive); Assert.Equal(fixture.Root.IsPublic, row.IsPublic);
        });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task NativeProjectionGuidMetadataSupportsBothPersistedUuidRepresentations(bool legacy)
        => WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id; var code = Guid.NewGuid();
            fixture.KnownOutcome = false;
            var changed = await fixture.Records.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("HolonId", id.ToString("D")),
                Builders<BsonDocument>.Update.Set("MetaData", new BsonDocument("code", new BsonBinaryData(code,
                    legacy ? GuidRepresentation.CSharpLegacy : GuidRepresentation.Standard))));
            fixture.KnownOutcome = changed.IsAcknowledged; Assert.Equal(1, changed.ModifiedCount);
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<Guid>(h, "code")).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.False(result.IsError, result.Message + result.Exception); Assert.Equal(code, result.Result.Providers.Single().Rows.Single());
        });

    [Fact]
    public Task NativeProjectionEmptyWindowCancellationAndUnsupportedAdmissionAreBounded()
        => WithMongoGraph(async fixture =>
        {
            var adapter = (ICosmicQueryProjectionStorageProvider)fixture.Provider;
            var plan = ProjectionPlan(fixture.Provider, CosmicQuery<Holon>.Create().Take(0).Select(h => h.Name).Result);
            var empty = await adapter.ExecuteCosmicProjectionAsync(plan);
            Assert.False(empty.IsError, empty.Message); Assert.Empty(empty.Result);
            var cancelled = await adapter.ExecuteCosmicProjectionAsync(plan, cancellationToken: new CancellationToken(true));
            Assert.Equal("CQL_QUERY_CANCELLED", cancelled.ErrorCode);
            var source = CosmicQuery<Holon>.Create().Select(h => CosmicQueryMetadata.Value<decimal>(h, "unsupported")).Result;
            var borrowed = CosmicProjectionPlanner.Plan(source, ProviderType.MongoDBOASIS, new[] { new CosmicProjectionTarget(
                ProviderType.MongoDBOASIS, adapter.QueryCapabilities, new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(decimal) })) }).Result;
            var rejected = await adapter.ExecuteCosmicProjectionAsync(borrowed);
            Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", rejected.ErrorCode);
        }, width: 0);

    [Fact]
    public Task NativeProjectionRejectsAmbiguousUtcPrecisionCompanions()
    {
        var instant = new DateTime(638500000000000007L, DateTimeKind.Utc);
        return WithMongoGraph(async fixture =>
        {
            var id = fixture.Root.Id;
            fixture.KnownOutcome = false;
            var changed = await fixture.Records.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("HolonId", id.ToString("D")),
                Builders<BsonDocument>.Update.Set("MetaDataExactUtcTicks", new BsonArray
                { new BsonDocument { { "Path", "/instant" }, { "Ticks", instant.Ticks } },
                  new BsonDocument { { "Path", "/instant" }, { "Ticks", instant.Ticks + 1 } } }));
            fixture.KnownOutcome = changed.IsAcknowledged; Assert.Equal(1, changed.ModifiedCount);
            var query = CosmicQuery<Holon>.Create().Where(h => h.Id == id).Select(h => CosmicQueryMetadata.Value<DateTime>(h, "instant")).Result;
            var result = await Manager(fixture.Provider).ExecuteProjectionQueryAsync(ProjectionPlan(fixture.Provider, query), new[] { fixture.Provider });
            Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.Null(result.Result);
        }, configure: root => root.MetaData["instant"] = instant);
    }

}
