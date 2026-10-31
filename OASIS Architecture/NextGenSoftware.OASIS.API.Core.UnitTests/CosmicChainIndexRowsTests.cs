using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChainIndexRowsTests
{
    private static (CosmicChainQueryCheckpoint Checkpoint, CosmicChainQueryFinalizedHead Head,
        Holon Row, CosmicChainQueryRecordReceipt Receipt) Fixture()
    {
        var source = new CosmicChainQuerySource(ProviderType.SolanaOASIS, "network",
            ProviderType.SQLLiteDBOASIS, Guid.NewGuid(), 2, new string('A', 64), 0);
        var position = new CosmicChainQueryPosition(1, "block");
        var row = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid() };
        var receipt = new CosmicChainQueryRecordReceipt(source, position, "commit",
            new(row.Id, row.Version, row.VersionId, false, new string('B', 64)));
        return (new(source, position, 0), new(source.SourceProvider, source.NetworkIdentity,
            position, DateTimeOffset.UtcNow), row, receipt);
    }

    [Fact]
    public void CapturesCollectionsAndPreservesLogicalAndExecutionIdentity()
    {
        var f = Fixture();
        var rows = new IHolon[] { f.Row };
        var receipts = new[] { f.Receipt };
        var page = new CosmicChainIndexRows(f.Checkpoint, f.Head, rows, receipts);
        rows[0] = new Holon();
        receipts[0] = null!;
        Assert.Same(f.Row, Assert.Single(page.Rows));
        Assert.Same(f.Receipt, Assert.Single(page.Receipts));
        Assert.Equal(ProviderType.SolanaOASIS, page.Checkpoint.Source.SourceProvider);
        Assert.Equal(ProviderType.SQLLiteDBOASIS, page.Checkpoint.Source.ExecutionProvider);
    }

    [Theory]
    [InlineData("count")] [InlineData("null-row")] [InlineData("null-receipt")] [InlineData("limit")]
    public void RejectsIncompleteOrUnboundedReceiptPages(string mode)
    {
        var f = Fixture();
        IEnumerable<IHolon> rows = mode == "limit" ? Enumerable.Repeat<IHolon>(f.Row, 10001)
            : new IHolon[] { mode == "null-row" ? null! : f.Row };
        IEnumerable<CosmicChainQueryRecordReceipt> receipts = mode == "count"
            ? Array.Empty<CosmicChainQueryRecordReceipt>() : mode == "limit"
            ? Enumerable.Repeat(f.Receipt, 10001)
            : new[] { mode == "null-receipt" ? null! : f.Receipt };
        Assert.Throws<ArgumentException>(() => new CosmicChainIndexRows(f.Checkpoint, f.Head, rows, receipts));
    }

    [Fact]
    public void EmptyPageRetainsCheckpointAndFreshHead()
    {
        var f = Fixture();
        var page = new CosmicChainIndexRows(f.Checkpoint, f.Head,
            Array.Empty<IHolon>(), Array.Empty<CosmicChainQueryRecordReceipt>());
        Assert.Empty(page.Rows);
        Assert.Same(f.Checkpoint, page.Checkpoint);
        Assert.Same(f.Head, page.Head);
    }
}
