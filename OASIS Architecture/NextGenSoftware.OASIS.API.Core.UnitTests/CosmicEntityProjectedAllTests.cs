using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityProjectionTests
{
    private sealed class EntityAllNative:EntitySelectedNative,ICosmicQueryEntityProjectedAllStorageProvider
    {
        public bool AllEnabled=true,AllProfile=true;
        public int AllCalls;
        public long Counterexamples;
        public bool SupportsEntityProjectedAll=>AllEnabled;
        public override CosmicProjectionCompositionCapabilities CompositionCapabilities=>new(
            CosmicSelectedStageFeatures.Paging,CosmicQueryFeatures.None,
            new[]{new CosmicQueryMetadataCapability(typeof(int),Enum.GetValues<CosmicQueryComparisonOperator>()),
                new CosmicQueryMetadataCapability(typeof(string),Enum.GetValues<CosmicQueryComparisonOperator>())},Array.Empty<Type>(),32,AllProfile,true);
        public Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicProjectedAllAsync<T,TResult>(
            CosmicProjectedAllPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,CancellationToken cancellationToken=default) where T:IHolon
            =>throw new Exception("No flat All substitution for entity stages.");
        public Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicEntityProjectedAllAsync<T,TResult>(
            CosmicProjectedAllPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,CancellationToken cancellationToken=default) where T:IHolon
        {AllCalls++;return Task.FromResult(new OASISResult<CosmicProjectedAllPacket>(new(Packet,Counterexamples)){IsLoaded=Complete,IsWarning=Partial});}
    }
    private static CosmicProjectedAllPlan<Holon,ProjectionDto> EntityAllPlan(EntityAllNative native,int threshold=5,int take=1)
    {
        var entity=CombinedPlan(native).EntitySource.Query;
        var selected=entity.Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version}).Result.Take(take).Result;
        var plan=CosmicProjectedAllPlanner.PlanEntity(selected,row=>row.Revision>threshold,native.QueryProviderType,
            new CosmicProjectionCompositionTarget(native.QueryProviderType,native.QueryCapabilities,native.ProjectionCapabilities,native.CompositionCapabilities),
            native.EntityCompositionCapabilities,true);
        Assert.False(plan.IsError,plan.Message);return plan.Result;
    }
    [Fact]
    public async Task EntitySelectedAllRetainsStagesAndUsesNativeCounterexamplesWithoutDtos()
    {
        var native=new EntityAllNative{Packet=CombinedPacket()};ProjectionDto.Constructors=0;
        var read=await Manager().ExecuteProjectedAllQueryAsync(EntityAllPlan(native),new[]{native});
        Assert.False(read.IsError,read.Message+read.Exception);Assert.True(read.Result.Providers.Single().Value);
        native.Counterexamples=1;
        var falseRead=await Manager().ExecuteProjectedAllQueryAsync(EntityAllPlan(native,10),new[]{native});
        Assert.False(falseRead.IsError,falseRead.Message);Assert.False(falseRead.Result.Providers.Single().Value);
        native.Counterexamples=0;
        var query=LinqRoot(native).Take(3).Reverse().Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version}).Take(1).Select(row=>row.Name);
        Assert.True(query.All(name=>name=="high"));
        Assert.Equal(3,native.AllCalls);Assert.Equal(0,native.Calls);Assert.Equal(0,native.CombinedCalls);Assert.Equal(0,ProjectionDto.Constructors);
    }
    [Fact]
    public async Task AllRequiresIndependentActualEnrollmentAndAcknowledgedCompleteProof()
    {
        var native=new EntityAllNative{Packet=CombinedPacket()};var plan=EntityAllPlan(native);
        native.AllEnabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await Manager().ExecuteProjectedAllQueryAsync(plan,new[]{native})).ErrorCode);
        native.AllEnabled=true;native.AllProfile=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await Manager().ExecuteProjectedAllQueryAsync(plan,new[]{native})).ErrorCode);
        Assert.Equal(0,native.AllCalls);native.AllProfile=true;native.Counterexamples=1;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectedAllQueryAsync(plan,new[]{native})).ErrorCode);
        native.Counterexamples=0;native.Packet=CombinedPacket(true);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectedAllQueryAsync(plan,new[]{native})).ErrorCode);
        native.Packet=CombinedPacket();native.Partial=true;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectedAllQueryAsync(plan,new[]{native})).ErrorCode);
    }
    [Fact]
    public async Task EmptyAllIsTrueOnlyAfterCompleteInputValidation()
    {
        var native=new EntityAllNative{Packet=CombinedPacket()};
        var input=native.Packet.Input;
        native.Packet=new(input,new[]{new CosmicProjectionStageProof(Array.Empty<Guid>())},Array.Empty<CosmicProjectionCompositionRow>(),2,2);
        var read=await Manager().ExecuteProjectedAllQueryAsync(EntityAllPlan(native,99,0),new[]{native});
        Assert.False(read.IsError,read.Message);Assert.True(read.Result.Providers.Single().Value);
    }
    [Fact]
    public async Task AllV2AndCancellationPrecedeDispatch()
    {
        var native=new EntityAllNative{Packet=CombinedPacket()};var plan=EntityAllPlan(native);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED",(await Manager().ExecuteProjectedAllQueryAsync(plan,new[]{native},cancellation.Token)).ErrorCode);
        Assert.Equal("CQL_V2_REQUIRED",(await Manager(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.Legacy).ExecuteProjectedAllQueryAsync(plan,new[]{native})).ErrorCode);
        Assert.Equal(0,native.AllCalls);
    }
}
