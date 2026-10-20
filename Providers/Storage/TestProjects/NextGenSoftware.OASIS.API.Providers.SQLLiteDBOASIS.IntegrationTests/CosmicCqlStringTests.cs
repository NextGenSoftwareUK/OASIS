using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public static IEnumerable<object[]> CqlStringCases()
    {
        foreach (var operation in new[] { 0, 1, 2 })
            foreach (var pattern in new[] { "", "Coffee", "%_[]'; --", "\0", "😀", "e\u0301", "é", "\0suffix" })
                yield return new object[] { operation, pattern };
    }
    [Theory]
    [MemberData(nameof(CqlStringCases))]
    public async Task NativeCqlOrdinalStringPredicatesMatchClrIncludingUnicodeNulAndLiteralWildcards(int operation, string pattern)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var names = new string?[] { null, "", "Coffee", "coffee", "x%_[]'; --y", "a\0b", "😀Cafe\u0301", "é-tail", "prefix\0suffix" };
        var rows = new List<Holon>();
        foreach (var name in names)
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = name! };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); rows.Add(row);
        }
        Expression<Func<Holon, bool>> predicate = operation switch {
            0 => h => h.Name != null && h.Name.Contains(pattern),
            1 => h => h.Name != null && h.Name.StartsWith(pattern, StringComparison.Ordinal),
            _ => h => h.Name != null && h.Name.EndsWith(pattern, StringComparison.Ordinal)
        };
        var expected = rows.Where(predicate.Compile()).Select(h => h.Id).OrderBy(id => id).ToArray();
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Id).OrderBy(id => id).Should().Equal(expected);
    }

    [Fact]
    public async Task NativeCqlNegatedStringPredicateHasExplicitTwoValuedNullSemantics()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        foreach (var name in new string?[] { null, "empty", "x" })
            (await provider.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = name! }, saveChildren: false)).IsError.Should().BeFalse();
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(h => !h.Name.Contains("x")));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Name).Should().BeEquivalentTo(new string?[] { null, "empty" });
    }

    [Fact]
    public void NativeCqlStringAdmissionDoesNotGrantUnmappedOrScalarMetadataPaths()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); ICosmicQueryStorageProvider adapter = provider;
        foreach (var query in new[] { CosmicQuery<Holon>.Create().Where(h => h.CustomKey.Contains("x")).Result,
            CosmicQuery<Holon>.Create().Where(h => CosmicQueryMetadata.Value<string>(h, "title").Contains("x")).Result })
        {
            var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType, new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
            plan.IsError.Should().BeTrue(); plan.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED"); plan.Result.Should().BeNull();
        }
        File.Exists(_databasePath).Should().BeFalse();
    }
}
