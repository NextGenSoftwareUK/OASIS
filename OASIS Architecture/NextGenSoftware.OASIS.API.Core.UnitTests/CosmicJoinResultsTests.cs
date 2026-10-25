using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicJoinResultsTests
{
    private static readonly Guid L1=Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid L2=Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid R1=Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid R2=Guid.Parse("40000000-0000-0000-0000-000000000001");
    private static readonly Guid Actor=Guid.Parse("50000000-0000-0000-0000-000000000001");
    public sealed class Dto
    {
        public static int Constructors;
        public Dto() { Constructors++; }
        public string Name { get; set; } = "";
        public int? Revision { get; set; }
        public bool Present { get; set; }
    }

    private static CosmicJoinSelection<Holon,Holon,string,Dto> Query(bool inner=false,CosmicJoinOptions? options=null,
        bool nullable=false,bool access=false,bool snapshot=false,bool orderedVersion=false)
    {
        var left=CosmicQuery<Holon>.Create();var right=CosmicQuery<Holon>.Create();
        if(access)
        {
            var policy=CosmicQueryReadAccess.OwnerOrPublic(Actor).Result;
            left=left.WithReadAccess(policy).Result;right=right.WithReadAccess(policy).Result;
        }
        if(snapshot)left=left.WithChildren(CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result).Result;
        if(orderedVersion)left=left.OrderBy(row=>row.Version).Result;
        var join=inner ? left.Join(right,row=>row.Name,row=>row.Name,options)
            :left.LeftJoin(right,row=>row.Name,row=>row.Name,options);
        return nullable ? join.Select((owner,member)=>new Dto { Name=owner.Description,
            Revision=CosmicQueryMetadata.Value<int?>(member,"revision"), Present=member!=null }).Result
            :join.Select((owner,member)=>new Dto { Name=owner.Description, Revision=(int?)member.Version, Present=member!=null }).Result;
    }

    private static CosmicJoinInputRow Input(Guid id,object? key,params object[] values)
        =>new(id,true,Actor,new[]{key!},values);
    private static CosmicJoinPacket Packet(CosmicJoinInputRow[] left,CosmicJoinInputRow[] right,CosmicJoinRow[] rows,
        long? leftCount=null,long? rightCount=null,long? pairs=null)
        =>new(left,right,rows,leftCount??left.Length,rightCount??right.Length,pairs??rows.Length);
    private static CosmicJoinPacket One(object? version=null)
        =>Packet(new[]{Input(L1,"coffee","owner")},new[]{Input(R1,"coffee",7)},
            new[]{new CosmicJoinRow(L1,R1,"owner",version??7,true)});
    private static void Invalid(CosmicJoinPacket packet,string code="CQL_PROVIDER_RESULT_INVALID",
        CosmicJoinSelection<Holon,Holon,string,Dto>? query=null)
    {
        Dto.Constructors=0;
        var result=CosmicJoinResults.Materialize(query??Query(),packet);
        result.ErrorCode.Should().Be(code,result.Message);
        result.Result.Should().BeNull();Dto.Constructors.Should().Be(0);
    }

    [Fact]
    public void MatchedNullAndUnmatchedNullKeepDifferentPublishedPresence()
    {
        Dto.Constructors=0;
        var packet=Packet(new[]{Input(L1,"coffee","first"),Input(L2,"tea","second")},
            new[]{Input(R1,"coffee",new object[]{null!})},new[]{
                new CosmicJoinRow(L1,R1,"first",null!,true),new CosmicJoinRow(L2,null,"second",null!,false)});
        var result=CosmicJoinResults.Materialize(Query(nullable:true),packet);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Select(row=>row.HasRight).Should().Equal(true,false);
        result.Result.Select(row=>row.Value.Revision).Should().OnlyContain(value=>value==null);
        Dto.Constructors.Should().Be(2);
    }

    [Fact]
    public void DuplicateKeysProduceAllDistinctNativePairs()
    {
        var packet=Packet(new[]{Input(L1,"coffee","first"),Input(L2,"coffee","second")},
            new[]{Input(R1,"coffee",7),Input(R2,"coffee",8)},new[]{
                new CosmicJoinRow(L1,R1,"first",7,true),new CosmicJoinRow(L1,R2,"first",8,true),
                new CosmicJoinRow(L2,R1,"second",7,true),new CosmicJoinRow(L2,R2,"second",8,true)});
        var result=CosmicJoinResults.Materialize(Query(inner:true),packet);
        result.IsError.Should().BeFalse(result.Message);result.Result.Count.Should().Be(4);
    }

    [Fact]
    public void NullKeyComponentsNeverMatch()
    {
        var left=new[]{Input(L1,null,"owner")};var right=new[]{Input(R1,null,7)};
        var valid=Packet(left,right,new[]{new CosmicJoinRow(L1,null,"owner",null!,false)});
        CosmicJoinResults.Materialize(Query(),valid).IsError.Should().BeFalse();
        Invalid(Packet(left,right,new[]{new CosmicJoinRow(L1,R1,"owner",7,true)}));
    }

    [Fact]
    public void MissingPairCannotHideBehindANativeCountClaim()
        =>Invalid(Packet(new[]{Input(L1,"coffee","owner")},new[]{Input(R1,"coffee",7)},Array.Empty<CosmicJoinRow>(),pairs:1));

    [Fact]
    public void DuplicatePairsCannotReplaceAnOmittedMatch()
        =>Invalid(Packet(new[]{Input(L1,"coffee","owner")},new[]{Input(R1,"coffee",7),Input(R2,"coffee",8)},
            new[]{new CosmicJoinRow(L1,R1,"owner",7,true),new CosmicJoinRow(L1,R1,"owner",7,true)}));

    [Fact]
    public void NativePairKeysMustAgree()
        =>Invalid(Packet(new[]{Input(L1,"coffee","owner")},new[]{Input(R1,"tea",7)},new[]{new CosmicJoinRow(L1,R1,"owner",7,true)}));

    [Fact]
    public void ExistingMatchesCannotBeReportedAsUnmatched()
        =>Invalid(Packet(new[]{Input(L1,"coffee","owner")},new[]{Input(R1,"coffee",7)},new[]{new CosmicJoinRow(L1,null,"owner",null!,false)}));

    [Fact]
    public void InnerJoinCannotPublishAnUnmatchedRow()
        =>Invalid(Packet(new[]{Input(L1,"coffee","owner")},Array.Empty<CosmicJoinInputRow>(),
            new[]{new CosmicJoinRow(L1,null,"owner",null!,false)}),query:Query(inner:true));

    [Fact]
    public void WrongScalarDomainInALaterRowPreventsEveryConstructor()
        =>Invalid(Packet(new[]{Input(L1,"coffee","first"),Input(L2,"coffee","second")},new[]{Input(R1,"coffee",7)},
            new[]{new CosmicJoinRow(L1,R1,"first",7,true),new CosmicJoinRow(L2,R1,"second",7L,true)}));

    [Fact]
    public void PresenceAndSelectedValuesMustAgreeWithTheSourceProof()
    {
        Invalid(Packet(new[]{Input(L1,"coffee","owner")},new[]{Input(R1,"coffee",7)},new[]{new CosmicJoinRow(L1,R1,"owner",7,false)}));
        Invalid(Packet(new[]{Input(L1,"coffee","owner")},new[]{Input(R1,"coffee",7)},new[]{new CosmicJoinRow(L1,R1,"forged",7,true)}));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EitherInaccessibleSourceIsRejectedBeforeConstruction(bool right)
    {
        var hidden=new CosmicJoinInputRow(right?R1:L1,false,Guid.NewGuid(),new object[]{"coffee"},right?new object[]{7}:new object[]{"owner"});
        Invalid(Packet(new[]{right?Input(L1,"coffee","owner"):hidden},new[]{right?hidden:Input(R1,"coffee",7)},
            new[]{new CosmicJoinRow(L1,R1,"owner",7,true)}),query:Query(access:true));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void EveryInputByteBudgetIsIndependent(bool right)
    {
        var options=right?CosmicJoinOptions.Create(maximumRightPayloadBytes:1).Result:CosmicJoinOptions.Create(maximumLeftPayloadBytes:1).Result;
        Invalid(One(),"CQL_RESULT_LIMIT",Query(options:options));
    }

    [Fact]
    public void OutputBytesAndPairWorkAreIndependentOfRowLimits()
    {
        Invalid(One(),"CQL_RESULT_LIMIT",Query(options:CosmicJoinOptions.Create(maximumPayloadBytes:1).Result));
        var packet=Packet(new[]{Input(L1,"coffee","first"),Input(L2,"coffee","second")},
            new[]{Input(R1,"coffee",7),Input(R2,"coffee",8)},Array.Empty<CosmicJoinRow>(),pairs:3);
        Invalid(packet,"CQL_RESULT_LIMIT",Query(options:CosmicJoinOptions.Create(maximumPairs:3).Result));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SourceCountsCannotExceedEitherBudget(bool right)
    {
        var packet=Packet(new[]{Input(L1,"coffee","first"),Input(L2,"tea","second")},
            new[]{Input(R1,"coffee",7),Input(R2,"tea",8)},Array.Empty<CosmicJoinRow>(),pairs:2);
        var options=right?CosmicJoinOptions.Create(maximumRightRows:1).Result:CosmicJoinOptions.Create(maximumLeftRows:1).Result;
        Invalid(packet,"CQL_RESULT_LIMIT",Query(options:options));
    }

    [Fact]
    public void OutputCannotSilentlyTruncateToItsResultBudget()
    {
        var packet=Packet(new[]{Input(L1,"coffee","first"),Input(L2,"coffee","second")},new[]{Input(R1,"coffee",7)},
            new[]{new CosmicJoinRow(L1,R1,"first",7,true)},pairs:2);
        Invalid(packet,"CQL_RESULT_LIMIT",Query(options:CosmicJoinOptions.Create(maximumResults:1).Result));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SourceAndPairOrderingMustBeDeterministic(bool source)
    {
        var right=source?new[]{Input(R2,"coffee",8),Input(R1,"coffee",7)}:new[]{Input(R1,"coffee",7),Input(R2,"coffee",8)};
        Invalid(Packet(new[]{Input(L1,"coffee","owner")},right,
            new[]{new CosmicJoinRow(L1,R2,"owner",8,true),new CosmicJoinRow(L1,R1,"owner",7,true)}));
    }

    [Fact]
    public void CountProofsCannotOmitSourceRows()
        =>Invalid(Packet(Array.Empty<CosmicJoinInputRow>(),Array.Empty<CosmicJoinInputRow>(),Array.Empty<CosmicJoinRow>(),leftCount:1));

    [Fact]
    public void RepeatedSourceIdentityIsInvalid()
        =>Invalid(Packet(new[]{Input(L1,"coffee","first"),Input(L1,"coffee","second")},new[]{Input(R1,"coffee",7)},Array.Empty<CosmicJoinRow>(),pairs:2));

    [Fact]
    public void CanonicalIdentityCellMustMatchItsSourceIdentity()
    {
        var query=CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),row=>row.Id,row=>row.ParentHolonId)
            .Select((owner,member)=>owner.Name).Result;
        var packet=Packet(new[]{new CosmicJoinInputRow(L1,true,Actor,new object[]{L2},new object[]{"owner"})},
            new[]{new CosmicJoinInputRow(R1,true,Actor,new object[]{L2},Array.Empty<object>())},new[]{new CosmicJoinRow(L1,R1,"owner")});
        CosmicJoinResults.Materialize(query,packet).ErrorCode.Should().Be("CQL_PROVIDER_RESULT_INVALID");
    }

    [Fact]
    public void RepeatedPathCellsMustAgree()
    {
        var query=CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),row=>row.Name,row=>row.Name)
            .Select((owner,member)=>owner.Name).Result;
        var packet=Packet(new[]{Input(L1,"coffee","wrong")},new[]{Input(R1,"coffee")},new[]{new CosmicJoinRow(L1,R1,"wrong")});
        CosmicJoinResults.Materialize(query,packet).ErrorCode.Should().Be("CQL_PROVIDER_RESULT_INVALID");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SharedSnapshotRejectsConflictingObservationsOfTheSameIdentity(bool snapshot)
    {
        var left=new CosmicJoinInputRow(L1,true,Actor,new object[]{"coffee"},new object[]{"owner"},new object[]{2});
        var packet=Packet(new[]{left},new[]{Input(L1,"coffee",7)},new[]{new CosmicJoinRow(L1,L1,"owner",7,true)});
        var result=CosmicJoinResults.Materialize(Query(snapshot:snapshot,orderedVersion:true),packet);
        result.IsError.Should().Be(snapshot,result.Message);
    }

    [Fact]
    public void CancellationBeforeValidationInvokesNoApplicationCode()
    {
        Dto.Constructors=0;
        using var cancel=new CancellationTokenSource();cancel.Cancel();
        CosmicJoinResults.Materialize(Query(),One(),cancel.Token).ErrorCode.Should().Be("CQL_QUERY_CANCELLED");
        Dto.Constructors.Should().Be(0);
    }

    [Fact]
    public void PacketAndCellArraysAreDetachedFromCallerMutation()
    {
        var keys=new object[]{"coffee"};var values=new object[]{"owner"};
        var left=new[]{new CosmicJoinInputRow(L1,true,Actor,keys,values)};
        var output=new object[]{"owner",7,true};var rows=new[]{new CosmicJoinRow(L1,R1,output)};
        var packet=Packet(left,new[]{Input(R1,"coffee",7)},rows);
        keys[0]="changed";values[0]="changed";output[0]="changed";left[0]=null!;rows[0]=null!;
        CosmicJoinResults.Materialize(Query(),packet).IsError.Should().BeFalse();
    }

    [Fact]
    public void PresenceIsPublishedEvenWhenTheDtoDoesNotSelectIt()
    {
        var query=CosmicQuery<Holon>.Create().LeftJoin(CosmicQuery<Holon>.Create(),row=>row.Name,row=>row.Name)
            .Select((owner,member)=>owner.Description).Result;
        var packet=Packet(new[]{Input(L1,"coffee","first"),Input(L2,"tea","second")},new[]{Input(R1,"coffee")},
            new[]{new CosmicJoinRow(L1,R1,"first"),new CosmicJoinRow(L2,null,"second")});
        var result=CosmicJoinResults.Materialize(query,packet);
        result.IsError.Should().BeFalse(result.Message);
        result.Result.Select(row=>row.HasRight).Should().Equal(true,false);
    }
    [Fact]
    public void OutputCannotNormalizeSignedZeroAwayFromItsSourceProof()
    {
        var query=CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),row=>row.Name,row=>row.Name)
            .Select((owner,member)=>CosmicQueryMetadata.Value<double>(member,"value")).Result;
        var negative=BitConverter.Int64BitsToDouble(long.MinValue);
        var left=new[]{Input(L1,"coffee")};var right=new[]{Input(R1,"coffee",negative)};
        CosmicJoinResults.Materialize(query,Packet(left,right,new[]{new CosmicJoinRow(L1,R1,0d)}))
            .ErrorCode.Should().Be("CQL_PROVIDER_RESULT_INVALID");
        var valid=CosmicJoinResults.Materialize(query,Packet(left,right,new[]{new CosmicJoinRow(L1,R1,negative)}));
        valid.IsError.Should().BeFalse(valid.Message);
        BitConverter.DoubleToInt64Bits(valid.Result.Single().Value).Should().Be(long.MinValue);
    }

    [Fact]
    public void OutputRetainsTheSourceDecimalRepresentation()
    {
        var query=CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),row=>row.Name,row=>row.Name)
            .Select((owner,member)=>CosmicQueryMetadata.Value<decimal>(member,"value")).Result;
        var left=new[]{Input(L1,"coffee")};var right=new[]{Input(R1,"coffee",1.00m)};
        CosmicJoinResults.Materialize(query,Packet(left,right,new[]{new CosmicJoinRow(L1,R1,1.0m)}))
            .ErrorCode.Should().Be("CQL_PROVIDER_RESULT_INVALID");
        CosmicJoinResults.Materialize(query,Packet(left,right,new[]{new CosmicJoinRow(L1,R1,1.00m)}))
            .IsError.Should().BeFalse();
    }

    [Fact]
    public void SignedZeroKeysStillUseLogicalNumericEquality()
    {
        var query=CosmicQuery<Holon>.Create().Join(CosmicQuery<Holon>.Create(),
            row=>CosmicQueryMetadata.Value<double>(row,"key"),row=>CosmicQueryMetadata.Value<double>(row,"key"))
            .Select((owner,member)=>owner.Description).Result;
        var packet=Packet(new[]{Input(L1,BitConverter.Int64BitsToDouble(long.MinValue),"owner")},
            new[]{Input(R1,0d)},new[]{new CosmicJoinRow(L1,R1,"owner")});
        CosmicJoinResults.Materialize(query,packet).IsError.Should().BeFalse();
    }
}