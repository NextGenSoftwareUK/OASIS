using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicEntityCountTests
{
    private sealed class Native : ICosmicQueryEntityCountStorageProvider
    {
        public ProviderType Identity=ProviderType.SQLLiteDBOASIS;
        public bool Enabled=true, StageEnabled=true, Loaded=true, Warning, Drift;
        public int Calls;
        public long Count=1;
        public string? Failure;
        public CosmicEntityCompositionPlan<Holon>? Seen;
        public ProviderType QueryProviderType => Identity;
        public CosmicQueryCapabilities QueryCapabilities => new(CosmicQueryFeatures.Paging,
            new[] { new CosmicQueryFieldCapability("Name",typeof(string),new[] { CosmicQueryComparisonOperator.Equal },Array.Empty<CosmicQueryStringOperator>()) });
        public CosmicEntityCompositionCapabilities EntityCompositionCapabilities => new(
            StageEnabled ? CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Paging : CosmicEntityStageFeatures.None,QueryCapabilities);
        public bool SupportsEntityCount { get { if (Drift) Identity=ProviderType.MongoDBOASIS; return Enabled; } }
        public Task<OASISResult<long>> ExecuteCosmicEntityCountAsync<T>(CosmicEntityCompositionPlan<T> plan,CancellationToken cancellationToken=default) where T:IHolon
        {
            Calls++; if(typeof(T)==typeof(Holon)) Seen=(CosmicEntityCompositionPlan<Holon>)(object)plan;
            return Task.FromResult(new OASISResult<long>(Count) { IsLoaded=Loaded,IsWarning=Warning,
                IsError=Failure!=null, ErrorCode=Failure,Message="native count outcome" });
        }
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
            => throw new InvalidOperationException("Count must not read flat rows.");
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicEntityCompositionAsync<T>(CosmicEntityCompositionPlan<T> plan,int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
            => throw new InvalidOperationException("Count must not materialize staged entities.");
    }
    private static CosmicEntityCompositionPlan<Holon> Plan(Native native)
    {
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result).Take(3).Result
            .Compose().Result.Where(row=>row.Name=="coffee").Result.Take(2).Result;
        var plan=CosmicEntityCompositionPlanner.Plan(query,ProviderType.SQLLiteDBOASIS,
            new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS,native.QueryCapabilities),native.EntityCompositionCapabilities);
        Assert.False(plan.IsError,plan.Message); return plan.Result;
    }
    private static HolonManager Manager(string mode=HyperDriveModes.V2)
    {
        var dna=new OASISDNA { OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode=mode } };
        return new HolonManager(null,dna,new ProviderManager(null,dna));
    }
    [Fact]
    public async Task NativeCountPreservesStagesAndNeverMaterializesRows()
    {
        var native=new Native(); var plan=Plan(native);
        var read=await Manager().ExecuteEntityCountQueryAsync(plan,new[] { native });
        Assert.False(read.IsError,read.Message+read.Exception); Assert.True(read.IsLoaded);
        Assert.Equal(1,read.Result.Providers.Single().Value); Assert.Equal(1,native.Calls);
        Assert.Same(plan.Query,native.Seen!.Query); Assert.Equal(3,native.Seen.Query.Source.Limit);
        Assert.Equal(2,native.Seen.Query.Stages.Count);
    }
    [Fact]
    public async Task ActualNativeCountAndStageCapabilitiesAreReadmittedBeforeIo()
    {
        var native=new Native(); var plan=Plan(native); native.Enabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await CosmicEntityCountExecutor.ExecuteAsync(plan,new[] { native })).ErrorCode);
        native.Enabled=true; native.StageEnabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await CosmicEntityCountExecutor.ExecuteAsync(plan,new[] { native })).ErrorCode);
        Assert.Equal(0,native.Calls);
    }
    [Fact]
    public async Task PartialWarningNegativeAndExcessiveCountsCannotBecomeSuccess()
    {
        var native=new Native(); var plan=Plan(native);
        foreach(var count in new[] { -1L,3L,1001L })
        {
            native.Count=count;
            Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await CosmicEntityCountExecutor.ExecuteAsync(plan,new[] { native })).ErrorCode);
        }
        native.Count=0; native.Loaded=false;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await CosmicEntityCountExecutor.ExecuteAsync(plan,new[] { native })).ErrorCode);
        native.Loaded=true; native.Warning=true;
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await CosmicEntityCountExecutor.ExecuteAsync(plan,new[] { native })).ErrorCode);
        native.Warning=false; native.Failure="CQL_QUERY_LIMIT";
        Assert.Equal("CQL_QUERY_LIMIT",(await CosmicEntityCountExecutor.ExecuteAsync(plan,new[] { native })).ErrorCode);
    }
    [Fact]
    public async Task V2CancellationAndPhysicalIdentityAreOwnedBeforeIo()
    {
        var native=new Native(); var plan=Plan(native);
        Assert.Equal("CQL_V2_REQUIRED",(await Manager(HyperDriveModes.Legacy).ExecuteEntityCountQueryAsync(plan,new[] { native })).ErrorCode);
        using var cancel=new CancellationTokenSource(); cancel.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED",(await CosmicEntityCountExecutor.ExecuteAsync(plan,new[] { native },cancel.Token)).ErrorCode);
        native.Drift=true;
        Assert.Equal("CQL_EXECUTION_INVALID",(await CosmicEntityCountExecutor.ExecuteAsync(plan,new[] { native })).ErrorCode);
        Assert.Equal(0,native.Calls);
    }
    [Fact]
    public async Task AdapterSnapshotCannotDuplicateOrReplaceTheSelectedIdentity()
    {
        var native=new Native(); var plan=Plan(native);
        Assert.Equal("CQL_EXECUTION_INVALID",(await CosmicEntityCountExecutor.ExecuteAsync(plan,new[] { native,native })).ErrorCode);
        Assert.Equal("CQL_PLAN_TARGET_UNAVAILABLE",(await CosmicEntityCountExecutor.ExecuteAsync(plan,Array.Empty<ICosmicQueryStorageProvider>())).ErrorCode);
        Assert.Equal(0,native.Calls);
    }
}
