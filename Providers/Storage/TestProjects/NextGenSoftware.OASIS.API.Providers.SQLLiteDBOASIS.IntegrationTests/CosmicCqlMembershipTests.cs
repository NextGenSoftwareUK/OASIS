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
    public static IEnumerable<object[]> CqlMembershipCases()
    {
        for (var selection = 0; selection < 12; selection++) foreach (var negate in new[] { false, true }) yield return new object[] { selection, negate };
    }
    [Theory]
    [MemberData(nameof(CqlMembershipCases))]
    public async Task NativeCqlMembershipMatchesClrForNullEmptyDuplicateAndScalarArrays(int selection, bool negate)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var names = new string?[] { null, "Coffee", "coffee", "%_'; --", "a\0b", "😀" };
        var rows = new List<Holon>();
        for (var index = 0; index < names.Length; index++)
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = names[index]!, Version = index + 1, IsActive = index % 2 == 0,
                HolonType = index % 2 == 0 ? HolonType.Planet : HolonType.Quest };
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse();
            rows.Add((Holon)(await provider.LoadHolonAsync(row.Id, loadChildren: false)).Result);
        }
        var text = new[] { "Coffee", "%_'; --" }; var nullable = new string?[] { null, "coffee" };
        var empty = Array.Empty<string>(); var onlyNull = new string?[] { null }; var versions = new[] { 1, 3, 5 };
        var types = new[] { HolonType.Quest }; var ids = new[] { rows[0].Id, rows[4].Id }; var flags = new[] { true };
        var duplicates = new[] { "Coffee", "Coffee" }; var unicode = new[] { "a\0b", "😀" }; var actors = new[] { Guid.Empty };
        Expression<Func<Holon, bool>> predicate = selection switch {
            0 => h => text.Contains(h.Name), 1 => h => nullable.Contains(h.Name), 2 => h => empty.Contains(h.Name),
            3 => h => onlyNull.Contains(h.Name), 4 => h => versions.Contains(h.Version), 5 => h => types.Contains(h.HolonType),
            6 => h => ids.Contains(h.Id), 7 => h => flags.Contains(h.IsActive), 8 => h => new[] { "Coffee", "coffee" }.Contains(h.Name),
            9 => h => duplicates.Contains(h.Name), 10 => h => unicode.Contains(h.Name), _ => h => actors.Contains(h.CreatedByAvatarId)
        };
        if (negate) predicate = Expression.Lambda<Func<Holon, bool>>(Expression.Not(predicate.Body), predicate.Parameters);
        var expected = rows.Where(predicate.Compile()).Select(h => h.Id).OrderBy(id => id).ToArray();
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Id).OrderBy(id => id).Should().Equal(expected);
    }

    [Fact]
    public async Task NativeCqlMembershipHonorsTheWholeQueryParameterBudgetBeforeIO()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); ICosmicQueryStorageProvider adapter = provider;
        var values = Enumerable.Range(0, CosmicQueryExpression.MaximumMembershipValues).Select(index => "value" + index).ToArray();
        var query = CosmicQuery<Holon>.Create();
        for (var index = 0; index < 8; index++) { var next = query.Where(h => values.Contains(h.Name)); next.IsError.Should().BeFalse(next.Message); query = next.Result; }
        var plan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType, new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        plan.IsError.Should().BeFalse(plan.Message);
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_QUERY_LIMIT"); File.Exists(_databasePath).Should().BeFalse();
    }

    [Fact]
    public async Task NativeCqlDoesNotAdmitUnsafeMixedNumericCoercionEvenWithForgedProfile()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString); ICosmicQueryStorageProvider adapter = provider;
        var value = 1.000000000000000000000000001m;
        var query = CosmicQuery<Holon>.Create().Where(h => (decimal)h.Version == value).Result;
        var actual = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType, new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
        actual.IsError.Should().BeTrue(); actual.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED");
        var overbroad = new CosmicQueryCapabilities(CosmicQueryFeatures.MixedNumericComparison, new[] { typeof(int) }, new[] { CosmicQueryComparisonOperator.Equal });
        var forged = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType, new[] { new CosmicQueryTarget(adapter.QueryProviderType, overbroad) });
        forged.IsError.Should().BeFalse(forged.Message);
        var result = await provider.ExecuteCosmicQueryAsync(forged.Result);
        result.IsError.Should().BeTrue(); result.ErrorCode.Should().Be("CQL_CAPABILITY_UNSUPPORTED"); File.Exists(_databasePath).Should().BeFalse();
    }
}
