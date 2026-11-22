using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicJoinLeftEntityTests
{
    private static readonly Guid L = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid R1 = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid R2 = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid Actor = Guid.Parse("40000000-0000-0000-0000-000000000001");

    private static CosmicJoinInputRow Proof(Guid id)
        => new(id,false,Actor,new object[]{"coffee"},Array.Empty<object>());

    private static CosmicJoinSelection<Holon,Holon,string,Holon> Selection()
    {
        var source=CosmicQuery<Holon>.Create()
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Actor).Result).Result;
        return source.LeftJoin(source,row=>row.Name,row=>row.Name).Result
            .Select((owner,match)=>owner).Result;
    }

    private static Holon Owner()
        => new() { Id=L,Name="coffee",CreatedByAvatarId=Actor,IsPublic=false,ParentHolonId=R2 };

    [Fact]
    public void WholeLeftPreservesNativeMultiplicityAndPageWithoutSharingProviderObjects()
    {
        var source=Owner();
        var query=Selection();
        query.SelectsLeftEntity.Should().BeTrue();
        query.SelectsChildEntity.Should().BeFalse();
        var packet=new CosmicJoinPacket(new[]{Proof(L)},new[]{Proof(R1),Proof(R2)},
            new[]{new CosmicJoinRow(L,R1),new CosmicJoinRow(L,R2)},1,2,2,
            leftEntities:new[]{source,source});
        var result=CosmicJoinResults.Materialize(query,packet);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().HaveCount(2);
        result.Result.Select(row=>row.Value.Id).Should().Equal(L,L);
        result.Result[0].Value.Should().NotBeSameAs(source);
        result.Result[1].Value.Should().NotBeSameAs(result.Result[0].Value);
        result.Result[0].Value.Name="caller mutation";
        source.Name.Should().Be("coffee");
        result.Result[1].Value.Name.Should().Be("coffee");

        var page=query.PageOutput(1,1).Result;
        page.SelectsLeftEntity.Should().BeTrue();
        var pagedPacket=new CosmicJoinPacket(new[]{Proof(L)},new[]{Proof(R1),Proof(R2)},
            new[]{new CosmicJoinRow(L,R2)},1,2,2,leftEntities:new[]{source});
        var paged=CosmicJoinResults.Materialize(page,pagedPacket);
        paged.IsError.Should().BeFalse(paged.Message);
        paged.Result.Should().ContainSingle().Which.Value.Id.Should().Be(L);

        var unmatched=new CosmicJoinPacket(new[]{Proof(L)},Array.Empty<CosmicJoinInputRow>(),
            new[]{new CosmicJoinRow(L,null)},1,0,1,leftEntities:new[]{source});
        var emptyMatch=CosmicJoinResults.Materialize(query,unmatched);
        emptyMatch.IsError.Should().BeFalse(emptyMatch.Message);
        emptyMatch.Result.Single().HasRight.Should().BeFalse();
        emptyMatch.Result.Single().Value.Id.Should().Be(L);
    }

    [Fact]
    public void WholeLeftRefusesIncompletePairsWrongEntityAndWrongAccessProofBeforePublication()
    {
        var query=Selection();
        var source=Owner();
        var incomplete=new CosmicJoinPacket(new[]{Proof(L)},new[]{Proof(R1),Proof(R2)},
            new[]{new CosmicJoinRow(L,R1)},1,2,2,leftEntities:new[]{source});
        var missing=CosmicJoinResults.Materialize(query,incomplete);
        missing.ErrorCode.Should().Be("CQL_PROVIDER_RESULT_INVALID");
        missing.Result.Should().BeNull();

        foreach(var mismatch in new[]{"identity","owner","visibility","missing","wrong side"})
        {
            var forged=Owner();
            if(mismatch=="identity")forged.Id=R1;
            if(mismatch=="owner")forged.CreatedByAvatarId=R2;
            if(mismatch=="visibility")forged.IsPublic=true;
            var packet=new CosmicJoinPacket(new[]{Proof(L)},new[]{Proof(R1)},
                new[]{new CosmicJoinRow(L,R1)},1,1,1,
                childEntities:mismatch=="wrong side"?new[]{forged}:null,
                leftEntities:mismatch is "missing" or "wrong side"?null:new[]{forged});
            var rejected=CosmicJoinResults.Materialize(query,packet);
            rejected.ErrorCode.Should().Be("CQL_PROVIDER_RESULT_INVALID",mismatch);
            rejected.Result.Should().BeNull();
        }
    }
}
