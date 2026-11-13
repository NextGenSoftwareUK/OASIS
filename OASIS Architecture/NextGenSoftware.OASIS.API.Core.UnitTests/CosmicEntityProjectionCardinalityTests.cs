using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityProjectionTests
{
    private static CosmicProjectedCardinalityPlan<Holon,ProjectionDto,TValue> EntityCardinality<TValue>(
        EntitySelectedNative native,CosmicScalarOperation operation)
    {
        var entity=CombinedPlan(native).EntitySource.Query;
        var query=entity.Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version}).Result.Take(1).Result;
        var plan=CosmicProjectedCardinalityPlanner.PlanEntity<Holon,ProjectionDto,TValue>(query,operation,native.QueryProviderType,
            new CosmicProjectionCompositionTarget(native.QueryProviderType,native.QueryCapabilities,native.ProjectionCapabilities,native.CompositionCapabilities),
            native.EntityCompositionCapabilities,true);
        Assert.False(plan.IsError,plan.Message);return plan.Result;
    }
    [Fact]
    public async Task EntitySelectedCardinalityUsesNativeCountWithoutDtoConstruction()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket(nativeCount:1)};ProjectionDto.Constructors=0;
        var count=await Manager().ExecuteProjectedCardinalityQueryAsync(EntityCardinality<int>(native,CosmicScalarOperation.Count),new[]{native});
        Assert.False(count.IsError,count.Message);Assert.Equal(1,count.Result.Providers.Single().Value);
        var longer=await Manager().ExecuteProjectedCardinalityQueryAsync(EntityCardinality<long>(native,CosmicScalarOperation.LongCount),new[]{native});
        Assert.False(longer.IsError,longer.Message);Assert.Equal(1L,longer.Result.Providers.Single().Value);
        var any=await Manager().ExecuteProjectedCardinalityQueryAsync(EntityCardinality<bool>(native,CosmicScalarOperation.Any),new[]{native});
        Assert.False(any.IsError,any.Message);Assert.True(any.Result.Providers.Single().Value);
        Assert.Equal(0,ProjectionDto.Constructors);Assert.Equal(3,native.CombinedCalls);Assert.Equal(0,native.Calls);
    }
    [Fact]
    public void StandardTerminalsRetainEntityAndSelectedStagesAcrossReselection()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket(nativeCount:1)};ProjectionDto.Constructors=0;
        var selected=LinqRoot(native).Take(3).Reverse().Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version})
            .Take(1).Select(row=>row.Name);
        Assert.Equal(1,selected.Count());Assert.Equal(1L,selected.LongCount());Assert.True(selected.Any());
        Assert.Equal(0,ProjectionDto.Constructors);Assert.Equal(3,native.CombinedCalls);
    }
    [Fact]
    public async Task CombinedCountNeedsIndependentActualEnrollmentAndExactAcknowledgement()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket(nativeCount:1)};
        var plan=EntityCardinality<int>(native,CosmicScalarOperation.Count);
        native.CardinalityEnabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await Manager().ExecuteProjectedCardinalityQueryAsync(plan,new[]{native})).ErrorCode);
        native.CardinalityEnabled=true;native.CardinalityProfile=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await Manager().ExecuteProjectedCardinalityQueryAsync(plan,new[]{native})).ErrorCode);
        Assert.Equal(0,native.CombinedCalls);
        native.CardinalityProfile=true;native.Packet=CombinedPacket();
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectedCardinalityQueryAsync(plan,new[]{native})).ErrorCode);
        native.Packet=CombinedPacket(nativeCount:2);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectedCardinalityQueryAsync(plan,new[]{native})).ErrorCode);
        native.Packet=CombinedPacket(true,1);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectedCardinalityQueryAsync(plan,new[]{native})).ErrorCode);
    }
    [Fact]
    public async Task CountCancellationAndV2AdmissionPrecedeDispatch()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket(nativeCount:1)};
        var plan=EntityCardinality<int>(native,CosmicScalarOperation.Count);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED",(await Manager().ExecuteProjectedCardinalityQueryAsync(plan,new[]{native},cancellation.Token)).ErrorCode);
        Assert.Equal("CQL_V2_REQUIRED",(await Manager(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.Legacy).ExecuteProjectedCardinalityQueryAsync(plan,new[]{native})).ErrorCode);
        Assert.Equal(0,native.CombinedCalls);
    }
}
