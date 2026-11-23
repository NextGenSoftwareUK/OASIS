using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicAsyncReductionBoundaryTests
{
    [Fact]
    public async Task LocalNumericReductionIsRefusedWithoutEnumerationAndCancellationIsPreserved()
    {
        var enumerated=false;
        var local=Enumerable.Range(0,1).Select(value=>{enumerated=true;return value;}).AsQueryable();
        var result=await local.ToCosmicSumAsync();
        Assert.True(result.IsError);Assert.Equal("CQL_QUERY_PROVIDER_INVALID",result.ErrorCode);Assert.False(enumerated);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var cancelled=await local.ToCosmicAverageAsync(cancellation.Token);
        Assert.True(cancelled.IsError);Assert.Equal("CQL_QUERY_CANCELLED",cancelled.ErrorCode);Assert.False(enumerated);
    }
}
