using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlSetOrderingTests
{
    [Fact]
    public void OutputOrderingPrecedesOuterWindowAndKeepsBothInputWindows()
    {
        var request=new CqlSetV1Request {Kind="Concat",Left=new(){Skip=1,Take=3},Right=new(){Skip=2,Take=4},
            Ordering=new[]{new CqlV1Ordering {Field="Name",Descending=true},new CqlV1Ordering {Field="Version"}},
            Skip=1,Take=2};
        var query=CqlV1Compiler.CompileSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.Equal(1,query.Left.Offset);Assert.Equal(3,query.Left.Limit);
        Assert.Equal(2,query.Right.Offset);Assert.Equal(4,query.Right.Limit);
        Assert.Equal(3,query.Output.Stages.Count);
        var order=Assert.IsType<CosmicEntityOrderStage>(query.Output.Stages[0]);
        Assert.Equal(new[]{"Name","Version","Id"},order.Orderings.Select(term=>Assert.Single(term.Path.Segments)));
        Assert.True(order.Orderings[0].Descending);Assert.False(order.Orderings[1].Descending);
        Assert.Equal(1,Assert.IsType<CosmicEntityWindowStage>(query.Output.Stages[1]).Count);
        Assert.Equal(2,Assert.IsType<CosmicEntityWindowStage>(query.Output.Stages[2]).Count);
    }
    [Fact]
    public void SetOrderingRejectsDuplicateUnknownAndEmptyTerms()
    {
        foreach(var terms in new[]{
            Array.Empty<CqlV1Ordering>(),
            new[]{new CqlV1Ordering {Field="Name"},new CqlV1Ordering {Field="Name"}},
            new[]{new CqlV1Ordering {Field="ProviderUniqueStorageKey"}}})
            Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSet(new(){Ordering=terms},
                Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
    [Fact]
    public void SetOrderingCaptureDetachesTermsAndExplicitIdIsNotDuplicated()
    {
        var term=new CqlV1Ordering {Field="Id",Descending=true};
        var request=new CqlSetV1Request {Ordering=new[]{term}};
        var captured=CqlRequestCapture.Capture(request);
        term.Field="Name";request.Ordering[0]=new(){Field="Version"};
        Assert.Equal("Id",captured.Ordering![0].Field);Assert.True(captured.Ordering[0].Descending);
        var query=CqlV1Compiler.CompileSet(captured,Guid.NewGuid(),ProviderType.SQLLiteDBOASIS);
        Assert.Single(Assert.IsType<CosmicEntityOrderStage>(Assert.Single(query.Output.Stages)).Orderings);
    }
}
