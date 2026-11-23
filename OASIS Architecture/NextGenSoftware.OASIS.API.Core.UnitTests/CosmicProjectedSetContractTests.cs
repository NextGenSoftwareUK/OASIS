using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicProjectedSetContractTests
{
    private static CosmicQuery<Holon> Root(Guid actor)=>CosmicQuery<Holon>
        .Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
    [Fact]
    public void SelectedSetsKeepBothInputsAndCaptureOutputStagesWithoutSourceWindowLeakage()
    {
        var root=Root(Guid.NewGuid());
        var left=root.Take(2).Result.Select(h=>h.Name).Result;
        var right=root.Skip(1).Result.Take(3).Result.Select(h=>h.Name).Result.Skip(1).Result;
        foreach(var kind in Enum.GetValues<CosmicSetQueryKind>())
        {
            var result=CosmicProjectedSetQuery<Holon,string>.Create(left,right,kind);
            Assert.False(result.IsError,result.Message);
            var set=result.Result.Where(value=>value=="coffee").Result.Skip(1).Result.Take(2).Result;
            Assert.Same(left,set.Left);Assert.Same(right,set.Right);
            Assert.Equal(kind,set.Kind);Assert.Equal(typeof(string),Assert.Single(set.ValueTypes));
            Assert.Equal(2,set.Left.Source.Limit);Assert.Equal(3,set.Right.Source.Limit);
            Assert.Single(set.Right.ResultStages);
            Assert.Null(set.Output.Source.Limit);Assert.Equal(0,set.Output.Source.Offset);
            Assert.Empty(set.Output.Source.BusinessPredicates);
            Assert.Equal(3,set.Output.ResultStages.Count);
            Assert.Empty(result.Result.Output.ResultStages);
        }
    }
    public sealed class Model
    {
        public static int Constructions;
        public Model(){Constructions++;}
        public string A {get;set;} = "";
        public string B {get;set;} = "";
    }
    [Fact]
    public void IncompatibleMemberBindingsRejectWithoutConstructingUserDtos()
    {
        Model.Constructions=0;var root=Root(Guid.NewGuid());
        var left=root.Select(h=>new Model {A=h.Name}).Result;
        var right=root.Select(h=>new Model {B=h.Name}).Result;
        var rejected=CosmicProjectedSetQuery<Holon,Model>.Create(left,right,CosmicSetQueryKind.Union);
        Assert.Equal("CQL_SET_VALUE_SHAPE_MISMATCH",rejected.ErrorCode);
        var admitted=CosmicProjectedSetQuery<Holon,Model>.Create(left,left,CosmicSetQueryKind.Concat);
        Assert.False(admitted.IsError,admitted.Message);Assert.Equal(0,Model.Constructions);
    }
    [Fact]
    public void BothSelectedInputsMustShareOneExplicitActorAndAuthority()
    {
        var left=Root(Guid.NewGuid()).Select(h=>h.Name).Result;
        var right=Root(Guid.NewGuid()).Select(h=>h.Name).Result;
        Assert.Equal("CQL_SET_ACCESS_MISMATCH",
            CosmicProjectedSetQuery<Holon,string>.Create(left,right,CosmicSetQueryKind.Union).ErrorCode);
        Assert.Equal("CQL_SET_QUERY_INVALID",
            CosmicProjectedSetQuery<Holon,string>.Create(null!,right,CosmicSetQueryKind.Union).ErrorCode);
        Assert.Equal("CQL_SET_QUERY_INVALID",
            CosmicProjectedSetQuery<Holon,string>.Create(left,left,(CosmicSetQueryKind)99).ErrorCode);
    }
}
