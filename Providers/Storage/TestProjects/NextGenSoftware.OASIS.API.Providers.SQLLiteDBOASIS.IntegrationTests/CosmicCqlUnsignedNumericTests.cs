using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(4294967295UL)]
    [InlineData(4294967296UL)]
    [InlineData(9007199254740993UL)]
    [InlineData(9223372036854775808UL)]
    [InlineData(ulong.MaxValue)]
    public async Task NativeUnsignedScalarRetainsEveryBoundaryWithoutHydration(ulong value)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, value);
        var source = CosmicQuery<ScalarMustNotConstruct>.Create().Where(h => ids.Contains(h.Id)).Result;

        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "value")).Result));
        sum.IsError.Should().BeFalse(sum.Message + sum.Exception);
        sum.Result.Should().Be(value);

        var mean = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "value")).Result));
        mean.IsError.Should().BeFalse(mean.Message + mean.Exception);
        mean.Result.Should().Be((decimal)value);
    }

    [Fact]
    public async Task NativeUnsignedOverflowIsDistinctFromAnExactAverage()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, ulong.MaxValue, ulong.MaxValue);
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;

        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "value")).Result));
        sum.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW");
        sum.IsLoaded.Should().BeFalse();
        var mean = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "value")).Result));
        mean.IsError.Should().BeFalse(mean.Message + mean.Exception);
        mean.Result.Should().Be((decimal)ulong.MaxValue);
    }

    [Fact]
    public async Task NativeUnsignedNullableAndEmptyWindowsKeepTheirContracts()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, null!, 9223372036854775808UL);
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var mean = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "value")).Result));
        mean.IsError.Should().BeFalse(mean.Message + mean.Exception);
        mean.Result.Should().Be(9223372036854775808m);

        var empty = source.Take(0).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            empty.Sum(h => CosmicQueryMetadata.Value<ulong?>(h, "value")).Result));
        sum.IsError.Should().BeFalse(sum.Message);
        sum.Result.Should().Be(0UL);
        var optional = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            empty.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "value")).Result));
        optional.IsError.Should().BeFalse(optional.Message);
        optional.Result.Should().BeNull();
        var required = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            empty.Average(h => CosmicQueryMetadata.Value<ulong>(h, "value")).Result));
        required.ErrorCode.Should().Be("CQL_EMPTY_SEQUENCE");
    }

    [Fact]
    public async Task NativeUnsignedSourceWindowExcludesLaterMalformedValues()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, 9223372036854775808UL, 1UL, "invalid after window");
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).OrderBy(h => h.Name).Take(2).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "value")).Result));
        sum.IsError.Should().BeFalse(sum.Message + sum.Exception);
        sum.Result.Should().Be(9223372036854775809UL);
        var mean = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Average(h => CosmicQueryMetadata.Value<ulong>(h, "value")).Result));
        mean.IsError.Should().BeFalse(mean.Message + mean.Exception);
        mean.Result.Should().Be(4611686018427387904.5m);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(1.5)]
    [InlineData("18446744073709551615")]
    [InlineData("not numeric")]
    public async Task NativeUnsignedRejectsWrongParticipatingDomains(object invalid)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedNumeric(provider, 1UL, invalid);
        var source = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id)).Result;
        var sum = await provider.ExecuteCosmicScalarAsync(ScalarPlan(provider,
            source.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "value")).Result));
        sum.ErrorCode.Should().Be("CQL_DATA_INVALID");
        sum.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task NativeUnsignedGroupedSumsAndMeansFilterOrderAndPageExactly()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", ulong.MaxValue - 1), ("a", 1UL),
            ("b", ulong.MaxValue - 2), ("b", 1UL));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<ulong>(h, "stock")) }).Result
            .Where(g => g.Total >= ulong.MaxValue - 1 && g.Mean >= 9223372036854775807m)
            .OrderByDescending(g => g.Mean).ThenBy(g => g.Key).Skip(1).Take(1).Result;

        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        row.Key.Should().Be("b");
        row.Total.Should().Be(ulong.MaxValue - 1);
        row.Mean.Should().Be(9223372036854775807m);
    }

    [Fact]
    public async Task NativeUnsignedGroupedSumComparesToAnUnsignedIdentity()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, (ulong.MaxValue, ulong.MaxValue), (ulong.MaxValue - 1, 1UL));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<ulong>(h, "category"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "stock")) }).Result
            .Where(g => g.Total == g.Key).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Total.Should().Be(ulong.MaxValue);
    }

    [Fact]
    public async Task NativeUnsignedGroupedOverflowCannotHideBehindAnEmptyResultPage()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("a", ulong.MaxValue), ("a", ulong.MaxValue));
        var groups = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category")).Result;
        var average = groups.Select(g => g.Average(h => CosmicQueryMetadata.Value<ulong>(h, "stock"))).Result;
        var good = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, average), new[] { provider });
        good.IsError.Should().BeFalse(good.Message + good.Exception);
        good.Result.Providers.Single().Rows.Single().Should().Be((decimal)ulong.MaxValue);

        var sum = groups.Select(g => g.Sum(h => CosmicQueryMetadata.Value<ulong>(h, "stock"))).Result.Take(0).Result;
        var failed = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, sum), new[] { provider });
        failed.ErrorCode.Should().Be("CQL_SCALAR_OVERFLOW");
        failed.Result.Should().BeNull();
    }

    [Fact]
    public async Task NativeUnsignedNullableGroupedAverageRetainsNullAndZero()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("empty", null), ("full", ulong.MaxValue));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new { g.Key, Total = g.Sum(h => CosmicQueryMetadata.Value<ulong?>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<ulong?>(h, "stock")) }).Result
            .Where(g => g.Mean == null && g.Total == 0UL).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        row.Key.Should().Be("empty");
        row.Total.Should().Be(0UL);
        row.Mean.Should().BeNull();
    }
}
