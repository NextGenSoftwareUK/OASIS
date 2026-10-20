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
        var capability = overbroad ? new CosmicQueryCapabilities(features, new[] { typeof(string), typeof(int), typeof(bool), typeof(Guid), typeof(HolonType), typeof(DateTime) },
            System.Enum.GetValues<CosmicQueryComparisonOperator>()) : SQLLiteDBOASIS.CqlCapabilities;
        var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.SQLLiteDBOASIS, new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, capability) });
        plan.IsError.Should().BeFalse(plan.Message); return plan.Result;
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
            h => CosmicQueryMetadata.Value<int>(h, "score") > 2
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
        var metadata = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => CosmicQueryMetadata.Value<int>(h, "score") > 2, overbroad: true));
        metadata.IsError.Should().BeTrue(); metadata.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
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
