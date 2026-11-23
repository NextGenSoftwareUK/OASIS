using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CosmicProjectedSetProofTests
{
    private static readonly Guid Actor=Guid.Parse("10000000-0000-0000-0000-000000000000");
    private static Guid Id(int value)=>Guid.Parse("00000000-0000-0000-0000-"+value.ToString("D12"));
    private static CosmicProjectedSetOccurrence L(int index)=>new(false,index);
    private static CosmicProjectedSetOccurrence R(int index)=>new(true,index);
    private static CosmicProjectedSetStageProof Stage(params CosmicProjectedSetOccurrence[] ids)=>new(ids);
    private static CosmicProjectedSetQuery<Holon,string> Query(CosmicSetQueryKind kind)
    {
        var root=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MongoDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Actor).Result).Result;
        var selected=root.Select(h=>h.Name).Result;
        return CosmicProjectedSetQuery<Holon,string>.Create(selected,selected,kind).Result;
    }
    private static CosmicProjectionCompositionPacket Input(bool right,bool forbidden=false)
    {
        object[] values=right?new object[]{"a","b"}:new object[]{"a",null!};
        var ids=right?new[]{Id(3),Id(4)}:new[]{Id(1),Id(2)};
        return new(
            ids.Select((id,index)=>new CosmicProjectionInputRow(id,false,forbidden?Guid.Empty:Actor,
                new[]{values[index]},Array.Empty<object>())).ToArray(),
            Array.Empty<CosmicProjectionStageProof>(),
            ids.Select((id,index)=>new CosmicProjectionCompositionRow(id,values[index])).ToArray(),2,0);
    }
    private static CosmicProjectedSetExecutionLimits Limits(int inputBytes=1048576)
        =>new(new CosmicProjectedSetOptions(new CosmicSetQueryOptions(maximumInputPayloadBytes:inputBytes)));
    [Fact]
    public void AllFourNativeOperationsUseTupleValuesAcrossDistinctSourceIdsAndNulls()
    {
        foreach(var kind in Enum.GetValues<CosmicSetQueryKind>())
        {
            var members=kind switch{
                CosmicSetQueryKind.Union=>new[]{L(0),L(1),R(1)},
                CosmicSetQueryKind.Concat=>new[]{L(0),L(1),R(0),R(1)},
                CosmicSetQueryKind.Intersect=>new[]{L(0)},
                _=>new[]{L(1)}};
            object Value(CosmicProjectedSetOccurrence id)=>id.IsRight?(id.Index==0?"a":"b"):(id.Index==0?"a":null!);
            var packet=new CosmicProjectedSetPacket(Input(false),Input(true),new(members),
                Array.Empty<CosmicProjectedSetStageProof>(),members.Select(id=>new CosmicProjectionRow(Value(id))).ToArray(),4);
            var result=CosmicProjectedSetResults.Materialize(Query(kind),packet,Limits());
            Assert.False(result.IsError,result.Message);
            Assert.Equal(members.Select(Value),result.Result.Cast<object>());
        }
    }
    [Fact]
    public void GlobalStagesAndExactPayloadMustMatchAcknowledgedNativeProof()
    {
        var query=Query(CosmicSetQueryKind.Union).OrderBy(value=>value).Result.Skip(1).Result.Take(1).Result;
        var stages=new[]{Stage(L(1),L(0),R(1)),Stage(L(0),R(1)),Stage(L(0))};
        CosmicProjectedSetPacket Packet(string value,CosmicProjectedSetStageProof[] proof)=>new(Input(false),Input(true),
            Stage(L(0),L(1),R(1)),proof,new[]{new CosmicProjectionRow(value)},12);
        var result=CosmicProjectedSetResults.Materialize(query,Packet("a",stages),Limits());
        Assert.False(result.IsError,result.Message);Assert.Equal("a",Assert.Single(result.Result));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",
            CosmicProjectedSetResults.Materialize(query,Packet("altered",stages),Limits()).ErrorCode);
        var corrupt=(CosmicProjectedSetStageProof[])stages.Clone();corrupt[1]=Stage(R(1),L(0));
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",
            CosmicProjectedSetResults.Materialize(query,Packet("a",corrupt),Limits()).ErrorCode);
    }
    [Fact]
    public void ZeroOutputCannotHideInaccessibleRightInputOrCombinedInputBudget()
    {
        var query=Query(CosmicSetQueryKind.Union).Take(0).Result;
        CosmicProjectedSetPacket Packet(bool forbidden)=>new(Input(false),Input(true,forbidden),
            Stage(L(0),L(1),R(1)),new[]{Stage()},Array.Empty<CosmicProjectionRow>(),7);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",
            CosmicProjectedSetResults.Materialize(query,Packet(true),Limits()).ErrorCode);
        Assert.Equal("CQL_RESULT_LIMIT",
            CosmicProjectedSetResults.Materialize(query,Packet(false),Limits(150)).ErrorCode);
    }
}
