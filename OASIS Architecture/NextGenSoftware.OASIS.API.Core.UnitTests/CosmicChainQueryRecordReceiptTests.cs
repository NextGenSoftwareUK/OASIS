using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChainQueryRecordReceiptTests
{
    private static readonly DateTimeOffset Now=new(2026,10,27,12,0,0,TimeSpan.Zero);
    private static readonly Guid Id=Guid.Parse("4429fb5c-fbb2-45ce-8148-13105f7220c5");
    private static readonly Guid VersionId=Guid.Parse("2399db93-cb22-4546-a432-0868ebfd68a0");
    private static CosmicChainQuerySource Source(string network="genesis-A",long start=0) => new(
        ProviderType.SolanaOASIS,network,ProviderType.SQLLiteDBOASIS,
        Guid.Parse("7327d4ef-d118-4877-90c4-c0da06a69204"),1,new string('A',64),start);
    private static CosmicChainQueryRecordIdentity Record(Guid? id=null,int version=1,Guid? versionId=null,
        bool deleted=false,string? hash=null) => new(id ?? Id,version,versionId ?? VersionId,deleted,hash ?? new string('B',64));
    private static CosmicChainQueryReadRequirement Requirement(long start=0) => new(Source(start:start),new(90,"block-90"),10,TimeSpan.FromSeconds(10));
    private static CosmicChainQueryCheckpoint Checkpoint(long start=0) => new(Source(start:start),new(100,"block-100"),start);
    private static CosmicChainQueryFinalizedHead Head(DateTimeOffset? observedAt=null) => new(
        ProviderType.SolanaOASIS,"genesis-A",new(110,"block-110"),observedAt ?? Now);
    private static CosmicChainQueryRecordReceipt Receipt(long slot=95,string? block=null,
        CosmicChainQueryRecordIdentity? record=null,CosmicChainQuerySource? source=null) => new(
        source ?? Source(),new(slot,block ?? "block-"+slot),"original-commit-reference",record ?? Record());

    [Theory]
    [InlineData(-10,true)] [InlineData(-11,false)] [InlineData(1,false)]
    public void IndependentObservationHasAnExplicitLifetimeIncludingExactBoundary(int seconds,bool accepted)
    {
        var result=CosmicChainQueryCheckpointAdmission.Admit(Requirement(),Checkpoint(),Head(Now.AddSeconds(seconds)),Now);
        Assert.Equal(!accepted,result.IsError);
        if(!accepted) Assert.Equal("CQL_SOURCE_OBSERVATION_EXPIRED",result.ErrorCode);
    }

    [Fact]
    public void ObservationOffsetsDoNotChangeAgeAndLaterPublicationMustRevalidate()
    {
        var head=Head(Now.ToOffset(TimeSpan.FromHours(5)));
        Assert.False(CosmicChainQueryCheckpointAdmission.Admit(Requirement(),Checkpoint(),head,Now).IsError);
        var receipt=Receipt();
        var result=CosmicChainQueryRecordAdmission.Admit(Requirement(),Checkpoint(),head,Now.AddSeconds(11),receipt,Receipt());
        Assert.Equal("CQL_SOURCE_OBSERVATION_EXPIRED",result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void LiveAndTombstoneReceiptsBindTheOriginalRevisionWithoutAcknowledgingALoad(bool deleted)
    {
        var record=Record(deleted:deleted);
        var receipt=Receipt(record:record);
        var result=CosmicChainQueryRecordAdmission.Admit(Requirement(),Checkpoint(),Head(),Now,receipt,Receipt(record:Record(deleted:deleted)));
        Assert.False(result.IsError,result.Message);
        Assert.Same(receipt,result.Result);
        Assert.Equal(deleted,result.Result.Record.IsDeleted);
        Assert.False(result.IsLoaded);
    }

    [Theory]
    [InlineData("id")] [InlineData("version")] [InlineData("version-id")]
    [InlineData("deletion")] [InlineData("hash")]
    public void IndexPayloadCannotReplaceTheCanonicalSourceIdentityOrContent(string change)
    {
        var decoded=change switch {
            "id"=>Record(id:Guid.Parse("7c731b6e-be85-46d9-af3a-6627cd9006f9")),
            "version"=>Record(version:2),
            "version-id"=>Record(versionId:Guid.Parse("006b112d-5560-4bb8-9b5f-a12d18c511c5")),
            "deletion"=>Record(deleted:true),
            _=>Record(hash:new string('C',64))
        };
        var result=CosmicChainQueryRecordAdmission.Admit(Requirement(),Checkpoint(),Head(),Now,Receipt(),Receipt(record:decoded));
        Assert.Equal("CQL_SOURCE_RECORD_MISMATCH",result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public void AReceiptFromAnotherSourceCannotBeLaunderedThroughAValidCheckpoint()
        => Assert.Equal("CQL_SOURCE_BINDING_MISMATCH",CosmicChainQueryRecordAdmission.Admit(
            Requirement(),Checkpoint(),Head(),Now,Receipt(source:Source("genesis-B")),Receipt()).ErrorCode);

    [Theory]
    [InlineData("reference")] [InlineData("slot")] [InlineData("block")]
    public void MatchingPayloadCannotHideASubstitutedSourceCommit(string change)
    {
        var original=Receipt();
        var substituted=change switch {
            "reference"=>new CosmicChainQueryRecordReceipt(Source(),original.Position,"another-commit",Record()),
            "slot"=>Receipt(96),
            _=>Receipt(95,"another-block")
        };
        var result=CosmicChainQueryRecordAdmission.Admit(Requirement(),Checkpoint(),Head(),Now,substituted,original);
        Assert.Equal("CQL_SOURCE_RECORD_MISMATCH",result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public void IndependentSourceReceiptAlsoMustBelongToTheSelectedNetwork()
        => Assert.Equal("CQL_SOURCE_BINDING_MISMATCH",CosmicChainQueryRecordAdmission.Admit(
            Requirement(),Checkpoint(),Head(),Now,Receipt(),Receipt(source:Source("genesis-B"))).ErrorCode);

    [Fact]
    public void ReceiptMustBeInsideCompleteRangeAndAgreeAtFinalizedAnchors()
    {
        Assert.Equal("CQL_SOURCE_RECORD_OUTSIDE_CHECKPOINT",CosmicChainQueryRecordAdmission.Admit(
            Requirement(),Checkpoint(),Head(),Now,Receipt(101),Receipt(101)).ErrorCode);
        var source=Source(start:50);
        Assert.Equal("CQL_SOURCE_RECORD_OUTSIDE_CHECKPOINT",CosmicChainQueryRecordAdmission.Admit(
            Requirement(50),Checkpoint(50),Head(),Now,Receipt(49,source:source),Receipt(49,source:source)).ErrorCode);
        Assert.Equal("CQL_SOURCE_FINALITY_CONFLICT",CosmicChainQueryRecordAdmission.Admit(
            Requirement(),Checkpoint(),Head(),Now,Receipt(100,"another-block"),Receipt(100,"another-block")).ErrorCode);
        Assert.Equal("CQL_SOURCE_FINALITY_CONFLICT",CosmicChainQueryRecordAdmission.Admit(
            Requirement(),Checkpoint(),Head(),Now,Receipt(90,"another-block"),Receipt(90,"another-block")).ErrorCode);
        var previous=new CosmicChainQueryCheckpoint(Source(),new(95,"block-95"),0);
        Assert.Equal("CQL_SOURCE_FINALITY_CONFLICT",CosmicChainQueryRecordAdmission.Admit(
            Requirement(),Checkpoint(),Head(),Now,Receipt(95,"another-block"),Receipt(95,"another-block"),previous).ErrorCode);
    }

    [Fact]
    public void MissingReceiptOrDecodedRecordFailsClosedAndLifetimeCannotBeDisabled()
    {
        Assert.Equal("CQL_SOURCE_EVIDENCE_MISSING",CosmicChainQueryRecordAdmission.Admit(
            Requirement(),Checkpoint(),Head(),Now,null!,Receipt()).ErrorCode);
        Assert.Equal("CQL_SOURCE_EVIDENCE_MISSING",CosmicChainQueryRecordAdmission.Admit(
            Requirement(),Checkpoint(),Head(),Now,Receipt(),null!).ErrorCode);
        Assert.Throws<ArgumentOutOfRangeException>(()=>new CosmicChainQueryReadRequirement(Source(),new(90,"block-90"),10,TimeSpan.Zero));
        Assert.Throws<ArgumentException>(()=>Record(version:0));
        Assert.Throws<ArgumentException>(()=>Record(versionId:Guid.Empty));
        Assert.Throws<ArgumentException>(()=>Record(hash:"not-a-canonical-hash"));
        Assert.Throws<ArgumentException>(()=>new CosmicChainQueryRecordReceipt(Source(),new(95,"block-95"),"unsafe\nreference",Record()));
    }
}
