using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityCompositionTests
{
    public sealed class EntityElementRow : Holon
    {
        public static int Constructors;
        public EntityElementRow() { Constructors++; }
        [CustomOASISProperty] public int Rating { get; set; }
    }
    private static CosmicEntityCompositionQuery<T> ElementSource<T>() where T:NextGenSoftware.OASIS.API.Core.Interfaces.IHolon
        => CosmicQuery<T>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result).Take(3).Result.Compose().Result.Skip(0).Result;
    private static CosmicEntityElementPlan<T> ElementPlan<T>(Native native,CosmicElementOperation operation,int? index=null)
        where T:NextGenSoftware.OASIS.API.Core.Interfaces.IHolon
    {
        var planned=CosmicEntityElementPlanner.Plan(ElementSource<T>(),operation,native.QueryProviderType,
            new CosmicQueryTarget(native.QueryProviderType,native.QueryCapabilities),native.EntityCompositionCapabilities,index);
        Assert.False(planned.IsError,planned.Message);return planned.Result;
    }
    [Fact]
    public async Task StagedSingleEstablishesCardinalityBeforeApplicationConstruction()
    {
        var native=Data("a","b");EntityElementRow.Constructors=0;
        var plan=ElementPlan<EntityElementRow>(native,CosmicElementOperation.Single);
        Assert.Equal(2,Assert.IsType<CosmicEntityWindowStage>(plan.Rows.Query.Stages.Last()).Count);
        var read=await Manager().ExecuteEntityElementQueryAsync(plan,new[] {native});
        Assert.Equal("CQL_SEQUENCE_MULTIPLE",read.ErrorCode);Assert.Equal(0,EntityElementRow.Constructors);
        Assert.Equal(0,native.RowCalls);Assert.Equal(1,native.StageCalls);
    }
    [Fact]
    public async Task StagedFirstUsesTheExistingTypedHydrationAndTrackingOwner()
    {
        var native=new Native();native.Value.MetaData=new() { ["Rating"]=7 };EntityElementRow.Constructors=0;
        var read=await Manager().ExecuteEntityElementQueryAsync(ElementPlan<EntityElementRow>(native,CosmicElementOperation.First),new[] {native});
        Assert.False(read.IsError,read.Message+read.Exception);Assert.Equal(7,read.Result.Rating);
        Assert.True(EntityElementRow.Constructors>0);Assert.False(HolonChangeTracking.HasChanges(read.Result));
        read.Result.Rating=8;Assert.True(HolonChangeTracking.HasChanges(read.Result));
        Assert.Equal(7,native.Value.MetaData["Rating"]);Assert.Equal(0,native.RowCalls);
    }
    [Fact]
    public async Task StagedIndexAndDefaultsPreserveNativeStageWindows()
    {
        var native=Data("a","b");
        var second=await Manager().ExecuteEntityElementQueryAsync(ElementPlan<Holon>(native,CosmicElementOperation.ElementAt,1),new[] {native});
        Assert.False(second.IsError,second.Message);Assert.Equal("b",second.Result.Name);
        var absent=await Manager().ExecuteEntityElementQueryAsync(ElementPlan<Holon>(native,CosmicElementOperation.ElementAtOrDefault,9),new[] {native});
        Assert.False(absent.IsError,absent.Message);Assert.True(absent.IsLoaded);Assert.Null(absent.Result);
        native.Data=Array.Empty<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>();
        var empty=await Manager().ExecuteEntityElementQueryAsync(ElementPlan<Holon>(native,CosmicElementOperation.First),new[] {native});
        Assert.Equal("CQL_SEQUENCE_EMPTY",empty.ErrorCode);
        var optional=await Manager().ExecuteEntityElementQueryAsync(ElementPlan<Holon>(native,CosmicElementOperation.SingleOrDefault),new[] {native});
        Assert.False(optional.IsError,optional.Message);Assert.Null(optional.Result);
    }
    [Fact]
    public async Task StagedElementGatesDoNotSubstituteFlatReads()
    {
        var native=new Native();var plan=ElementPlan<Holon>(native,CosmicElementOperation.First);
        Assert.Equal("CQL_V2_REQUIRED",(await Manager(HyperDriveModes.Legacy).ExecuteEntityElementQueryAsync(plan,new[] {native})).ErrorCode);
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED",(await Manager().ExecuteEntityElementQueryAsync(plan,new[] {native},cancel.Token)).ErrorCode);
        var tail=CosmicEntityElementPlanner.Plan(ElementSource<Holon>(),CosmicElementOperation.Last,native.QueryProviderType,
            new CosmicQueryTarget(native.QueryProviderType,native.QueryCapabilities),native.EntityCompositionCapabilities);
        Assert.Equal("CQL_ORDER_REQUIRED",tail.ErrorCode);
        native.Enabled=false;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await Manager().ExecuteEntityElementQueryAsync(plan,new[] {native})).ErrorCode);
        Assert.Equal(0,native.StageCalls);Assert.Equal(0,native.RowCalls);
    }
}
