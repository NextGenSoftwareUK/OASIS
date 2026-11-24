using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicAsyncCardinalityBoundaryTests
{
    [Fact]
    public async Task LocalReductionIsRefusedWithoutEnumerationAndCancellationIsPreserved()
    {
        var enumerated=false;
        var local=Enumerable.Range(0,1).Select(value=>{enumerated=true;return value;}).AsQueryable();
        var count=await local.ToCosmicCountAsync();
        Assert.True(count.IsError);Assert.Equal("CQL_QUERY_PROVIDER_INVALID",count.ErrorCode);Assert.False(enumerated);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await local.ToCosmicAnyAsync(cancellation.Token);
        Assert.True(cancelled.IsError);Assert.Equal("CQL_QUERY_CANCELLED",cancelled.ErrorCode);Assert.False(enumerated);
    }
}
