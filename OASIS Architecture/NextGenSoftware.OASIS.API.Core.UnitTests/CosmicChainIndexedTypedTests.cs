using Moq;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    public sealed class IndexedTypedRow : Holon
    {
        [CustomOASISProperty] public int Rating { get; set; }
    }
    public sealed class IndexedThrowingRow : Holon
    {
        [CustomOASISProperty] public int Rating { get => 0; set => throw new InvalidOperationException("typed source failure"); }
    }
    public sealed class IndexedIdentityMutatingRow : Holon
    {
        [CustomOASISProperty] public int Rating { get => 0; set => Id = Guid.NewGuid(); }
    }
    private static HolonManager Manager(string mode = HyperDriveModes.V2)
    {
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        return new HolonManager(null, dna, new ProviderManager(null, dna));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ManagerRetainsSourceEvidenceAndUsesCleanTypedTracking(bool empty)
    {
        var f = Fixture(empty ? "empty" : "rows");
        f.Row.MetaData = new() { ["Rating"] = 7 };
        var result = await Manager().ExecuteIndexedQueryAsync(Plan<IndexedTypedRow>(), f.Reader,
            Mock.Of<ICosmicChainQuerySourceObserver>());
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal(source.SourceProvider, result.Result.Checkpoint.Source.SourceProvider);
        Assert.Equal(source.ExecutionProvider, result.Result.Checkpoint.Source.ExecutionProvider);
        Assert.Equal(result.Result.Rows.Count, result.Result.Receipts.Count);
        if (empty) { Assert.Empty(result.Result.Rows); return; }
        var row = Assert.Single(result.Result.Rows);
        Assert.Equal(7, row.Rating);
        Assert.False(HolonChangeTracking.HasChanges(row));
        Assert.NotSame(f.Row, row);
        Assert.Equal(row.Id, Assert.Single(result.Result.Receipts).Record.Id);
        row.Rating = 9;
        Assert.True(HolonChangeTracking.HasChanges(row));
        Assert.Equal(7, f.Row.MetaData["Rating"]);
        Assert.Empty(row.Children);
    }

    [Fact]
    public async Task ManagerReturnsHydrationErrorWithoutPublishingPartialTypedRows()
    {
        var f = Fixture("rows"); f.Row.MetaData = new() { ["Rating"] = 7 };
        var result = await Manager().ExecuteIndexedQueryAsync(Plan<IndexedThrowingRow>(), f.Reader,
            Mock.Of<ICosmicChainQuerySourceObserver>());
        Assert.Equal("COSMIC_METADATA_HYDRATION_FAILED", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.False(result.IsLoaded);
        Assert.Equal(7, f.Row.MetaData["Rating"]);
    }

    [Fact]
    public async Task TypedSetterCannotChangeTheReceiptBoundIdentity()
    {
        var f = Fixture("rows"); f.Row.MetaData = new() { ["Rating"] = 7 };
        var original = f.Row.Id;
        var result = await Manager().ExecuteIndexedQueryAsync(Plan<IndexedIdentityMutatingRow>(), f.Reader,
            Mock.Of<ICosmicChainQuerySourceObserver>());
        Assert.Equal("CQL_SOURCE_RECORD_MISMATCH", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal(original, f.Row.Id);
    }

    [Fact]
    public async Task LegacyManagerRejectsIndexedExecutionBeforeReaderIo()
    {
        var f = Fixture("rows");
        var result = await Manager("V1").ExecuteIndexedQueryAsync(Plan<IndexedTypedRow>(), f.Reader,
            Mock.Of<ICosmicChainQuerySourceObserver>());
        Assert.Equal("CQL_V2_REQUIRED", result.ErrorCode);
        Assert.Equal(0, f.Reader.Calls);
    }
}
