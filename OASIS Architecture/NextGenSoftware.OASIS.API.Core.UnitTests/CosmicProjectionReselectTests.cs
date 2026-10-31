using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicLinqProjectionElementTests
{
    public sealed class RenamedDto
    {
        public string? Renamed { get; set; }
    }

    [Fact]
    public void RepeatedSelectRetainsEarlierWindowsAndNeverConstructsIntermediateDto()
    {
        var native = new Native { ExtremaEnabled = true, Values = new[] { "c", "a", "b" } };
        var initial = Root(native).Select(row => new Dto { Label = row.Name }).Skip(1).Take(2);
        Dto.Constructions = 0;
        var renamed = initial.Select(row => new RenamedDto { Renamed = row.Label }).OrderBy(row => row.Renamed);
        Assert.Equal(new[] { "a", "b" }, renamed.Select(row => row.Renamed).ToArray());
        Assert.Equal("a", renamed.Select(row => row.Renamed).Min());
        Assert.Equal("b", renamed.Max(row => row.Renamed));
        Assert.Equal(0, Dto.Constructions);
        Assert.Equal("a", renamed.Select(row => row).First().Renamed);
    }

    [Fact]
    public void RepeatedSelectionRebindsCapturedFiltersAtEveryExecution()
    {
        var native = new Native { ExtremaEnabled = true, Values = new[] { "a", "b", "c" } };
        var threshold = "a";
        var selected = Root(native).Select(row => new Dto { Label = row.Name })
            .Where(row => row.Label == threshold).Take(2).Select(row => row.Label).Select(value => value);
        Assert.Equal(new[] { "a" }, selected.ToArray());
        threshold = "c";
        Assert.Equal(new[] { "c" }, selected.ToArray());
        Assert.Equal(1, selected.Count());
        Assert.Equal("c", selected.Single());
    }

    [Fact]
    public void FluentReselectionRetainsHiddenColumnsAndReordersFinalConstruction()
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result);
        var first = source.Select(row => new { row.Name, row.Version }).Result;
        var second = first.Select(row => new { Revision = row.Version, Text = row.Name, Copy = row.Version }).Result;
        Assert.Equal(new[] { 1, 0, 1 }, second.OutputColumnIndices);
        var rows = CosmicProjectionResults.Materialize(second, new[] { new CosmicProjectionRow("sample", 7) });
        Assert.False(rows.IsError, rows.Message);
        Assert.Equal(7, rows.Result[0].Revision);
        Assert.Equal("sample", rows.Result[0].Text);
        Assert.Equal(7, rows.Result[0].Copy);
        var scalar = second.Select(row => row.Revision).Result;
        var values = CosmicProjectionResults.Materialize(scalar, new[] { new CosmicProjectionRow("sample", 7) });
        Assert.Equal(7, values.Result[0]);
        Assert.Equal(2, scalar.Columns.Count);
        Assert.Equal(new[] { 1 }, scalar.Take(1).Result.OutputColumnIndices);
        Assert.Equal("CQL_PROJECTION_UNSUPPORTED", second.Select(row => row.Revision + 1).ErrorCode);
    }

    [Fact]
    public void RepeatedSelectRejectsComputationsIndexedSelectorsAndExcessiveChains()
    {
        var native = new Native();
        var selected = Root(native).Select(row => row.Name).Take(2);
        Assert.Equal("CQL_PROJECTION_UNSUPPORTED", Assert.Throws<CosmicLinqException>(() => selected.Select(value => value + "suffix")).Code);
        Assert.Throws<CosmicLinqException>(() => selected.Select((value, index) => value));
        for (var index = 0; index < 32; index++) selected = selected.Select(value => value);
        Assert.Throws<CosmicLinqException>(() => selected.Select(value => value));
        Assert.Equal(0, native.Calls);
    }
}
