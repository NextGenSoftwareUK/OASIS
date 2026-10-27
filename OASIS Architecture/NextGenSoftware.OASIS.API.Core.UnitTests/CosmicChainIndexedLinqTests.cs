using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task IndexedLinqIsDeferredAndReturnsTypedSourceEvidence(bool empty)
    {
        var f = Fixture(empty ? "empty" : "rows"); f.Row.MetaData = new() { ["Rating"] = 7 };
        var created = CosmicLinq.CreateIndexed(Manager(), f.Reader, Mock.Of<ICosmicChainQuerySourceObserver>(),
            Plan<IndexedTypedRow>().Query);
        Assert.False(created.IsError, created.Message);
        var query = created.Result.OrderBy(row => row.Id).Take(1);
        Assert.Equal(0, f.Reader.Calls);
        var page = await query.ToCosmicIndexedPageAsync();
        Assert.False(page.IsError, page.Message + page.Exception);
        Assert.Equal(1, f.Reader.Calls);
        Assert.Equal(source.SourceProvider, page.Result.Checkpoint.Source.SourceProvider);
        Assert.Equal(source.ExecutionProvider, page.Result.Checkpoint.Source.ExecutionProvider);
        Assert.Equal(page.Result.Rows.Count, page.Result.Receipts.Count);
        if (!empty)
        {
            var row = Assert.Single(page.Result.Rows);
            Assert.Equal(7, row.Rating);
            Assert.False(HolonChangeTracking.HasChanges(row));
            // The standard sequence surface remains available; callers requesting
            // receipts explicitly use the page terminal above.
            var list = await query.ToCosmicListAsync();
            Assert.False(list.IsError, list.Message);
            Assert.Equal(row.Id, Assert.Single(list.Result).Id);
        }
    }

    [Theory]
    [InlineData("projection")] [InlineData("stage")] [InlineData("scalar")]
    public void IndexedUnsupportedShapesRejectBeforeReaderIo(string mode)
    {
        var f = Fixture("rows");
        var query = CosmicLinq.CreateIndexed(Manager(), f.Reader, Mock.Of<ICosmicChainQuerySourceObserver>(), Plan().Query).Result;
        var error = Assert.Throws<CosmicLinqException>(() => {
            if (mode == "projection") _ = query.Select(row => row.Name);
            else if (mode == "stage") _ = query.Take(1).Where(row => row.IsPublic);
            else _ = query.Count();
        });
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED", error.Code);
        Assert.Equal(0, f.Reader.Calls);
    }

    [Fact]
    public async Task IndexedPageCancellationPerformsNoReaderIo()
    {
        var f = Fixture("rows");
        var query = CosmicLinq.CreateIndexed(Manager(), f.Reader, Mock.Of<ICosmicChainQuerySourceObserver>(), Plan().Query).Result;
        var page = await query.ToCosmicIndexedPageAsync(new CancellationToken(true));
        Assert.Equal("CQL_QUERY_CANCELLED", page.ErrorCode);
        Assert.Equal(0, f.Reader.Calls);
    }

    [Fact]
    public async Task LocalQueryableCannotSubstituteForAnIndexedPage()
    {
        var page = await new[] { new Holon() }.AsQueryable().ToCosmicIndexedPageAsync();
        Assert.Equal("CQL_QUERY_PROVIDER_INVALID", page.ErrorCode);
    }
}
