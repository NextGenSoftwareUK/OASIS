using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Fact]
    public async Task NativeCqlUnicodeScalarOrderPreservesSupplementaryBoundaryThroughPaging()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();

        // U+E000 sorts before U+10000 in Unicode scalar order. CLR UTF-16
        // ordinal sorting reverses that pair; using it as an oracle is incorrect.
        var values = new string?[] { null, "A", "\uE000", "\U00010000" };
        var rows = values.Select(value => new Holon
        {
            Id = Guid.NewGuid(),
            Name = value!,
            MetaData = new() { ["unicode"] = value! }
        }).ToArray();

        foreach (var row in rows)
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();

        ICosmicQueryStorageProvider adapter = provider;
        var ids = rows.Select(row => row.Id).ToArray();
        var source = CosmicQuery<Holon>.Create().Where(row => ids.Contains(row.Id)).Result;

        // Canonical and metadata operands must share the same native ordering.
        await Verify(source.OrderBy(row => row.Name).Result, 0, 1, 2, 3);
        await Verify(source.OrderByDescending(row => row.Name).Result, 3, 2, 1, 0);
        await Verify(source.OrderBy(row => CosmicQueryMetadata.Value<string>(row, "unicode")).Result, 0, 1, 2, 3);
        await Verify(source.OrderByDescending(row => CosmicQueryMetadata.Value<string>(row, "unicode")).Result, 3, 2, 1, 0);
        await Verify(source.OrderByDescending(row => CosmicQueryMetadata.Value<string>(row, "unicode")).Skip(1).Take(2).Result, 2, 1);

        async Task Verify(CosmicQuery<Holon> query, params int[] expected)
        {
            var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType,
                new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
            plan.IsError.Should().BeFalse(plan.Message);
            var result = await adapter.ExecuteCosmicQueryAsync(plan.Result);
            result.IsError.Should().BeFalse(result.Message + result.Exception);
            result.Result.Select(row => row.Id).Should().Equal(expected.Select(index => rows[index].Id));
        }
    }
}
