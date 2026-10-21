using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)]
    public async Task NativeCqlNullableMetadataUsesExplicitNullAndLiftedScalarSemantics(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object?[] values = { null, null, true, false, 1, long.MinValue, long.MaxValue, 2.5d, "1" };
        var ids = new List<Guid>();
        for (var index = 0; index < values.Length; index++)
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = index == 0 ? new() : new() { ["value"] = values[index]! } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        var flags = new bool?[] { null, true }; var numbers = new int?[] { null, 1 };
        Expression<Func<Holon, bool>> predicate = selection switch {
            0 => h => CosmicQueryMetadata.Value<long?>(h, "value") == null,
            1 => h => CosmicQueryMetadata.Value<long?>(h, "value") != null,
            2 => h => CosmicQueryMetadata.Value<long?>(h, "value") < 0L,
            3 => h => CosmicQueryMetadata.Value<long?>(h, "value") != 1L,
            4 => h => CosmicQueryMetadata.Value<bool?>(h, "value") == null,
            5 => h => CosmicQueryMetadata.Value<bool?>(h, "value") != null,
            6 => h => flags.Contains(CosmicQueryMetadata.Value<bool?>(h, "value")),
            7 => h => numbers.Contains(CosmicQueryMetadata.Value<int?>(h, "value")),
            _ => h => 1L <= CosmicQueryMetadata.Value<long?>(h, "value")
        };
        int[] selected = selection switch { 0 or 4 => new[] { 1 }, 1 => new[] { 4, 5, 6 }, 2 => new[] { 5 },
            3 => new[] { 1, 5, 6 }, 5 => new[] { 2, 3 }, 6 => new[] { 1, 2 }, 7 => new[] { 1, 4 }, _ => new[] { 4, 6 } };
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Id).OrderBy(id => id).Should().Equal(selected.Select(i => ids[i]).OrderBy(id => id));
    }

    [Fact]
    public async Task NativeCqlScalarMetadataDoesNotTraverseArraysOrStringEncodedObjects()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var nested = new Dictionary<string, object> { ["value"] = 2 };
        object?[] values = { nested, new[] { nested }, "{\"value\":2}", null, new Dictionary<string, object>() };
        var ids = new List<Guid>();
        foreach (var value in values)
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["nested"] = value! } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => CosmicQueryMetadata.Value<int>(h, "nested", "value") == 2));
        result.IsError.Should().BeFalse(result.Message + result.Exception); result.Result.Select(h => h.Id).Should().Equal(ids[0]);
    }

    [Fact]
    public async Task NativeCqlScalarMetadataHonorsTheWholeQueryParameterBudgetBeforeIO()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var values = Enumerable.Range(0, CosmicQueryExpression.MaximumMembershipValues).Select(i => "v" + i).ToArray();
        var query = CosmicQuery<Holon>.Create();
        for (var i = 0; i < 8; i++)
        {
            var next = query.Where(h => values.Contains(CosmicQueryMetadata.Value<string>(h, "value")));
            next.IsError.Should().BeFalse(next.Message); query = next.Result;
        }
        var plan = CosmicQueryPlanner.Plan(query, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicQueryTarget(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS, SQLLiteDBOASIS.CqlCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); File.Exists(_databasePath).Should().BeFalse();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(4)] [InlineData(5)]
    public async Task NativeCqlScalarMetadataRejectsUncertifiedTypesAndCoercionBeforeIO(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        Expression<Func<Holon, bool>> predicate = selection switch {
            0 => h => CosmicQueryMetadata.Value<decimal>(h, "x") == 1.000000000000000000000000001m,
            1 => h => CosmicQueryMetadata.Value<double>(h, "x") == 1.1d,
            4 => h => CosmicQueryMetadata.Value<NextGenSoftware.OASIS.API.Core.Enums.KarmaSourceType>(h, "x") == NextGenSoftware.OASIS.API.Core.Enums.KarmaSourceType.Game,
            _ => h => (decimal)CosmicQueryMetadata.Value<int>(h, "x") == 1.1m
        };
        var query = CosmicQuery<Holon>.Create().Where(predicate); query.IsError.Should().BeFalse(query.Message);
        SQLLiteDBOASIS.CqlCapabilities.Admit(query.Result).IsError.Should().BeTrue();
        var forged = new CosmicQueryCapabilities(CosmicQueryFeatures.Metadata | CosmicQueryFeatures.MixedNumericComparison,
            new[] { typeof(decimal), typeof(double), typeof(Guid), typeof(DateTime), typeof(NextGenSoftware.OASIS.API.Core.Enums.KarmaSourceType), typeof(int) },
            System.Enum.GetValues<CosmicQueryComparisonOperator>());
        var plan = CosmicQueryPlanner.Plan(query.Result, NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicQueryTarget(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.SQLLiteDBOASIS, forged) });
        plan.IsError.Should().BeFalse(plan.Message);
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED"); File.Exists(_databasePath).Should().BeFalse();
    }

    public static IEnumerable<object[]> ScalarMetadataSelections => Enumerable.Range(0, 26).Select(i => new object[] { i });

    [Theory]
    [MemberData(nameof(ScalarMetadataSelections))]
    public async Task NativeCqlScalarMetadataKeepsMissingNullAndJsonTypesDistinct(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var key = "score.\"'; --";
        object?[] values = { null, null, 1, 2, "2", true, false, 2.5d, long.MaxValue, (long)int.MaxValue + 1,
            new[] { 2 }, new Dictionary<string, object> { ["x"] = 2 }, "a\0b", "😀%_'; --", int.MinValue, int.MaxValue, long.MinValue, 2.5m, ulong.MaxValue };
        var ids = new List<Guid>();
        for (var index = 0; index < values.Length; index++)
        {
            var nested = index == 0 ? new Dictionary<string, object>() : new Dictionary<string, object> { [key] = values[index]! };
            var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["nested"] = nested } };
            var save = await provider.SaveHolonAsync(row, saveChildren: false); save.IsError.Should().BeFalse(save.Message + save.Exception); ids.Add(row.Id);
        }
        var numbers = new[] { 1, 2 }; var strings = new string?[] { null, "2" }; var empty = Array.Empty<string>();
        Expression<Func<Holon, bool>> predicate = selection switch {
            0 => h => CosmicQueryMetadata.Value<int>(h, "nested", key) > 1,
            1 => h => CosmicQueryMetadata.Value<int>(h, "nested", key) == 2,
            2 => h => CosmicQueryMetadata.Value<int>(h, "nested", key) != 2,
            3 => h => CosmicQueryMetadata.Value<int>(h, "nested", key) < 2,
            4 => h => CosmicQueryMetadata.Value<int>(h, "nested", key) >= 2,
            5 => h => CosmicQueryMetadata.Value<int>(h, "nested", key) <= 2,
            6 => h => CosmicQueryMetadata.Value<int?>(h, "nested", key) == null,
            7 => h => CosmicQueryMetadata.Value<int?>(h, "nested", key) != null,
            8 => h => CosmicQueryMetadata.Value<int?>(h, "nested", key) != 2,
            9 => h => CosmicQueryMetadata.Value<long>(h, "nested", key) > 2L,
            10 => h => CosmicQueryMetadata.Value<long>(h, "nested", key) == long.MinValue,
            11 => h => CosmicQueryMetadata.Value<string>(h, "nested", key) == "2",
            12 => h => CosmicQueryMetadata.Value<string>(h, "nested", key) != "2",
            13 => h => CosmicQueryMetadata.Value<string>(h, "nested", key) == null,
            14 => h => CosmicQueryMetadata.Value<string>(h, "nested", key) != null,
            15 => h => CosmicQueryMetadata.Value<bool>(h, "nested", key) == true,
            16 => h => CosmicQueryMetadata.Value<bool>(h, "nested", key) != true,
            17 => h => CosmicQueryMetadata.Value<bool?>(h, "nested", key) != true,
            18 => h => CosmicQueryMetadata.Value<string>(h, "nested", key).Contains("\0"),
            19 => h => CosmicQueryMetadata.Value<string>(h, "nested", key).StartsWith("😀", StringComparison.Ordinal),
            20 => h => CosmicQueryMetadata.Value<string>(h, "nested", key).EndsWith("b", StringComparison.Ordinal),
            21 => h => numbers.Contains(CosmicQueryMetadata.Value<int>(h, "nested", key)),
            22 => h => strings.Contains(CosmicQueryMetadata.Value<string>(h, "nested", key)),
            23 => h => !(CosmicQueryMetadata.Value<int>(h, "nested", key) == 2),
            24 => h => empty.Contains(CosmicQueryMetadata.Value<string>(h, "nested", key)),
            _ => h => CosmicQueryMetadata.Value<string>(h, "nested", key).Contains("")
        };
        int[] selected = selection switch {
            0 => new[] { 3, 15 }, 1 => new[] { 3 }, 2 => new[] { 2, 14, 15 }, 3 => new[] { 2, 14 },
            4 => new[] { 3, 15 }, 5 => new[] { 2, 3, 14 }, 6 => new[] { 1 }, 7 => new[] { 2, 3, 14, 15 },
            8 => new[] { 1, 2, 14, 15 }, 9 => new[] { 8, 9, 15 }, 10 => new[] { 16 }, 11 => new[] { 4 },
            12 => new[] { 1, 12, 13 }, 13 => new[] { 1 }, 14 => new[] { 4, 12, 13 }, 15 => new[] { 5 },
            16 => new[] { 6 }, 17 => new[] { 1, 6 }, 18 => new[] { 12 }, 19 => new[] { 13 }, 20 => new[] { 12 },
            21 => new[] { 2, 3 }, 22 => new[] { 1, 4 }, 23 => Enumerable.Range(0, values.Length).Where(i => i != 3).ToArray(),
            24 => Array.Empty<int>(), _ => new[] { 4, 12, 13 }
        };
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Id).OrderBy(id => id).Should().Equal(selected.Select(i => ids[i]).OrderBy(id => id));
    }
}
