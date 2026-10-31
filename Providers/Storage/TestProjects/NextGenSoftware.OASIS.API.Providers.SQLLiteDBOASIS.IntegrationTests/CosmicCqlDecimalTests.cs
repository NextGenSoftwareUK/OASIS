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
    public async Task NativeDecimalPredicatesUseExactRawTokens(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        object?[] values = { decimal.MinValue, decimal.MaxValue, 0m, -0.1m, 0.1m, 1e-28m, -1e-28m,
            1.0000000000000000000000000001m, 9007199254740993.1m, 9007199254740993.2m, null, "0.1", 1e100d };
        var ids = new List<Guid>();
        foreach (var value in values)
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["price"] = value!, ["reference"] = 0.1m } };
            var saved = await provider.SaveHolonAsync(row, saveChildren: false);
            saved.IsError.Should().BeFalse(saved.Message + saved.Exception); ids.Add(row.Id);
        }
        var members = new[] { -0.1m, 1e-28m };
        Expression<Func<Holon, bool>> predicate = selection switch
        {
            0 => h => CosmicQueryMetadata.Value<decimal>(h, "price") == 0.1m,
            1 => h => CosmicQueryMetadata.Value<decimal>(h, "price") > 9007199254740993m,
            2 => h => members.Contains(CosmicQueryMetadata.Value<decimal>(h, "price")),
            3 => h => CosmicQueryMetadata.Value<decimal?>(h, "price") == null,
            4 => h => CosmicQueryMetadata.Value<decimal?>(h, "price") != 0.1m,
            5 => h => CosmicQueryMetadata.Value<decimal>(h, "price") == CosmicQueryMetadata.Value<decimal>(h, "reference"),
            6 => h => !(CosmicQueryMetadata.Value<decimal>(h, "price") == 0.1m),
            _ => h => CosmicQueryMetadata.Value<decimal>(h, "price") < 0m
        };
        int[] expected = selection switch
        {
            0 or 5 => new[] { 4 }, 1 => new[] { 1, 8, 9 }, 2 => new[] { 3, 5 }, 3 => new[] { 10 },
            4 => Enumerable.Range(0, 11).Except(new[] { 4 }).ToArray(),
            6 => Enumerable.Range(0, values.Length).Except(new[] { 4 }).ToArray(), _ => new[] { 0, 3, 6 }
        };
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Id).Should().BeEquivalentTo(expected.Select(index => ids[index]));
    }

    [Theory]
    [InlineData("price")] [InlineData("p.rice")] [InlineData("quote\"key")]
    public async Task NativeDecimalProjectionOrderingAndExtremaRemainExact(string key)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var values = new[] { decimal.MaxValue, decimal.MinValue, 1e-28m, -1e-28m, 9007199254740993.1m, 9007199254740993.2m };
        foreach (var value in values)
            (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), MetaData = new() { [key] = value } }, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<ScalarMustNotConstruct>.Create()
            .OrderBy(h => CosmicQueryMetadata.Value<decimal>(h, key))
            .Select(h => CosmicQueryMetadata.Value<decimal>(h, key)).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Should().Equal(values.OrderBy(value => value));
        var source = CosmicQuery<ScalarMustNotConstruct>.Create();
        var min = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Min(h => CosmicQueryMetadata.Value<decimal>(h, key)).Result));
        var max = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, source.Max(h => CosmicQueryMetadata.Value<decimal>(h, key)).Result));
        min.IsError.Should().BeFalse(min.Message + min.Exception); max.IsError.Should().BeFalse(max.Message + max.Exception);
        min.Result.Should().Be(decimal.MinValue); max.Result.Should().Be(decimal.MaxValue);
    }

    [Fact]
    public async Task NativeDecimalGroupedKeysNormalizeScaleAndFilterPublishedValues()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var value in new decimal?[] { 1m, 1.000m, null, -1e-28m, 9007199254740993.1m, 9007199254740993.2m })
            (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), MetaData = new() { ["price"] = value! } }, saveChildren: false)).IsError.Should().BeFalse();
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().GroupBy(h => CosmicQueryMetadata.Value<decimal?>(h, "price"))
            .Select(g => new { Price = g.Key, Items = g.LongCount(), Minimum = g.Min(h => CosmicQueryMetadata.Value<decimal?>(h, "price")) })
            .Where(row => row.Price > 0m).OrderBy(row => row.Price).Take(3).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        var rows = result.Result.Providers.Single().Rows;
        rows.Select(row => row.Price).Should().Equal(1m, 9007199254740993.1m, 9007199254740993.2m);
        rows[0].Items.Should().Be(2); rows.Select(row => row.Minimum).Should().Equal(rows.Select(row => row.Price));
    }

    [Theory]
    [InlineData("0.1234567890123456789012345678901")] [InlineData("1e-1000")]
    [InlineData("79228162514264337593543950336")] [InlineData("\"1.5\"")]
    [InlineData("true")] [InlineData("[1.5]")] [InlineData("{}")] [InlineData("null")]
    public async Task NativeDecimalProjectionRejectsUnrepresentableOrForeignTokens(string token)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid() }, saveChildren: false)).IsError.Should().BeFalse();
        using (var context = new DataContext(TestConnectionString))
            context.Database.ExecuteSqlRaw("UPDATE Holon SET MetaDataJson = {0}", "{\"price\":" + token + "}").Should().Be(1);
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().Select(h => CosmicQueryMetadata.Value<decimal>(h, "price")).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_DATA_INVALID");
    }

    [Theory]
    [InlineData("1e-28", "0.0000000000000000000000000001")]
    [InlineData("-0", "0")] [InlineData("0.1000000000000000000000000000", "0.1")]
    public async Task NativeDecimalRawEquivalentTokensHaveOneExactKey(string token, string expected)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid() }, saveChildren: false)).IsError.Should().BeFalse();
        using (var context = new DataContext(TestConnectionString))
            context.Database.ExecuteSqlRaw("UPDATE Holon SET MetaDataJson = {0}", "{\"price\":" + token + "}").Should().Be(1);
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().Select(h => CosmicQueryMetadata.Value<decimal>(h, "price")).Result;
        var result = await CosmicManager(provider, true).ExecuteProjectionQueryAsync(ProjectionPlan(provider, query), new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Single().Should().Be(decimal.Parse(expected, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task NativeDecimalTokenBudgetCannotDisappearBehindAFilter()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid() }, saveChildren: false)).IsError.Should().BeFalse();
        using (var context = new DataContext(TestConnectionString))
            context.Database.ExecuteSqlRaw("UPDATE Holon SET MetaDataJson = {0}", "{\"price\":0." + new string('0', 5000) + "1}").Should().Be(1);
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => CosmicQueryMetadata.Value<decimal>(h, "price") > 1m));
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_QUERY_LIMIT");
    }
}
