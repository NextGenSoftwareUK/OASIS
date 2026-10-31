using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicJoinExecutorTests
{
    public static CosmicQueryCapabilities Sources => new(CosmicQueryFeatures.None,Array.Empty<CosmicQueryFieldCapability>());
    public static CosmicProjectionCapabilities Paths => new(new[] {
        new CosmicProjectionFieldCapability("Name",typeof(string)),
        new CosmicProjectionFieldCapability("Description",typeof(string)),
        new CosmicProjectionFieldCapability("Version",typeof(int)) },Array.Empty<Type>());
    public static CosmicJoinCapabilities Capabilities => new(new[]{CosmicJoinKind.Inner,CosmicJoinKind.Left},Paths,Paths);
    private static readonly Guid Left=Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Right=Guid.Parse("20000000-0000-0000-0000-000000000001");
    public sealed class Dto
    {
        public static int Calls;
        public Dto() { Calls++; }
        public string Label { get; set; } = "";
        public int? Revision { get; set; }
        public bool Present { get; set; }
    }
    private class RowOnly(ProviderType provider):ICosmicQueryStorageProvider
    {
        public virtual ProviderType QueryProviderType=>provider;
        public CosmicQueryCapabilities QueryCapabilities=>Sources;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
            =>throw new InvalidOperationException("Join execution must not hydrate entity rows.");
    }
    private sealed class Native(ProviderType provider):RowOnly(provider),ICosmicQueryJoinStorageProvider
    {
        public ProviderType Identity=provider;
        public override ProviderType QueryProviderType=>Identity;
        public CosmicJoinCapabilities JoinCapabilities { get; set; }=Capabilities;
        public int Calls;
        public bool Loaded=true;
        public bool NullResult;
        public string? Failure;
        public Action? DuringRead;
        public CosmicJoinExecutionLimits? Limits;
        public CosmicJoinPacket Data=Packet();
        public Task<OASISResult<CosmicJoinPacket>> ExecuteCosmicJoinAsync<TLeft,TRight,TKey,TResult>(
            CosmicJoinPlan<TLeft,TRight,TKey,TResult> plan,CosmicJoinExecutionLimits limits,
            CancellationToken cancellationToken=default) where TLeft:IHolon where TRight:IHolon
        {
            Calls++;Limits=limits;DuringRead?.Invoke();
            Assert.Single(plan.LeftSourcePlan.Targets);Assert.Single(plan.RightSourcePlan.Targets);
            return Task.FromResult(NullResult?null!:new OASISResult<CosmicJoinPacket>(Data){
                IsLoaded=Loaded,IsError=Failure!=null,ErrorCode=Failure! });
        }
    }
    public static CosmicJoinPacket Packet(bool invalid=false,bool empty=false)
        =>empty ? new(Array.Empty<CosmicJoinInputRow>(),Array.Empty<CosmicJoinInputRow>(),Array.Empty<CosmicJoinRow>(),0,0,0)
        :new(new[]{new CosmicJoinInputRow(Left,true,Guid.Empty,new object[]{"coffee"},new object[]{"coffee"})},
            new[]{new CosmicJoinInputRow(Right,true,Guid.Empty,new object[]{"coffee"},new object[]{7})},
            new[]{new CosmicJoinRow(Left,Right,"coffee",invalid?7L:(object)7,true)},1,1,1);
    private static CosmicJoinPlan<Holon,Holon,string,Dto> Plan(CosmicJoinOptions? options=null,params ProviderType[] providers)
    {
        var scope=CosmicQueryScope.Named(providers).Result;
        var query=CosmicQuery<Holon>.Create(scope).LeftJoin(CosmicQuery<Holon>.Create(scope),row=>row.Name,row=>row.Name,options)
            .Select((left,right)=>new Dto {Label=left.Description,Revision=(int?)right.Version,Present=right!=null}).Result;
        return CosmicJoinPlanner.Plan(query,providers[0],providers.Select(provider=>new CosmicJoinTarget(provider,Sources,Capabilities)).ToArray()).Result;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task AllActualAdaptersPreflightBeforeFirstDispatch(bool rowOnly)
    {
        var first=new Native(ProviderType.MongoDBOASIS);
        ICosmicQueryStorageProvider second=rowOnly?new RowOnly(ProviderType.Neo4jOASIS)
            :new Native(ProviderType.Neo4jOASIS){JoinCapabilities=new(Array.Empty<CosmicJoinKind>(),Paths,Paths)};
        var result=await CosmicJoinExecutor.ExecuteAsync(Plan(null,first.QueryProviderType,second.QueryProviderType),new[]{first,second});
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",result.ErrorCode);Assert.Equal(0,first.Calls);
    }

    [Fact]
    public async Task LaterInvalidPacketPreventsEveryDtoConstructorAndPublication()
    {
        var first=new Native(ProviderType.MongoDBOASIS);var second=new Native(ProviderType.Neo4jOASIS){Data=Packet(invalid:true)};
        Dto.Calls=0;
        var result=await CosmicJoinExecutor.ExecuteAsync(Plan(null,first.QueryProviderType,second.QueryProviderType),new[]{first,second});
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(0,Dto.Calls);
        Assert.Equal(1,first.Calls);Assert.Equal(1,second.Calls);
    }

    [Fact]
    public async Task SuccessPreservesProviderProvenanceAndDetachedPresence()
    {
        var first=new Native(ProviderType.MongoDBOASIS);var second=new Native(ProviderType.Neo4jOASIS);
        Dto.Calls=0;first.DuringRead=()=>Assert.Equal(0,Dto.Calls);second.DuringRead=()=>Assert.Equal(0,Dto.Calls);
        var result=await CosmicJoinExecutor.ExecuteAsync(Plan(null,first.QueryProviderType,second.QueryProviderType),new[]{first,second});
        Assert.False(result.IsError,result.Message);Assert.True(result.IsLoaded);Assert.Equal(2,Dto.Calls);
        Assert.Equal(new[]{first.QueryProviderType,second.QueryProviderType},result.Result.Providers.Select(provider=>provider.Provider));
        Assert.Equal("coffee",result.Result.Providers[0].Rows.Single().Value.Label);
        Assert.True(result.Result.Providers[0].Rows.Single().HasRight);
        Assert.NotSame(result.Result.Providers[0].Rows.Single().Value,result.Result.Providers[1].Rows.Single().Value);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    [InlineData(4)] [InlineData(5)] [InlineData(6)]
    public async Task AllSevenBudgetsAreConsumedGloballyAcrossProviders(int index)
    {
        var options=index switch {
            0=>CosmicJoinOptions.Create(maximumLeftRows:1).Result,
            1=>CosmicJoinOptions.Create(maximumRightRows:1).Result,
            2=>CosmicJoinOptions.Create(maximumPairs:1).Result,
            3=>CosmicJoinOptions.Create(maximumResults:1).Result,
            4=>CosmicJoinOptions.Create(maximumLeftPayloadBytes:150).Result,
            5=>CosmicJoinOptions.Create(maximumRightPayloadBytes:150).Result,
            _=>CosmicJoinOptions.Create(maximumPayloadBytes:150).Result };
        var first=new Native(ProviderType.MongoDBOASIS);var second=new Native(ProviderType.Neo4jOASIS);
        Dto.Calls=0;
        var result=await CosmicJoinExecutor.ExecuteAsync(Plan(options,first.QueryProviderType,second.QueryProviderType),new[]{first,second});
        Assert.Equal("CQL_RESULT_LIMIT",result.ErrorCode);Assert.Null(result.Result);Assert.Equal(0,Dto.Calls);
        Assert.Equal(1,first.Calls);Assert.Equal(1,second.Calls);
        Assert.NotNull(second.Limits);
    }

    [Fact]
    public async Task ExhaustedBudgetsStillPermitACompleteEmptyLaterProvider()
    {
        var first=new Native(ProviderType.MongoDBOASIS);var second=new Native(ProviderType.Neo4jOASIS){Data=Packet(empty:true)};
        var options=CosmicJoinOptions.Create(maximumLeftRows:1,maximumRightRows:1,maximumPairs:1,maximumResults:1).Result;
        var result=await CosmicJoinExecutor.ExecuteAsync(Plan(options,first.QueryProviderType,second.QueryProviderType),new[]{first,second});
        Assert.False(result.IsError,result.Message);Assert.Empty(result.Result.Providers[1].Rows);
        Assert.Equal(0,second.Limits!.MaximumLeftRows);Assert.Equal(0,second.Limits.MaximumResults);
    }

    [Fact]
    public async Task CancellationAfterReadStopsLaterDispatchAndConstruction()
    {
        using var cancellation=new CancellationTokenSource();
        var first=new Native(ProviderType.MongoDBOASIS){DuringRead=cancellation.Cancel};var second=new Native(ProviderType.Neo4jOASIS);
        Dto.Calls=0;
        var result=await CosmicJoinExecutor.ExecuteAsync(Plan(null,first.QueryProviderType,second.QueryProviderType),new[]{first,second},cancellationToken:cancellation.Token);
        Assert.Equal("CQL_QUERY_CANCELLED",result.ErrorCode);Assert.Equal(0,second.Calls);Assert.Equal(0,Dto.Calls);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CapabilityAndIdentityChangesAreReadmittedBeforeLaterNativeWork(bool identity)
    {
        var second=new Native(ProviderType.Neo4jOASIS);
        var first=new Native(ProviderType.MongoDBOASIS){DuringRead=()=>{
            if(identity)second.Identity=ProviderType.SQLLiteDBOASIS;
            else second.JoinCapabilities=new(Array.Empty<CosmicJoinKind>(),Paths,Paths); }};
        var result=await CosmicJoinExecutor.ExecuteAsync(Plan(null,first.QueryProviderType,second.QueryProviderType),new[]{first,second});
        Assert.Equal(identity?"CQL_PLAN_INVALID":"CQL_CAPABILITY_UNSUPPORTED",result.ErrorCode);Assert.Equal(0,second.Calls);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task UnacknowledgedAndNullProviderResponsesAreNotPartialSuccess(bool missing)
    {
        var first=new Native(ProviderType.MongoDBOASIS){Loaded=false,NullResult=missing};
        var result=await CosmicJoinExecutor.ExecuteAsync(Plan(null,first.QueryProviderType),new[]{first});
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",result.ErrorCode);Assert.Null(result.Result);
    }

    [Fact]
    public async Task ProviderFailureDoesNotFallBackToEntityHydration()
    {
        var first=new Native(ProviderType.MongoDBOASIS){Failure="NATIVE_READ_FAILED"};
        var result=await CosmicJoinExecutor.ExecuteAsync(Plan(null,first.QueryProviderType),new[]{first});
        Assert.Equal("NATIVE_READ_FAILED",result.ErrorCode);Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task NamedRuntimeRecordsUseTheSameExecutionAndValidationOwner(ProviderType provider)
    {
        var native=new Native(provider);var join=Plan(null,provider).Query.Join;
        var selected=join.SelectColumns(new[]{
            CosmicJoinBinding.Left(join,"label",(NextGenSoftware.OASIS.API.Core.Holons.Holon h)=>h.Description).Result,
            CosmicJoinBinding.Right(join,"revision",(NextGenSoftware.OASIS.API.Core.Holons.Holon h)=>h.Version,allowMissing:true).Result,
            CosmicJoinBinding.Presence(join,"present").Result}).Result;
        var plan=CosmicJoinPlanner.Plan(selected,provider,new[]{new CosmicJoinTarget(provider,Sources,Capabilities)});
        Assert.False(plan.IsError,plan.Message);
        var result=await CosmicJoinExecutor.ExecuteAsync(plan.Result,new[]{native});
        Assert.False(result.IsError,result.Message);Assert.Equal(1,native.Calls);
        var row=result.Result.Providers.Single().Rows.Single();
        Assert.True(row.HasRight);Assert.Equal("coffee",row.Value["label"]);Assert.Equal(7,row.Value["revision"]);Assert.Equal(true,row.Value["present"]);
    }

    [Theory]
    [InlineData(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,true)]
    [InlineData(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.Legacy,false)]
    public async Task ManagerExposesJoinExecutionOnlyInV2(string mode,bool supported)
    {
        var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=mode}};
        var manager=new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null,dna,
            new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna));
        var native=new Native(ProviderType.SQLLiteDBOASIS);
        var result=await manager.ExecuteJoinQueryAsync(Plan(null,native.QueryProviderType),new[]{native});
        Assert.Equal(!supported,result.IsError);Assert.Equal(supported?1:0,native.Calls);
        if(!supported)Assert.Equal("CQL_V2_REQUIRED",result.ErrorCode);
    }
}
