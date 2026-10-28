using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityProjectionTests
{
    public sealed class ProjectionDto
    {
        public static int Constructors;
        public ProjectionDto(){Constructors++;}
        public string Name {get;set;}="";
        public int Revision {get;set;}
    }
    private sealed class Native:ICosmicQueryEntityProjectionStorageProvider
    {
        public bool Enabled=true,StagesEnabled=true,Loaded=true,Warning;
        public int Calls;
        public CosmicEntityProjectionPlan<Holon,ProjectionDto>? Seen;
        public IReadOnlyList<CosmicProjectionRow> Rows=new[] {new CosmicProjectionRow("coffee",7)};
        public ProviderType QueryProviderType=>ProviderType.SQLLiteDBOASIS;
        public bool SupportsEntityProjection=>Enabled;
        public CosmicQueryCapabilities QueryCapabilities=>new(CosmicQueryFeatures.Paging,
            new[] {new CosmicQueryFieldCapability("Name",typeof(string),new[]{CosmicQueryComparisonOperator.Equal},Array.Empty<CosmicQueryStringOperator>())});
        public CosmicProjectionCapabilities ProjectionCapabilities=>new(new[] {
            new CosmicProjectionFieldCapability("Name",typeof(string)),new CosmicProjectionFieldCapability("Version",typeof(int))},Array.Empty<Type>());
        public CosmicEntityCompositionCapabilities EntityCompositionCapabilities=>new(StagesEnabled
            ? CosmicEntityStageFeatures.Filter|CosmicEntityStageFeatures.Paging|CosmicEntityStageFeatures.Reverse:CosmicEntityStageFeatures.None,QueryCapabilities);
        public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicEntityProjectionAsync<T,TResult>(CosmicEntityProjectionPlan<T,TResult> plan,
            int maximumResults=100,int maximumPayloadBytes=CosmicProjectionResults.MaximumPayloadLimit,CancellationToken cancellationToken=default) where T:IHolon
        {
            Calls++;if(typeof(T)==typeof(Holon)&&typeof(TResult)==typeof(ProjectionDto))Seen=(CosmicEntityProjectionPlan<Holon,ProjectionDto>)(object)plan;
            return Task.FromResult(new OASISResult<IReadOnlyList<CosmicProjectionRow>>(Rows){IsLoaded=Loaded,IsWarning=Warning});
        }
        public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicProjectionAsync<T,TResult>(CosmicProjectionPlan<T,TResult> plan,int maximumResults=100,
            int maximumPayloadBytes=CosmicProjectionResults.MaximumPayloadLimit,CancellationToken cancellationToken=default) where T:IHolon=>throw new Exception("No flat projection substitution.");
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon=>throw new Exception("No flat entity read.");
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicEntityCompositionAsync<T>(CosmicEntityCompositionPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon=>throw new Exception("No entity materialization for projection.");
    }
    private static HolonManager Manager(string mode=HyperDriveModes.V2)
    {
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=mode}};
        return new HolonManager(null,dna,new ProviderManager(null,dna));
    }
    private static CosmicEntityProjectionPlan<Holon,ProjectionDto> Plan(Native native)
    {
        var source=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result).Take(3).Result
            .Compose().Result.Where(row=>row.Name=="coffee").Result.Reverse().Result.Take(1).Result;
        var query=source.Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        Assert.False(query.IsError,query.Message);
        var plan=CosmicEntityProjectionPlanner.Plan(query.Result,native.QueryProviderType,
            new CosmicProjectionTarget(native.QueryProviderType,native.QueryCapabilities,native.ProjectionCapabilities),native.EntityCompositionCapabilities);
        Assert.False(plan.IsError,plan.Message);return plan.Result;
    }
    [Fact]
    public async Task ImmutableEntitySourceAndNativeCellsUseTheExistingProjectionOwner()
    {
        ProjectionDto.Constructors=0;var native=new Native();var plan=Plan(native);
        Assert.Equal(0,ProjectionDto.Constructors);Assert.Equal(3,plan.Query.Source.Source.Limit);
        Assert.Equal(3,plan.Query.Source.Stages.Count);Assert.Same(plan.Query.Source.Source,plan.Selection.Query.Source);
        var read=await Manager().ExecuteEntityProjectionQueryAsync(plan,new[]{native});
        Assert.False(read.IsError,read.Message+read.Exception);Assert.Equal("coffee",read.Result.Providers.Single().Rows.Single().Name);
        Assert.Equal(7,read.Result.Providers.Single().Rows.Single().Revision);Assert.Equal(1,ProjectionDto.Constructors);
        Assert.Same(plan.Query,native.Seen!.Query);Assert.Equal(1,native.Calls);
    }
    [Fact]
    public async Task InvalidCellsAndImpossibleWindowCountsAreRejectedBeforeDtoConstruction()
    {
        var native=new Native();var plan=Plan(native);ProjectionDto.Constructors=0;
        native.Rows=new[]{new CosmicProjectionRow("coffee","wrong domain")};
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await CosmicEntityProjectionExecutor.ExecuteAsync(plan,new[]{native})).ErrorCode);
        native.Rows=new[]{new CosmicProjectionRow("a",1),new CosmicProjectionRow("b",2)};
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await CosmicEntityProjectionExecutor.ExecuteAsync(plan,new[]{native})).ErrorCode);
        Assert.Equal(0,ProjectionDto.Constructors);
    }
    [Fact]
    public async Task ActualStageAndProjectionEnrollmentAreRequiredBeforeIo()
    {
        var native=new Native();var plan=Plan(native);native.Enabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await CosmicEntityProjectionExecutor.ExecuteAsync(plan,new[]{native})).ErrorCode);
        native.Enabled=true;native.StagesEnabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await CosmicEntityProjectionExecutor.ExecuteAsync(plan,new[]{native})).ErrorCode);
        Assert.Equal(0,native.Calls);
    }
    [Fact]
    public async Task V2CancellationAndIncompleteNativeProofCannotConstructDtos()
    {
        var native=new Native();var plan=Plan(native);ProjectionDto.Constructors=0;
        Assert.Equal("CQL_V2_REQUIRED",(await Manager(HyperDriveModes.Legacy).ExecuteEntityProjectionQueryAsync(plan,new[]{native})).ErrorCode);
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED",(await CosmicEntityProjectionExecutor.ExecuteAsync(plan,new[]{native},cancellationToken:cancel.Token)).ErrorCode);
        Assert.Equal(0,native.Calls);native.Loaded=false;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await CosmicEntityProjectionExecutor.ExecuteAsync(plan,new[]{native})).ErrorCode);
        native.Loaded=true;native.Warning=true;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await CosmicEntityProjectionExecutor.ExecuteAsync(plan,new[]{native})).ErrorCode);
        Assert.Equal(0,ProjectionDto.Constructors);
    }
}
