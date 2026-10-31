using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    private async Task<Guid[]> SeedSinglePromotionRows(SQLLiteDBOASIS provider)
    {
        var ids = new List<Guid>();
        foreach (var value in new[] { MathF.BitDecrement(0.1f), 0.1f, MathF.BitIncrement(0.1f) })
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["reading"] = value } };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
            ids.Add(row.Id);
        }
        return ids.ToArray();
    }

    [Fact]
    public async Task NativeSinglePromotedDoubleComparisonsPreserveLiteralPrecision()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedSinglePromotionRows(provider);
        Expression<Func<Holon, bool>>[] predicates =
        {
            h => (double)CosmicQueryMetadata.Value<float>(h, "reading") == 0.1d,
            h => (double)CosmicQueryMetadata.Value<float>(h, "reading") != 0.1d,
            h => (double)CosmicQueryMetadata.Value<float>(h, "reading") > 0.1d,
            h => (double)CosmicQueryMetadata.Value<float>(h, "reading") >= 0.1d,
            h => (double)CosmicQueryMetadata.Value<float>(h, "reading") < 0.1d,
            h => (double)CosmicQueryMetadata.Value<float>(h, "reading") <= 0.1d,
            h => (double)CosmicQueryMetadata.Value<float>(h, "reading") > 0.100000002d,
            h => (double)CosmicQueryMetadata.Value<float>(h, "reading") < 0.100000002d
        };
        int[][] expected = { Array.Empty<int>(), new[] { 0, 1, 2 }, new[] { 1, 2 }, new[] { 1, 2 },
            new[] { 0 }, new[] { 0 }, new[] { 2 }, new[] { 0, 1 } };
        for (var index = 0; index < predicates.Length; index++)
        {
            var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicates[index]));
            result.IsError.Should().BeFalse(result.Message + result.Exception);
            result.Result.Select(h => h.Id).Should().BeEquivalentTo(expected[index].Select(item => ids[item]));
        }
    }

    [Fact]
    public async Task NativeSinglePromotedMembershipDoesNotNarrowDoubleMembers()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedSinglePromotionRows(provider);
        var members = new[] { 0.1d };
        var none = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => members.Contains((double)CosmicQueryMetadata.Value<float>(h, "reading"))));
        none.IsError.Should().BeFalse(none.Message + none.Exception); none.Result.Should().BeEmpty();
        members = new[] { (double)0.1f };
        var exact = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => members.Contains((double)CosmicQueryMetadata.Value<float>(h, "reading"))));
        exact.IsError.Should().BeFalse(exact.Message + exact.Exception);
        exact.Result.Select(h => h.Id).Should().Equal(ids[1]);
    }

    [Fact]
    public async Task NativeSinglePromotedGroupedLiteralDoesNotChangeKeyIdentity()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        await SeedSinglePromotionRows(provider);
        var query = CosmicQuery<ScalarMustNotConstruct>.Create().GroupBy(h => CosmicQueryMetadata.Value<float>(h, "reading"))
            .Select(g => new { Reading = g.Key, Items = g.LongCount() })
            .Where(row => (double)row.Reading == 0.1d).OrderBy(row => row.Reading).Result;
        var result = await CosmicGroupedExecutor.ExecuteAsync(GroupPlan(provider, query), new ICosmicQueryStorageProvider[] { provider });
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Providers.Single().Rows.Should().BeEmpty();
    }
}
