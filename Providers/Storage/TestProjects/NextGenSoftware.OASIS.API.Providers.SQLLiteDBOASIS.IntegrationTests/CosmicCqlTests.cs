using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private static CosmicQueryPlan<Holon> CqlPlan(Expression<Func<Holon, bool>> predicate, bool overbroad = false)
    {
        var scope = CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result;
        var query = CosmicQuery<Holon>.Create(scope).Where(predicate);
        query.IsError.Should().BeFalse(query.Message);
        var features = CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not |
            CosmicQueryFeatures.Metadata | CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.BooleanConstant;
        var capability = overbroad ? new CosmicQueryCapabilities(features, new[] { typeof(string), typeof(int), typeof(bool), typeof(Guid), typeof(HolonType), typeof(KarmaSourceType), typeof(DateTime) },
            System.Enum.GetValues<CosmicQueryComparisonOperator>()) : SQLLiteDBOASIS.CqlCapabilities;
        var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.SQLLiteDBOASIS, new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, capability) });
        plan.IsError.Should().BeFalse(plan.Message); return plan.Result;
    }

    private static CosmicQueryPlan<InvalidMetadataRow> NativeTypedPlan(ICosmicQueryStorageProvider adapter)
        => CosmicQueryPlanner.Plan(CosmicQuery<InvalidMetadataRow>.Create(), adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) }).Result;

    [Fact]
    public async Task TypedCqlNativeHydrationIsCleanAndNoOpSaveDoesNotAdvanceRevision()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row = new Holon { Id = Guid.NewGuid(), Name = "typed-cql", MetaData = new() { ["Rating"] = 7, ["Name"] = "forged" } };
        (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        var before = (await provider.LoadHolonAsync(row.Id, loadChildren: false)).Result;
        var manager = CosmicManager(provider, true); ICosmicQueryStorageProvider adapter = provider;
        var result = await manager.ExecuteQueryAsync(NativeTypedPlan(adapter), new[] { adapter });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var typed = result.Result.Providers.Single().Rows.Single(); typed.Rating.Should().Be(7); typed.Name.Should().Be("typed-cql");
        HolonChangeTracking.HasChanges(typed).Should().BeFalse();
        var noOp = await manager.SaveHolonAsync(typed, Guid.NewGuid(), saveChildren: false, providerType: ProviderType.SQLLiteDBOASIS);
        noOp.IsError.Should().BeFalse(noOp.Message + noOp.Exception); noOp.IsSaved.Should().BeFalse();
        var after = (await provider.LoadHolonAsync(row.Id, loadChildren: false)).Result;
        after.Version.Should().Be(before.Version);
        CosmicConcurrency.GetProviderRevisionId(after, ProviderType.SQLLiteDBOASIS).Should().Be(CosmicConcurrency.GetProviderRevisionId(before, ProviderType.SQLLiteDBOASIS));
    }

    [Fact]
    public async Task TypedCqlInvalidMetadataDoesNotPublishEarlierValidRows()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var values = new object[] { 7, "invalid-integer" };
        for (var index = 0; index < values.Length; index++)
            (await provider.SaveHolonAsync(new Holon { Id = Guid.Parse("00000000-0000-0000-0000-" + (index + 1).ToString("D12")),
                MetaData = new() { ["Rating"] = values[index] } }, saveChildren: false)).IsError.Should().BeFalse();
        ICosmicQueryStorageProvider adapter = provider;
        var result = await CosmicManager(provider, true).ExecuteQueryAsync(NativeTypedPlan(adapter), new[] { adapter });
        AssertTypedMetadataFailure(result);
    }

    [Fact]
    public async Task TypedCqlCancellationIsExplicitBeforeOpeningDatabase()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); ICosmicQueryStorageProvider adapter = provider;
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        // The explicit adapter is supplied at execution; avoid the existing injected-provider constructor's activation side effect.
        var manager = CosmicManager(null!, true);
        var result = await manager.ExecuteQueryAsync(NativeTypedPlan(adapter), new[] { adapter }, cancellationToken: cancellation.Token);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); result.Result.Should().BeNull();
        File.Exists(_databasePath).Should().BeFalse();
    }

    public sealed class InvalidMetadataRow : Holon
    { [NextGenSoftware.OASIS.API.Core.CustomAttrbiutes.CustomOASISProperty] public int Rating { get; set; } }

    private static void AssertTypedMetadataFailure<TValue>(NextGenSoftware.OASIS.Common.OASISResult<TValue> result)
    {
        result.IsError.Should().BeTrue(); result.IsLoaded.Should().BeFalse(); result.Result.Should().BeNull();
        result.ErrorCode.Should().Be("COSMIC_METADATA_HYDRATION_FAILED", result.Message + result.Exception); result.Exception.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0, false)] [InlineData(0, true)] [InlineData(1, false)] [InlineData(1, true)]
    [InlineData(2, false)] [InlineData(2, true)] [InlineData(3, false)] [InlineData(3, true)]
    public async Task V2TypedQueriesReturnMetadataErrorsWithoutDefaultOrPartialResults(int route, bool asynchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row = new Holon { Id = Guid.NewGuid(), Name = "conversion-error", MetaData = new() { ["Rating"] = "invalid-integer" } };
        var saved = await provider.SaveHolonAsync(row, saveChildren: false); saved.IsError.Should().BeFalse(saved.Message);
        var manager = CosmicManager(provider, true);
        if (route < 2)
        {
            var key = saved.Result.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS];
            var result = route == 0 ? asynchronous ? await manager.LoadHolonAsync<InvalidMetadataRow>(row.Id, loadChildren: false)
                : manager.LoadHolon<InvalidMetadataRow>(row.Id, loadChildren: false)
                : asynchronous ? await manager.LoadHolonAsync<InvalidMetadataRow>(key, loadChildren: false)
                : manager.LoadHolon<InvalidMetadataRow>(key, loadChildren: false);
            AssertTypedMetadataFailure(result);
        }
        else
        {
            var result = route == 2 ? asynchronous ? await manager.LoadAllHolonsAsync<InvalidMetadataRow>(loadChildren: false)
                : manager.LoadAllHolons<InvalidMetadataRow>(loadChildren: false, cache: false)
                : asynchronous ? await manager.SearchHolonsAsync<InvalidMetadataRow>("conversion-error", Guid.Empty, searchOnlyForCurrentAvatar: false, loadChildren: false, cache: false)
                : manager.SearchHolons<InvalidMetadataRow>("conversion-error", Guid.Empty, searchOnlyForCurrentAvatar: false, loadChildren: false, cache: false);
            AssertTypedMetadataFailure(result);
        }
        var native = await provider.LoadHolonAsync(row.Id, loadChildren: false);
        native.IsError.Should().BeFalse(native.Message); native.Result.MetaData["Rating"].ToString().Should().Be("invalid-integer");
    }

    [Fact]
    public async Task V2TypedLoadPreservesCanonicalIdentityAndValuesAgainstMetadataOverrides()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var id = Guid.NewGuid(); var actor = Guid.NewGuid();
        var row = new Holon { Id = id, Name = "canonical", CreatedByAvatarId = actor, IsPublic = false,
            MetaData = new() { ["Id"] = Guid.NewGuid().ToString(), ["Name"] = "forged", ["IsPublic"] = true, ["CreatedByAvatarId"] = Guid.NewGuid().ToString() } };
        (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        var result = CosmicManager(provider, true).LoadHolon<Holon>(id, loadChildren: false);
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Id.Should().Be(id); result.Result.Name.Should().Be("canonical");
        result.Result.CreatedByAvatarId.Should().Be(actor); result.Result.IsPublic.Should().BeFalse(); result.Result.IsChanged.Should().BeFalse();
    }

    [Fact]
    public async Task CosmicCqlSharedExecutorUsesTheNativeSQLiteContract()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var row = new Holon { Id = Guid.NewGuid(), Name = "shared-executor", MetaData = new() { ["tag"] = null! } };
        (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        ICosmicQueryStorageProvider adapter = provider;
        adapter.QueryProviderType.Should().Be(ProviderType.SQLLiteDBOASIS);
        var query = CosmicQuery<Holon>.Create().Where(h => h.Name == "shared-executor" && CosmicQueryMetadata.IsNull(h, "tag")).Result;
        var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType, new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        var result = await CosmicQueryExecutor.ExecuteAsync(plan.Result, new[] { adapter });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.TotalRows.Should().Be(1); result.Result.Providers.Single().Rows.Single().Id.Should().Be(row.Id);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public async Task CosmicCqlMetadataPresenceKeepsMissingNullTypesAndLiteralKeysDistinct(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var key = "a.b\"'; --";
        var values = new[] {
            new Holon { Id = Guid.NewGuid(), Name = "missing", MetaData = new() },
            new Holon { Id = Guid.NewGuid(), Name = "explicit-null", MetaData = new() { ["status"] = null! } },
            new Holon { Id = Guid.NewGuid(), Name = "text-null", MetaData = new() { ["status"] = "null", ["nested"] = new Dictionary<string, object> { [key] = null! } } },
            new Holon { Id = Guid.NewGuid(), Name = "numeric", MetaData = new() { ["status"] = 0, ["nested"] = new[] { new Dictionary<string, object> { ["target"] = null! } } } }
        };
        foreach (var value in values) (await provider.SaveHolonAsync(value, saveChildren: false)).IsError.Should().BeFalse();
        Expression<Func<Holon, bool>> predicate = selection switch {
            0 => h => CosmicQueryMetadata.Exists(h, "status"),
            1 => h => CosmicQueryMetadata.IsNull(h, "status"),
            2 => h => !CosmicQueryMetadata.Exists(h, "status"),
            3 => h => CosmicQueryMetadata.Exists(h, "nested", key),
            4 => h => CosmicQueryMetadata.IsNull(h, "nested", key),
            5 => h => CosmicQueryMetadata.Exists(h, "nested", "target"),
            _ => h => CosmicQueryMetadata.Exists(h, "status") && !CosmicQueryMetadata.IsNull(h, "status")
        };
        var expected = selection switch {
            0 => new[] { "explicit-null", "numeric", "text-null" }, 1 => new[] { "explicit-null" },
            2 => new[] { "missing" }, 3 or 4 => new[] { "text-null" }, 5 => Array.Empty<string>(), _ => new[] { "numeric", "text-null" }
        };
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Name).OrderBy(n => n).Should().Equal(expected.OrderBy(n => n));
    }

    [Fact]
    public async Task CosmicCqlMetadataParameterBudgetRejectsBeforeOpeningDatabase()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var query = CosmicQuery<Holon>.Create();
        for (var i = 0; i < 32; i++)
            query = query.Where(h => CosmicQueryMetadata.Exists(h, "a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p")
                || CosmicQueryMetadata.IsNull(h, "a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p")).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); File.Exists(_databasePath).Should().BeFalse();
    }

    [Fact]
    public void CosmicCqlProductionCapabilitiesRejectUnmappedFieldsBeforeExecution()
    {
        var profile = SQLLiteDBOASIS.CqlCapabilities;
        profile.RestrictsCanonicalFields.Should().BeTrue();
        var unsupported = new Expression<Func<Holon, bool>>[] {
            h => h.CustomKey == "key", h => h.IsDeleted,
            h => CosmicQueryMetadata.Value<decimal>(h, "score") > 2m
        };
        foreach (var predicate in unsupported)
        {
            var query = CosmicQuery<Holon>.Create().Where(predicate).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
                new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, profile) });
            plan.IsError.Should().BeTrue(); plan.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED"); plan.Result.Should().BeNull();
        }
        File.Exists(_databasePath).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task CosmicCqlNativeResultsMatchClrPredicates(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var values = new[] {
            new Holon { Id = Guid.NewGuid(), Name = null!, Description = "empty", HolonType = HolonType.Planet, Version = 1, IsActive = true },
            new Holon { Id = Guid.NewGuid(), Name = "Coffee", Description = null!, HolonType = HolonType.Planet, Version = 2, IsActive = false },
            new Holon { Id = Guid.NewGuid(), Name = "coffee", Description = "x'; DROP TABLE Holon; --", HolonType = HolonType.Quest, Version = 3, IsActive = true }
        };
        foreach (var value in values) (await provider.SaveHolonAsync(value, saveChildren: false)).IsError.Should().BeFalse();
        // Compare against independently materialized CLR values, including provider-owned save version semantics.
        var persisted = new List<IHolon>();
        foreach (var value in values) persisted.Add((await provider.LoadHolonAsync(value.Id, loadChildren: false)).Result);
        var selectedId = values[1].Id; var parentId = Guid.Empty;
        Expression<Func<Holon, bool>> predicate = selection switch {
            0 => h => h.Name != "Coffee" && !(h.Description == "empty"),
            1 => h => h.Name == "Coffee" || h.Description == "x'; DROP TABLE Holon; --",
            2 => h => h.Version >= 2 && h.HolonType != HolonType.Quest,
            3 => h => h.Id == selectedId || (h.ParentHolonId == parentId && h.IsActive),
            _ => h => h.Name == null || !h.IsActive
        };
        var expected = persisted.Cast<Holon>().Where(predicate.Compile()).Select(h => h.Id).OrderBy(id => id).ToArray();
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Id).OrderBy(id => id).Should().Equal(expected);
        (await provider.LoadHolonAsync(values[2].Id, loadChildren: false)).Result.Description.Should().Be("x'; DROP TABLE Holon; --");
    }

    [Fact]
    public async Task CosmicCqlUnsupportedPathsRejectBeforeOpeningDatabase()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var metadata = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => CosmicQueryMetadata.Value<KarmaSourceType>(h, "score") == KarmaSourceType.Game, overbroad: true));
        metadata.IsError.Should().BeTrue(); metadata.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
        var date = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => h.CreatedDate > cutoff, overbroad: true));
        date.IsError.Should().BeTrue(); date.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
        File.Exists(_databasePath).Should().BeFalse();
    }

    [Fact]
    public async Task CosmicCqlEnforcesResultBudgetAndCancellationWithoutPartialSuccess()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        for (var i = 0; i < 2; i++) (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "budget" }, saveChildren: false)).IsError.Should().BeFalse();
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => h.Name == "budget"), maximumResults: 1);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); result.Result.Should().BeNull();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => true), cancellationToken: cancellation.Token);
        cancelled.IsError.Should().BeTrue(); cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED"); cancelled.Result.Should().BeNull();
    }

    [Fact]
    public async Task CosmicCqlRejectsForeignPlanAndInvalidLimitBeforeOpeningDatabase()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        var foreign = CosmicQueryPlanner.Plan(query, ProviderType.None, new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS,
            new CosmicQueryCapabilities(CosmicQueryFeatures.None, Array.Empty<Type>(), Array.Empty<CosmicQueryComparisonOperator>())) }).Result;
        var result = await provider.ExecuteCosmicQueryAsync(foreign);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_PLAN_INVALID");
        var limit = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => true), maximumResults: 0);
        limit.IsError.Should().BeTrue(); limit.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); File.Exists(_databasePath).Should().BeFalse();
    }
}
