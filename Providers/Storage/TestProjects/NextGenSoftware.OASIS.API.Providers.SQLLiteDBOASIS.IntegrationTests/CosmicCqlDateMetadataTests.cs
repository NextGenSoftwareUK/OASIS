using System.Linq.Expressions;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public static IEnumerable<object[]> DateMetadataSelections => Enumerable.Range(0, 18).Select(i => new object[] { i });

    [Theory]
    [MemberData(nameof(DateMetadataSelections))]
    public async Task NativeCqlUtcDateMetadataPreservesTicksAndRejectsMalformedCalendarValues(int selection)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var instant = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        var target = instant.AddTicks(1); var later = instant.AddTicks(2);
        var minimum = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        var maximum = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
        var leap = new DateTime(2000, 2, 29, 0, 0, 0, DateTimeKind.Utc);
        object?[] values = { null, null, target, later, instant, minimum, maximum, "2026-10-20T12:00:00.0000001Z",
            "2026-10-20T12:00:00Z", "2026-10-20T12:00:00.0Z", "2000-02-29T00:00:00Z",
            "1900-02-29T00:00:00Z", "2026-04-31T00:00:00Z", "2026-13-01T00:00:00Z", "0000-01-01T00:00:00Z",
            "2026-10-20T24:00:00Z", "2026-10-20T12:60:00Z", "2026-10-20T12:00:60Z", "2026-10-20T12:00:00.Z",
            "2026-10-20T12:00:00.00000001Z", "2026-10-20T12:00:00+00:00", "2026-10-20T12:00:00Z\0", "2026-10-20T12:00:00Z\n",
            "2026-10-20T12:00:00.1xZ", 7, new[] { target }, "2026-10-20t12:00:00z" };
        var ids = new List<Guid>();
        for (var i = 0; i < values.Length; i++)
        {
            var row = new Holon { Id = Guid.NewGuid(), MetaData = i == 0 ? new() : new() { ["when/~"] = values[i]! } };
            var saved = await provider.SaveHolonAsync(row, saveChildren: false);
            saved.IsError.Should().BeFalse(saved.Message + saved.Exception); ids.Add(row.Id);
        }
        var dates = new[] { target }; var nullableDates = new DateTime?[] { null, target };
        Expression<Func<Holon, bool>> predicate = selection switch {
            0 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") == target,
            1 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") != target,
            2 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") > target,
            3 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") >= target,
            4 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") < target,
            5 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") <= target,
            6 => h => CosmicQueryMetadata.Value<DateTime?>(h, "when/~") == null,
            7 => h => CosmicQueryMetadata.Value<DateTime?>(h, "when/~") != null,
            8 => h => CosmicQueryMetadata.Value<DateTime?>(h, "when/~") != target,
            9 => h => dates.Contains(CosmicQueryMetadata.Value<DateTime>(h, "when/~")),
            10 => h => nullableDates.Contains(CosmicQueryMetadata.Value<DateTime?>(h, "when/~")),
            11 => h => !(CosmicQueryMetadata.Value<DateTime>(h, "when/~") == target),
            12 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") == minimum,
            13 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") == maximum,
            14 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") == leap,
            15 => h => CosmicQueryMetadata.Value<DateTime>(h, "when/~") == instant,
            16 => h => target == CosmicQueryMetadata.Value<DateTime>(h, "when/~"),
            _ => h => CosmicQueryMetadata.Value<string>(h, "when/~") == "2026-10-20T12:00:00.0Z"
        };
        int[] selected = selection switch {
            0 or 9 or 16 => new[] { 2, 7 }, 1 => new[] { 3, 4, 5, 6, 8, 9, 10 }, 2 => new[] { 3, 6 },
            3 => new[] { 2, 3, 6, 7 }, 4 => new[] { 4, 5, 8, 9, 10 }, 5 => new[] { 2, 4, 5, 7, 8, 9, 10 },
            6 => new[] { 1 }, 7 => Enumerable.Range(2, 9).ToArray(), 8 => new[] { 1, 3, 4, 5, 6, 8, 9, 10 },
            10 => new[] { 1, 2, 7 }, 11 => Enumerable.Range(0, values.Length).Except(new[] { 2, 7 }).ToArray(),
            12 => new[] { 5 }, 13 => new[] { 6 }, 14 => new[] { 10 }, 15 => new[] { 4, 8, 9 }, _ => new[] { 9 }
        };
        var result = await provider.ExecuteCosmicQueryAsync(CqlPlan(predicate));
        result.IsError.Should().BeFalse(result.Message + result.Exception);
        result.Result.Select(h => h.Id).OrderBy(id => id).Should().Equal(selected.Select(i => ids[i]).OrderBy(id => id));
    }
}
