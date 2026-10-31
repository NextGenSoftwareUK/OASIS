using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public async Task NativeUInt64PredicatesPreserveFullDomain(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object?[] values = { null, 0UL, 9007199254740993UL, (ulong)long.MaxValue,
            (ulong)long.MaxValue + 1UL, ulong.MaxValue - 1UL, ulong.MaxValue,
            -1L, 1.0m, 18446744073709551616m, "18446744073709551615" };
        var ids = new List<Guid>();
        foreach (var value in values)
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["counter"] = value! } };
            var saved = await provider.SaveHolonAsync(row, saveChildren: false);
            saved.IsError.Should().BeFalse(saved.Message + saved.Exception);
            ids.Add(row.Id);
        }
        var members = new[] { ulong.MaxValue, (ulong)long.MaxValue + 1UL };
        Expression<Func<Holon, bool>> predicate = selection switch
        {
            0 => row => CosmicQueryMetadata.Value<ulong>(row, "counter") == ulong.MaxValue,
            1 => row => CosmicQueryMetadata.Value<ulong>(row, "counter") > (ulong)long.MaxValue,
            2 => row => CosmicQueryMetadata.Value<ulong>(row, "counter") < 1UL,
            3 => row => members.Contains(CosmicQueryMetadata.Value<ulong>(row, "counter")),
            4 => row => CosmicQueryMetadata.Value<ulong?>(row, "counter") == null,
            5 => row => CosmicQueryMetadata.Value<ulong?>(row, "counter") != ulong.MaxValue,
            6 => row => !(CosmicQueryMetadata.Value<ulong>(row, "counter") == ulong.MaxValue),
            _ => row => CosmicQueryMetadata.Value<ulong>(row, "counter") >= ulong.MaxValue - 1UL
        };
        int[] expected = selection switch
        {
            0 => new[] { 6 }, 1 => new[] { 4, 5, 6 }, 2 => new[] { 1 },
            3 => new[] { 4, 6 }, 4 => new[] { 0 }, 5 => new[] { 0, 1, 2, 3, 4, 5 },
            6 => Enumerable.Range(0, values.Length).Except(new[] { 6 }).ToArray(),
            _ => new[] { 5, 6 }
        };
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(row => row.Id).Should().BeEquivalentTo(expected.Select(index => ids[index]));
    }

    [Theory]
    [InlineData("counter")] [InlineData("a.b")] [InlineData("quote\"key")] [InlineData("nested")]
    public async Task NativeUInt64OrderProjectionAndGroupedKeysRetainAdjacentMaxima(string key)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        Expression<Func<ScalarMustNotConstruct, ulong>> selector = key == "nested"
            ? row => CosmicQueryMetadata.Value<ulong>(row, "outer", "counter")
            : row => CosmicQueryMetadata.Value<ulong>(row, key);
        foreach (var value in new[] { ulong.MaxValue, 0UL, ulong.MaxValue - 1UL })
            (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), MetaData = key == "nested"
                ? new() { ["outer"] = new Dictionary<string, object> { ["counter"] = value } }
                : new() { [key] = value } },
                saveChildren: false)).IsError.Should().BeFalse();

        var selected = CosmicQuery<ScalarMustNotConstruct>.Create()
            .OrderBy(selector)
            .Select(selector).Result;
        var projected = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, selected),
            new ICosmicQueryStorageProvider[] { provider });
        projected.IsError.Should().BeFalse(projected.Message + projected.Exception);
        projected.Result.Providers.Single().Rows.Should().Equal(0UL, ulong.MaxValue - 1UL, ulong.MaxValue);

        var grouped = CosmicQuery<ScalarMustNotConstruct>.Create()
            .GroupBy(selector)
            .Select(group => new { Value = group.Key, Items = group.LongCount() })
            .Where(row => row.Value >= ulong.MaxValue - 1UL)
            .OrderBy(row => row.Value).Result;
        var groups = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, grouped), new[] { (ICosmicQueryStorageProvider)provider });
        groups.IsError.Should().BeFalse(groups.Message + groups.Exception);
        groups.Result.Providers.Single().Rows.Select(row => row.Value).Should().Equal(ulong.MaxValue - 1UL, ulong.MaxValue);
    }
    [Theory]
    [InlineData("-1")] [InlineData("18446744073709551616")]
    [InlineData("1.0")] [InlineData("\"18446744073709551615\"")]
    public async Task NativeUInt64ProjectionRejectsWrongDomainInsteadOfCoercing(string json)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["counter"] = document.RootElement.Clone() } };
        (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
        var selected = CosmicQuery<ScalarMustNotConstruct>.Create()
            .Select(h => CosmicQueryMetadata.Value<ulong>(h, "counter")).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, selected),
            new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("CQL_DATA_INVALID");
    }

    [Theory]
    [InlineData("0.5")] [InlineData("18446744073709551616")] [InlineData("-1")]
    public async Task NativeUInt64MixedLiteralCannotRoundOrOverflowIntoItsDomain(string literal)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var value = decimal.Parse(literal, System.Globalization.CultureInfo.InvariantCulture);
        var query = CosmicQuery<Holon>.Create()
            .Where(row => (decimal)CosmicQueryMetadata.Value<ulong>(row, "counter") == value).Result;
        var adapter = (ICosmicQueryStorageProvider)provider;
        var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType,
            new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
    }

    [Fact]
    public async Task NativeUInt64FieldComparisonsRemainExactAcrossInt64Boundary()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var matches = new List<Guid>();
        foreach (var pair in new[] { (ulong.MaxValue, ulong.MaxValue - 1UL),
            ((ulong)long.MaxValue, (ulong)long.MaxValue + 1UL), (0UL, 0UL) })
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["a"] = pair.Item1, ["b"] = pair.Item2 } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
            if (pair.Item1 > pair.Item2) matches.Add(row.Id);
        }
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(row =>
            CosmicQueryMetadata.Value<ulong>(row, "a") > CosmicQueryMetadata.Value<ulong>(row, "b")));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(row => row.Id).Should().BeEquivalentTo(matches);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeUInt64ExtremaDecodeExactUnsignedResults(bool nullable)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var value in new ulong?[] { nullable ? null : 0UL, ulong.MaxValue - 1UL, ulong.MaxValue })
            (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), MetaData = new() { ["counter"] = value! } },
                saveChildren: false)).IsError.Should().BeFalse();
        var source = CosmicQuery<ScalarMustNotConstruct>.Create();
        if (nullable)
        {
            var min = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider,
                source.Min(row => CosmicQueryMetadata.Value<ulong?>(row, "counter")).Result));
            var max = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider,
                source.Max(row => CosmicQueryMetadata.Value<ulong?>(row, "counter")).Result));
            min.IsError.Should().BeFalse(min.Message + min.Exception);
            max.IsError.Should().BeFalse(max.Message + max.Exception);
            min.Result.Should().Be(ulong.MaxValue - 1UL);
            max.Result.Should().Be(ulong.MaxValue);
        }
        else
        {
            var min = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider,
                source.Min(row => CosmicQueryMetadata.Value<ulong>(row, "counter")).Result));
            var max = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider,
                source.Max(row => CosmicQueryMetadata.Value<ulong>(row, "counter")).Result));
            min.IsError.Should().BeFalse(min.Message + min.Exception);
            max.IsError.Should().BeFalse(max.Message + max.Exception);
            min.Result.Should().Be(0UL);
            max.Result.Should().Be(ulong.MaxValue);
        }
    }

}
