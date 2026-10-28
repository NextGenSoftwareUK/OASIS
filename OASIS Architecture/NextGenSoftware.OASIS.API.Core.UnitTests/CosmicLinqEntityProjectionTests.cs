using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityProjectionTests
{
    private static IQueryable<Holon> LinqRoot(Native native,string mode=HyperDriveModes.V2)
        =>CosmicLinq.Create(Manager(mode),native,CosmicQuery<Holon>.Create(CosmicQueryScope.Named(native.QueryProviderType).Result)).Result;

    [Fact]
    public async Task StandardSelectIsDeferredAndRetainsEntityStagesForSyncAndAsyncReads()
    {
        ProjectionDto.Constructors=0;var native=new Native();
        var query=LinqRoot(native).Take(3).Where(row=>row.Name=="coffee").Reverse().Take(1)
            .Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        Assert.Equal(0,native.Calls);Assert.Equal(0,ProjectionDto.Constructors);
        var read=await query.ToCosmicListAsync();Assert.False(read.IsError,read.Message+read.Exception);
        Assert.Equal("coffee",Assert.Single(read.Result).Name);Assert.Equal(3,native.Seen!.Source.Query.Source.Limit);
        Assert.Equal(3,native.Seen.Source.Query.Stages.Count);Assert.Equal(1,ProjectionDto.Constructors);
        Assert.Equal(7,Assert.Single(query.ToArray()).Revision);Assert.Equal(2,native.Calls);
    }

    [Fact]
    public async Task EntitySelectRebindsClosuresForEveryExecution()
    {
        var native=new Native();var desired="coffee";
        var query=LinqRoot(native).Take(3).Where(row=>row.Name==desired)
            .Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        desired="tea";Assert.False((await query.ToCosmicListAsync()).IsError);
        var first=Assert.IsType<CosmicEntityFilterStage<Holon>>(native.Seen!.Source.Query.Stages[0]);
        Assert.Equal("tea",Assert.IsType<CosmicQueryComparison>(Assert.Single(first.Predicate.Predicates)).Literal.Value);
        desired="water";query.ToArray();
        var second=Assert.IsType<CosmicEntityFilterStage<Holon>>(native.Seen!.Source.Query.Stages[0]);
        Assert.Equal("water",Assert.IsType<CosmicQueryComparison>(Assert.Single(second.Predicate.Predicates)).Literal.Value);
    }

    [Fact]
    public async Task EntitySelectRequiresActualEnrollmentAndV2BeforeIo()
    {
        var native=new Native();var query=LinqRoot(native).Take(3).Where(row=>row.Name=="coffee")
            .Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        native.Enabled=false;Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await query.ToCosmicListAsync()).ErrorCode);
        native.Enabled=true;native.StagesEnabled=false;Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",(await query.ToCosmicListAsync()).ErrorCode);
        native.StagesEnabled=true;
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        Assert.Equal("CQL_QUERY_CANCELLED",(await query.ToCosmicListAsync(cancel.Token)).ErrorCode);
        var legacy=LinqRoot(native,HyperDriveModes.Legacy).Take(3).Where(row=>row.Name=="coffee")
            .Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        Assert.Equal("CQL_V2_REQUIRED",(await legacy.ToCosmicListAsync()).ErrorCode);Assert.Equal(0,native.Calls);
    }

    [Fact]
    public async Task EntitySelectValidatesAllCellsBeforeConstructingApplicationDtos()
    {
        ProjectionDto.Constructors=0;var native=new Native{Rows=new[]{new CosmicProjectionRow("coffee","invalid")}};
        var query=LinqRoot(native).Take(3).Where(row=>row.Name=="coffee")
            .Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await query.ToCosmicListAsync()).ErrorCode);
        Assert.Equal(0,ProjectionDto.Constructors);Assert.Equal(1,native.Calls);
    }

    [Fact]
    public void UnsupportedPostSelectionOperatorsCannotSubstituteFlatSourceExecution()
    {
        var native=new Native();var entity=LinqRoot(native).Take(3).Where(row=>row.Name=="coffee");
        var query=entity.Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.Where(row=>row.Name=="coffee").ToArray()).Code);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.Count()).Code);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>query.Sum(row=>row.Revision)).Code);
        Assert.Equal("CQL_COMPOSITION_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>entity.Select((row,index)=>row.Name)).Code);
        Assert.Equal(0,native.Calls);
    }

    [Fact]
    public void NonGenericQueryProviderSelectUsesTheSameEntityProjectionContract()
    {
        var native=new Native();var entity=LinqRoot(native).Take(3).Where(row=>row.Name=="coffee");
        var expression=entity.Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version}).Expression;
        var query=Assert.IsAssignableFrom<IQueryable<ProjectionDto>>(entity.Provider.CreateQuery(expression));
        Assert.Equal(7,Assert.Single(query.ToArray()).Revision);Assert.Equal(1,native.Calls);
    }
}
