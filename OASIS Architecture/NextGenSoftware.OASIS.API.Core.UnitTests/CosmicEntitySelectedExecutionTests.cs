using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityProjectionTests
{
    private class EntitySelectedNative:Native,ICosmicQueryEntityProjectionCardinalityStorageProvider
    {
        public bool Combined=true,CardinalityEnabled=true,CardinalityProfile=true;
        public bool SupportsEntityProjectionCardinality=>CardinalityEnabled;
        public bool Complete=true,Partial;
        public int CombinedCalls;
        public CosmicProjectionCompositionPlan<Holon,ProjectionDto>? CombinedSeen;
        public CosmicProjectionCompositionPacket Packet=null!;
        public bool SupportsEntityProjectionComposition=>Combined;
        public virtual CosmicProjectionCompositionCapabilities CompositionCapabilities=>new(
            CosmicSelectedStageFeatures.Paging,CosmicQueryFeatures.None,
            Array.Empty<CosmicQueryMetadataCapability>(),Array.Empty<Type>(),32,false,CardinalityProfile);
        public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T,TResult>(
            CosmicProjectionCompositionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,CancellationToken cancellationToken=default) where T:IHolon
            =>throw new Exception("The flat selected owner cannot substitute for entity-selected execution.");
        public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicEntityProjectionCompositionAsync<T,TResult>(
            CosmicProjectionCompositionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,CancellationToken cancellationToken=default) where T:IHolon
        {
            CombinedCalls++;
            if(typeof(T)==typeof(Holon) && typeof(TResult)==typeof(ProjectionDto))
                CombinedSeen=(CosmicProjectionCompositionPlan<Holon,ProjectionDto>)(object)plan;
            return Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>(Packet){IsLoaded=Complete,IsWarning=Partial});
        }
    }
    private static readonly Guid Low=Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid High=Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static CosmicProjectionCompositionPlan<Holon,ProjectionDto> CombinedPlan(EntitySelectedNative native,int entityLimit=3)
    {
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result).Take(3).Result
            .Compose().Result.Reverse().Result.Take(entityLimit).Result;
        var selected=source.Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version}).Result.Take(1).Result;
        var planned=CosmicProjectionCompositionPlanner.PlanEntity(selected,native.QueryProviderType,
            new CosmicProjectionCompositionTarget(native.QueryProviderType,native.QueryCapabilities,native.ProjectionCapabilities,native.CompositionCapabilities),
            native.EntityCompositionCapabilities);
        Assert.False(planned.IsError,planned.Message);return planned.Result;
    }
    private static CosmicProjectionCompositionPacket CombinedPacket(bool invalidHidden=false,long? nativeCount=null)
        =>new(new[]{
            new CosmicProjectionInputRow(High,true,Guid.Empty,new object[]{"high",7},Array.Empty<object>()),
            new CosmicProjectionInputRow(Low,true,Guid.Empty,new object[]{"low",invalidHidden?(object)"wrong":6},Array.Empty<object>())},
            new[]{new CosmicProjectionStageProof(new[]{High})},new[]{new CosmicProjectionCompositionRow(High,"high",7)},2,2,nativeCount);

    [Fact]
    public async Task StandardSelectedStagesUseTheCombinedOwnerAndRegisteredRoot()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket()};
        var selected=LinqRoot(native).Take(3).Reverse().Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        // Combined enrollment is independent of the final-only projection contract.
        native.Enabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>selected.ToArray()).Code);
        var wrapped=selected.Provider.CreateQuery<ProjectionDto>(selected.Expression);
        var nonGeneric=(IQueryable<ProjectionDto>)selected.Provider.CreateQuery(selected.Expression);
        var read=await wrapped.Take(1).ToCosmicListAsync();
        Assert.False(read.IsError,read.Message+read.Exception);
        Assert.Equal("high",Assert.Single(read.Result).Name);
        Assert.Equal("high",Assert.Single(nonGeneric.Take(1).ToArray()).Name);
        Assert.Equal(2,native.CombinedCalls);Assert.Equal(0,native.Calls);
    }

    [Fact]
    public async Task CombinedNativeOwnerPreservesEntityOrderBeforeSelectedWindows()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket()};var plan=CombinedPlan(native);
        ProjectionDto.Constructors=0;
        var read=await Manager().ExecuteProjectionCompositionQueryAsync(plan,new[]{native});
        Assert.False(read.IsError,read.Message+read.Exception);
        Assert.Equal("high",read.Result.Providers.Single().Rows.Single().Name);
        Assert.Equal(1,ProjectionDto.Constructors);Assert.Equal(1,native.CombinedCalls);
        Assert.Same(plan.EntitySource,native.CombinedSeen!.EntitySource);
        Assert.True(CosmicProjectionCompositionPlanner.Admit(plan,native.QueryProviderType,native.QueryCapabilities,
            native.ProjectionCapabilities,native.CompositionCapabilities).IsError);
        // Flat packets still require their own source order; only combined native admission changes this invariant.
        Assert.True(CosmicProjectionCompositionResults.Materialize(plan.Query,native.Packet).IsError);
    }
    [Fact]
    public async Task CombinedProofValidatesHiddenCellsAndEntityOutputBoundsBeforeDtos()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket(true)};var plan=CombinedPlan(native);
        ProjectionDto.Constructors=0;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectionCompositionQueryAsync(plan,new[]{native})).ErrorCode);
        native.Packet=CombinedPacket();plan=CombinedPlan(native,1);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectionCompositionQueryAsync(plan,new[]{native})).ErrorCode);
        Assert.Equal(0,ProjectionDto.Constructors);
    }
    [Fact]
    public async Task ActualIndependentEnrollmentAndV2AreRequiredBeforeDispatch()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket()};var plan=CombinedPlan(native);
        native.Combined=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await Manager().ExecuteProjectionCompositionQueryAsync(plan,new[]{native})).ErrorCode);
        native.Combined=true;native.StagesEnabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await Manager().ExecuteProjectionCompositionQueryAsync(plan,new[]{native})).ErrorCode);
        native.StagesEnabled=true;
        Assert.Equal("CQL_V2_REQUIRED",(await Manager(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.Legacy).ExecuteProjectionCompositionQueryAsync(plan,new[]{native})).ErrorCode);
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED",(await Manager().ExecuteProjectionCompositionQueryAsync(plan,new[]{native},cancellationToken:cancellation.Token)).ErrorCode);
        Assert.Equal(0,native.CombinedCalls);
    }
    [Fact]
    public async Task IncompleteOrWarningCombinedProofCannotConstructDtos()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket(),Complete=false};var plan=CombinedPlan(native);
        ProjectionDto.Constructors=0;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectionCompositionQueryAsync(plan,new[]{native})).ErrorCode);
        native.Complete=true;native.Partial=true;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await Manager().ExecuteProjectionCompositionQueryAsync(plan,new[]{native})).ErrorCode);
        Assert.Equal(0,ProjectionDto.Constructors);
    }
}
