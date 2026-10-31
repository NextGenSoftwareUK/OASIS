using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    [InlineData(5)] [InlineData(6)] [InlineData(7)] [InlineData(8)] [InlineData(9)]
    public async Task NativeCqlGuidMetadataUsesCanonicalIdentityAndStrictTypedDomain(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var target = Guid.Parse("aabbccdd-1234-5678-9abc-def012345678");
        var other = Guid.Parse("12345678-1234-5678-9abc-def012345678");
        object?[] values = { null, null, target, target.ToString("D").ToUpperInvariant(), other,
            target.ToString("N"), "{" + target + "}", " " + target, target + "\0", "zzbbccdd-1234-5678-9abc-def012345678", 7, new[] { target } };
        var ids = new List<Guid>();
        for (var i = 0; i < values.Length; i++)
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = i == 0 ? new() : new() { ["guid.value"] = values[i]! } };
            var saved = await provider.SaveHolonAsync(row, saveChildren: false);
            saved.IsError.Should().BeFalse(saved.Message + saved.Exception); ids.Add(row.Id);
        }
        var members = new[] { target }; var nullableMembers = new Guid?[] { null, target };
        Expression<Func<Holon, bool>> predicate = selection switch {
            0 => h => CosmicQueryMetadata.Value<Guid>(h, "guid.value") == target,
            1 => h => CosmicQueryMetadata.Value<Guid>(h, "guid.value") != target,
            2 => h => CosmicQueryMetadata.Value<Guid?>(h, "guid.value") == null,
            3 => h => CosmicQueryMetadata.Value<Guid?>(h, "guid.value") != null,
            4 => h => CosmicQueryMetadata.Value<Guid?>(h, "guid.value") != target,
            5 => h => members.Contains(CosmicQueryMetadata.Value<Guid>(h, "guid.value")),
            6 => h => nullableMembers.Contains(CosmicQueryMetadata.Value<Guid?>(h, "guid.value")),
            7 => h => !(CosmicQueryMetadata.Value<Guid>(h, "guid.value") == target),
            8 => h => CosmicQueryMetadata.Value<string>(h, "guid.value") == "AABBCCDD-1234-5678-9ABC-DEF012345678",
            _ => h => target == CosmicQueryMetadata.Value<Guid>(h, "guid.value")
        };
        int[] selected = selection switch {
            0 or 5 or 9 => new[] { 2, 3 }, 1 => new[] { 4 }, 2 => new[] { 1 },
            3 => new[] { 2, 3, 4 }, 4 => new[] { 1, 4 }, 6 => new[] { 1, 2, 3 },
            8 => new[] { 3 }, _ => Enumerable.Range(0, values.Length).Except(new[] { 2, 3 }).ToArray()
        };
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Id).OrderBy(id => id).Should().Equal(selected.Select(i => ids[i]).OrderBy(id => id));
    }
}
