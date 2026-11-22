using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicJoinEntityPairTests
{
    private static readonly Guid L1=Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid L2=Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid R1=Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid R2=Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static CosmicJoinInputRow Proof(Guid id,string key)=>new(id,true,Guid.Empty,new object[]{key},Array.Empty<object>());
    private static Holon Entity(Guid id,string name)=>new(){Id=id,Name=name,IsPublic=true};

    private static CosmicJoinSelection<Holon,Holon,string,CosmicJoinEntityPair<Holon,Holon>> Selection()
        =>CosmicQuery<Holon>.Create().LeftJoin(CosmicQuery<Holon>.Create(),l=>l.Name,r=>r.Name).Result.SelectEntities().Result;

    [Fact]
    public void PairSelectionKeepsBothDetachedRecordsAndExplicitEmptyMatch()
    {
        var first=Entity(L1,"coffee");var second=Entity(L2,"tea");
        var right1=Entity(R1,"coffee");var right2=Entity(R2,"coffee");
        var packet=new CosmicJoinPacket(new[]{Proof(L1,"coffee"),Proof(L2,"tea")},new[]{Proof(R1,"coffee"),Proof(R2,"coffee")},
            new[]{new CosmicJoinRow(L1,R1),new CosmicJoinRow(L1,R2),new CosmicJoinRow(L2,null)},2,2,3,
            childEntities:new[]{right1,right2,null!},leftEntities:new[]{first,first,second});
        var result=CosmicJoinResults.Materialize(Selection(),packet);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Select(row=>row.Value.Left.Id).Should().Equal(L1,L1,L2);
        result.Result.Select(row=>row.Value.HasRight).Should().Equal(true,true,false);
        result.Result[2].Value.Right.Should().BeNull();
        result.Result[0].Value.Left.Should().NotBeSameAs(first);
        result.Result[1].Value.Left.Should().NotBeSameAs(result.Result[0].Value.Left);
        result.Result[0].Value.Right.Should().NotBeSameAs(right1);
        result.Result[0].Value.Left.Name="changed";
        first.Name.Should().Be("coffee");
        result.Result[1].Value.Left.Name.Should().Be("coffee");

        var page=Selection().PageOutput(1,1).Result;
        page.SelectsEntityPair.Should().BeTrue();
        var pagedPacket=new CosmicJoinPacket(new[]{Proof(L1,"coffee"),Proof(L2,"tea")},new[]{Proof(R1,"coffee"),Proof(R2,"coffee")},
            new[]{new CosmicJoinRow(L1,R2)},2,2,3,childEntities:new[]{right2},leftEntities:new[]{first});
        var paged=CosmicJoinResults.Materialize(page,pagedPacket);
        paged.IsError.Should().BeFalse(paged.Message);
        paged.Result.Should().ContainSingle().Which.Value.Right.Id.Should().Be(R2);
    }

    [Fact]
    public void PairSelectionRefusesContradictorySnapshotsAndForgedUnmatchedPayload()
    {
        var changed=Entity(L1,"different snapshot");
        var contradictory=new CosmicJoinPacket(new[]{Proof(L1,"coffee")},new[]{Proof(R1,"coffee"),Proof(R2,"coffee")},
            new[]{new CosmicJoinRow(L1,R1),new CosmicJoinRow(L1,R2)},1,2,2,
            childEntities:new[]{Entity(R1,"coffee"),Entity(R2,"coffee")},leftEntities:new[]{Entity(L1,"coffee"),changed});
        CosmicJoinResults.Materialize(Selection(),contradictory).ErrorCode.Should().Be("CQL_PROVIDER_RESULT_INVALID");
        var forged=new CosmicJoinPacket(new[]{Proof(L1,"coffee")},Array.Empty<CosmicJoinInputRow>(),
            new[]{new CosmicJoinRow(L1,null)},1,0,1,
            childEntities:new[]{Entity(R1,"coffee")},leftEntities:new[]{Entity(L1,"coffee")});
        CosmicJoinResults.Materialize(Selection(),forged).ErrorCode.Should().Be("CQL_PROVIDER_RESULT_INVALID");
    }
}
