using System.Globalization;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("1.25")]
    [InlineData("0.0000000000000000000000000001")]
    [InlineData("79228162514264337593543950335")]
    [InlineData("-79228162514264337593543950335")]
    public async Task NativeDecimalNumericRetainsExactBoundaryWithoutHydration(string text)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var value = decimal.Parse(text, CultureInfo.InvariantCulture);
        var ids = await SeedNumeric(provider, value);
        var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id)).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result));
        sum.IsError.Should().BeFalse(sum.Message + sum.Exception);
        sum.Result.Should().Be(value);
        var mean = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Average(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result));
        mean.IsError.Should().BeFalse(mean.Message + mean.Exception);
        mean.Result.Should().Be(value);
    }

    [Fact]
    public async Task NativeDecimalNumericCancellationRetainsTheSmallestFraction()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, decimal.MaxValue, decimal.MaxValue, decimal.MinValue,
            decimal.MinValue, 0.0000000000000000000000000001m);
        var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id)).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result));
        sum.IsError.Should().BeFalse(sum.Message + sum.Exception);
        sum.Result.Should().Be(0.0000000000000000000000000001m);
    }

    [Fact]
    public async Task NativeDecimalNumericSumOverflowStillAllowsARepresentableMean()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, decimal.MaxValue, decimal.MaxValue);
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result));
        sum.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW");
        var mean = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Average(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result));
        mean.IsError.Should().BeFalse(mean.Message + mean.Exception);
        mean.Result.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public async Task NativeDecimalNumericNullableAndEmptyWindowsKeepTheirContracts()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, null!, 1.25m);
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var mean = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Average(h => CosmicQueryMetadata.Value<decimal?>(h, "value")).Result));
        mean.IsError.Should().BeFalse(mean.Message + mean.Exception);
        mean.Result.Should().Be(1.25m);
        var empty = source.Take(0).Result;
        var zero = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            empty.Sum(h => CosmicQueryMetadata.Value<decimal?>(h, "value")).Result));
        zero.IsError.Should().BeFalse(zero.Message + zero.Exception);
        zero.Result.Should().Be(0m);
        var absent = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            empty.Average(h => CosmicQueryMetadata.Value<decimal?>(h, "value")).Result));
        absent.IsError.Should().BeFalse(absent.Message + absent.Exception);
        absent.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeDecimalNumericSourceWindowExcludesMalformedLaterValues()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, 1.25m, "outside window");
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(1).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result));
        sum.IsError.Should().BeFalse(sum.Message + sum.Exception);
        sum.Result.Should().Be(1.25m);
    }

    [Theory]
    [InlineData("1.25")]
    [InlineData("not numeric")]
    [InlineData(true)]
    [InlineData(null)]
    public async Task NativeDecimalNumericRejectsInvalidRequiredInput(object value)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, value);
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var result = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result));
        result.ErrorCode.Should().Be("CQL_DATA_INVALID");
        result.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeDecimalNumericGroupedFiltersOrderAndPageUseTheRoundedResult()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("tiny", 0.0000000000000000000000000001m),
            ("tiny", 0.0000000000000000000000000002m), ("whole", 1m), ("whole", 2m));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "stock")) })
            .Where(g => g.Total >= 0.0000000000000000000000000003m && g.Mean >= 0.0000000000000000000000000002m)
            .OrderByDescending(g => g.Mean).ThenBy(g => g.Key).Skip(1).Take(1).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        row.Key.Should().Be("tiny");
        row.Total.Should().Be(0.0000000000000000000000000003m);
        row.Mean.Should().Be(0.0000000000000000000000000002m);
    }

    [Fact]
    public async Task NativeDecimalNumericGroupedOverflowCannotHideBehindAnEmptyPage()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("one", decimal.MaxValue), ("one", decimal.MaxValue));
        var groups = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"));
        var mean = groups.Select(g => g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "stock")))
            .Where(value => value == decimal.MaxValue).OrderBy(value => value).Result;
        var good = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, mean), new[] { provider });
        good.IsError.Should().BeFalse(good.Message + good.Exception);
        good.Result.Providers.Single().Rows.Single().Should().Be(decimal.MaxValue);
        var sum = groups.Select(g => g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "stock"))).Take(0).Result;
        var failed = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, sum), new[] { provider });
        failed.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW");
        failed.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeDecimalNumericNullableGroupsPublishZeroAndNull()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("empty", null), ("full", decimal.MaxValue));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<decimal?>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<decimal?>(h, "stock")) })
            .Where(g => g.Total == 0m && g.Mean == null).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        row.Key.Should().Be("empty"); row.Total.Should().Be(0m); row.Mean.Should().BeNull();
    }

    [Fact]
    public async Task NativeDecimalNumericSumCanCompareWithAnExactDecimalKey()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("one", decimal.MaxValue));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<decimal>(h, "stock"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "stock")) })
            .Where(g => g.Total == g.Key).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Total.Should().Be(decimal.MaxValue);
    }
    [Fact]
    public async Task NativeDecimalNumericAccessExcludesHiddenMalformedInputs()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var actor = Guid.NewGuid(); var foreign = Guid.NewGuid(); var ids = new List<Guid>();
        foreach (var item in new[] { (actor, false, (object)1.25m), (foreign, true, (object)2.5m), (foreign, false, (object)"hidden malformed") })
        {
            var row = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = item.Item1, IsPublic = item.Item2,
                MetaData = new() { ["category"] = "one", ["stock"] = item.Item3 } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
            ids.Add(row.Id);
        }
        var keys = ids.ToArray();
        var source = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .Where(h => keys.Contains(h.Id)).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "stock")).Result));
        sum.IsError.Should().BeFalse(sum.Message + sum.Exception); sum.Result.Should().Be(3.75m);
        var query = source.GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { Count = g.Count(), Total = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "stock")) }).Result;
        var grouped = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { provider });
        grouped.IsError.Should().BeFalse(grouped.Message + grouped.Exception);
        var result = grouped.Result.Providers.Single().Rows.Single();
        result.Count.Should().Be(2); result.Total.Should().Be(3.75m); result.Mean.Should().Be(1.875m);
    }

    [Fact]
    public async Task NativeDecimalNumericPreCancellationLeavesTheProviderReusable()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, 1.25m, 1.25m);
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var plan = ScalarPlan(provider, source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await provider.ExecuteCosmicScalarAsync(plan, cancellation.Token);
        cancelled.ErrorCode.Should().Be("CQL_QUERY_CANCELLED");
        var fresh = await provider.ExecuteCosmicScalarAsync(plan);
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception); fresh.Result.Should().Be(2.5m);
    }

    [Fact]
    public async Task NativeDecimalNumericInputIsIndependentOfGraphPublicationBudget()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        await SeedNumeric(provider, Enumerable.Repeat((object)0.0000000000000000000000000001m, 137).ToArray());
        var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maximumNodes: 1).Result;
        var source = CosmicQuery<ScalarMustNotConstruct>.Create().WithChildren(graph)
            .Where(h => h.Name.StartsWith("owned numeric ", StringComparison.Ordinal)).Result;
        var result = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Should().Be(137m * 0.0000000000000000000000000001m);
    }

    [Fact]
    public async Task NativeDecimalNumericGroupedSumFiltersTheFinalRoundedValue()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("one", decimal.MaxValue), ("one", -0.1m));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { Total = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "stock")) })
            .Where(g => g.Total == decimal.MaxValue).OrderBy(g => g.Total).Take(1).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Total.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public async Task NativeDecimalNumericRejectsUnrepresentableOriginalJsonInsteadOfRoundingInput()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var document = System.Text.Json.JsonDocument.Parse("1.00000000000000000000000000000000000000001");
        var ids = await SeedNumeric(provider, document.RootElement.Clone());
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var result = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "value")).Result));
        result.ErrorCode.Should().Be("CQL_DATA_INVALID"); result.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeDecimalNumericMixedPacketWidthsKeepEverySelectedColumnAligned()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = new List<Guid>();
        for (var index = 0; index < 2; index++)
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = new()
                { ["category"] = "one", ["stock"] = 7L, ["price"] = 1.25m } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
            ids.Add(row.Id);
        }
        var keys = ids.ToArray();
        var query = CosmicQuery<Holon>.Create().Where(h => keys.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new
            {
                g.Key,
                IntegerTotal = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                DecimalTotal = g.Sum(h => CosmicQueryMetadata.Value<decimal>(h, "price")),
                IntegerMean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                DecimalMean = g.Average(h => CosmicQueryMetadata.Value<decimal>(h, "price")),
                Minimum = g.Min(h => CosmicQueryMetadata.Value<decimal>(h, "price"))
            }).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var selected = result.Result.Providers.Single().Rows.Single();
        selected.Key.Should().Be("one"); selected.IntegerTotal.Should().Be(14L);
        selected.DecimalTotal.Should().Be(2.5m); selected.IntegerMean.Should().Be(7m);
        selected.DecimalMean.Should().Be(1.25m); selected.Minimum.Should().Be(1.25m);
    }

}
