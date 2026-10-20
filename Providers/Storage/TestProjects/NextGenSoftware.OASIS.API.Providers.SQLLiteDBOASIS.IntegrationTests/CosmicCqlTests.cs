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
    private static CosmicQueryPlan<Holon> CqlPlan(Expression<Func<Holon, bool>> predicate)
    {
        var scope = CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result;
        var query = CosmicQuery<Holon>.Create(scope).Where(predicate);
        query.IsError.Should().BeFalse(query.Message);
        var features = CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not |
            CosmicQueryFeatures.Metadata | CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.BooleanConstant;
        var capability = new CosmicQueryCapabilities(features, new[] { typeof(string), typeof(int), typeof(bool), typeof(Guid), typeof(HolonType), typeof(DateTime) },
            System.Enum.GetValues<CosmicQueryComparisonOperator>());
        var plan = CosmicQueryPlanner.Plan(query.Result, ProviderType.SQLLiteDBOASIS, new[] { new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, capability) });
        plan.IsError.Should().BeFalse(plan.Message); return plan.Result;
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
        var metadata = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => CosmicQueryMetadata.Value<int>(h, "score") > 2));
        metadata.IsError.Should().BeTrue(); metadata.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
        var cutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var date = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => h.CreatedDate > cutoff));
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
