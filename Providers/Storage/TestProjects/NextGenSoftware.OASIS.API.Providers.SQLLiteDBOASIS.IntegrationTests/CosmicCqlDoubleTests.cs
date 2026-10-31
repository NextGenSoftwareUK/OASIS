using System.Linq.Expressions;
using System.Globalization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public async Task NativeDoublePredicatesUseExactRawTokens(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object?[] values = { double.MinValue, double.MaxValue, 0d, -0.1d, 0.1d, double.Epsilon, -double.Epsilon,
            Math.BitIncrement(1d), 9007199254740992d, 9007199254740994d, null, "0.1", 1e100d };
        var ids = new List<Guid>();
        foreach (var value in values)
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["price"] = value!, ["reference"] = 0.1d } };
            var saved = await provider.SaveHolonAsync(row, saveChildren: false);
            saved.IsError.Should().BeFalse(saved.Message + saved.Exception); ids.Add(row.Id);
        }
        var members = new[] { -0.1d, double.Epsilon };
        Expression<Func<Holon, bool>> predicate = selection switch
        {
            0 => h => CosmicQueryMetadata.Value<double>(h, "price") == 0.1d,
            1 => h => CosmicQueryMetadata.Value<double>(h, "price") > 9007199254740992d,
            2 => h => members.Contains(CosmicQueryMetadata.Value<double>(h, "price")),
            3 => h => CosmicQueryMetadata.Value<double?>(h, "price") == null,
            4 => h => CosmicQueryMetadata.Value<double?>(h, "price") != 0.1d,
            5 => h => CosmicQueryMetadata.Value<double>(h, "price") == CosmicQueryMetadata.Value<double>(h, "reference"),
            6 => h => !(CosmicQueryMetadata.Value<double>(h, "price") == 0.1d),
            _ => h => CosmicQueryMetadata.Value<double>(h, "price") < 0d
        };
        int[] expected = selection switch
        {
            0 or 5 => new[] { 4 }, 1 => new[] { 1, 9, 12 }, 2 => new[] { 3, 5 }, 3 => new[] { 10 },
            4 => Enumerable.Range(0, values.Length).Except(new[] { 4, 11 }).ToArray(),
            6 => Enumerable.Range(0, values.Length).Except(new[] { 4 }).ToArray(), _ => new[] { 0, 3, 6 }
        };
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Id).Should().BeEquivalentTo(expected.Select(index => ids[index]));
    }

    [Theory]
    [InlineData("price")] [InlineData("p.rice")] [InlineData("quote\"key")]
    public async Task NativeDoubleProjectionOrderingAndExtremaRemainExact(string key)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var values = new[] { double.MaxValue, double.MinValue, double.Epsilon, -double.Epsilon, 9007199254740992d, 9007199254740994d };
        foreach (var value in values)
            (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), MetaData = new() { [key] = value } }, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<ScalarMustNotConstruct>.Create()
            .OrderBy(h => CosmicQueryMetadata.Value<double>(h, key))
            .Select(h => CosmicQueryMetadata.Value<double>(h, key)).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Should().Equal(values.OrderBy(value => value));
        var source = CosmicQuery<ScalarMustNotConstruct>.Create();
        var min = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => CosmicQueryMetadata.Value<double>(h, key)).Result));
        var max = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Max(h => CosmicQueryMetadata.Value<double>(h, key)).Result));
        min.IsError.Should().BeFalse(min.Message + min.Exception); max.IsError.Should().BeFalse(max.Message + max.Exception);
        min.Result.Should().Be(double.MinValue); max.Result.Should().Be(double.MaxValue);
    }

    [Fact]
    public async Task NativeDoubleGroupedKeysNormalizeIdentityAndFilterPublishedValues()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var value in new double?[] { 1d, 1.000d, null, -double.Epsilon, 9007199254740992d, 9007199254740994d })
            (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), MetaData = new() { ["price"] = value! } }, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().GroupBy(h => CosmicQueryMetadata.Value<double?>(h, "price"))
            .Select(g => new { Price = g.Key, Items = g.LongCount(), Minimum = g.Min(h => CosmicQueryMetadata.Value<double?>(h, "price")) })
            .Where(row => row.Price > 0d).OrderBy(row => row.Price).Take(3).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var rows = result.Result.Providers.Single().Rows;
        rows.Select(row => row.Price).Should().Equal(1d, 9007199254740992d, 9007199254740994d);
        rows[0].Items.Should().Be(2); rows.Select(row => row.Minimum).Should().Equal(rows.Select(row => row.Price));
    }

    [Theory]
    [InlineData("1e1000")] [InlineData("1e-1000")]
    [InlineData("\"1.5\"")]
    [InlineData("true")] [InlineData("[1.5]")] [InlineData("{}")] [InlineData("null")]
    public async Task NativeDoubleProjectionRejectsUnrepresentableOrForeignTokens(string token)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid() }, saveChildren: false)).IsError.Should().BeFalse();
        using (var context = new DataContext(TestConnectionString))
            context.Database.ExecuteSqlRaw("UPDATE Holon SET MetaDataJson = {0}", "{\"price\":" + token + "}").Should().Be(1);
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().Select(h => CosmicQueryMetadata.Value<double>(h, "price")).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_DATA_INVALID");
    }

    [Theory]
    [InlineData("4.9406564584124654e-324", "5E-324")]
    [InlineData("1.00000000000000011102230246251565404236316680908203125", "1")]
    [InlineData("1.00000000000000011102230246251565404236316680908203126", "1.0000000000000002")]
    [InlineData("9007199254740993", "9007199254740992")]
    [InlineData("-0", "0")] [InlineData("0.1000000000000000000000000000", "0.1")]
    public async Task NativeDoubleRawTokensUseDeclaredBinary64Semantics(string token, string expected)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid() }, saveChildren: false)).IsError.Should().BeFalse();
        using (var context = new DataContext(TestConnectionString))
            context.Database.ExecuteSqlRaw("UPDATE Holon SET MetaDataJson = {0}", "{\"price\":" + token + "}").Should().Be(1);
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().Select(h => CosmicQueryMetadata.Value<double>(h, "price")).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Should().Be(double.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task NativeDoubleTokenBudgetCannotDisappearBehindAFilter()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid() }, saveChildren: false)).IsError.Should().BeFalse();
        using (var context = new DataContext(TestConnectionString))
            context.Database.ExecuteSqlRaw("UPDATE Holon SET MetaDataJson = {0}", "{\"price\":0." + new string('0', 5000) + "1}").Should().Be(1);
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => CosmicQueryMetadata.Value<double>(h, "price") > 1d));
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_QUERY_LIMIT");
    }
}
