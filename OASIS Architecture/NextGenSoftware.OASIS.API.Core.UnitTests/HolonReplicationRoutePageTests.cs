using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class HolonReplicationRoutePageTests
{
    [Theory]
    [InlineData(0,null)] [InlineData(257,null)] [InlineData(1,"")] [InlineData(1," route")]
    public void InventoryRequestRejectsUnboundedOrInexactContinuation(int size,string after)
        => Assert.ThrowsAny<ArgumentException>(()=>HolonReplicationRoutePage.ValidateRequest(size,after));

    [Fact]
    public void InventoryPageOwnsItsHeadsAndNeverExposesACompletedOrDuplicateRoute()
    {
        HolonDeliveryInspection Head(string target,HolonDeliveryState state=HolonDeliveryState.Pending)
            =>new(1,Guid.NewGuid(),target,state,0,null,null,null);
        var input=new List<HolonDeliveryInspection>{Head("legacy-route-A"),Head("new-route-a",HolonDeliveryState.ReconciliationRequired)};
        var page=new HolonReplicationRoutePage(input,true);input.Clear();
        Assert.Equal(2,page.Heads.Count);Assert.Equal("new-route-a",page.NextAfterTargetId);
        Assert.Throws<ArgumentException>(()=>new HolonReplicationRoutePage(Array.Empty<HolonDeliveryInspection>(),true));
        Assert.Throws<ArgumentException>(()=>new HolonReplicationRoutePage(new[]{Head("done",HolonDeliveryState.Completed)},false));
        Assert.Throws<ArgumentException>(()=>new HolonReplicationRoutePage(new[]{Head("same"),Head("same")},false));
        Assert.Throws<ArgumentException>(()=>new HolonReplicationRoutePage(Enumerable.Range(0,257).Select(i=>Head("route-"+i)),false));
        Assert.Null(new HolonReplicationRoutePage(Array.Empty<HolonDeliveryInspection>(),false).NextAfterTargetId);
    }
}
