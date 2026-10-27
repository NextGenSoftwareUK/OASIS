using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChainQueryCheckpointTests
{
    private static readonly DateTimeOffset Now = new(2026,10,27,12,0,0,TimeSpan.Zero);
    private static NextGenSoftware.OASIS.Common.OASISResult<CosmicChainQueryCheckpoint> Admit(CosmicChainQueryReadRequirement requirement, CosmicChainQueryCheckpoint checkpoint, CosmicChainQueryFinalizedHead head, CosmicChainQueryCheckpoint? previous=null)
        => CosmicChainQueryCheckpointAdmission.Admit(requirement,checkpoint,head,Now,previous);
    private static readonly Guid IndexId = Guid.Parse("19ae935d-cea7-466c-8318-6923d8b843c1");
    private static CosmicChainQuerySource Source(string network="genesis-A", ProviderType source=ProviderType.SolanaOASIS,
        ProviderType executor=ProviderType.SQLLiteDBOASIS, Guid? index=null, int schema=1, string? catalogue=null, long start=0)
        => new(source, network, executor, index ?? IndexId, schema, catalogue ?? new string('A',64), start);
    private static CosmicChainQueryPosition Position(long slot, string? block=null) => new(slot, block ?? "block-"+slot);
    private static CosmicChainQueryCheckpoint Checkpoint(long slot=100, CosmicChainQuerySource? source=null,
        long start=0, string? block=null) => new(source ?? Source(),Position(slot,block),start);
    private static CosmicChainQueryFinalizedHead Head(long slot=110, string network="genesis-A",
        ProviderType source=ProviderType.SolanaOASIS, string? block=null) => new(source,network,Position(slot,block),Now);
    private static CosmicChainQueryReadRequirement Requirement(long minimum=90, long lag=10,
        CosmicChainQuerySource? source=null, string? block=null) => new(source ?? Source(),Position(minimum,block),lag,TimeSpan.FromSeconds(10));

    [Fact]
    public void CompleteExplicitlyBoundCheckpointPassesAtExactLagBoundary()
    {
        var checkpoint=Checkpoint();
        var result=Admit(Requirement(),checkpoint,Head(),Checkpoint(95));
        Assert.False(result.IsError,result.Message);
        Assert.Same(checkpoint,result.Result);
        Assert.Equal(ProviderType.SolanaOASIS,result.Result.Source.SourceProvider);
        Assert.Equal(ProviderType.SQLLiteDBOASIS,result.Result.Source.ExecutionProvider);
        Assert.False(result.IsLoaded); // Admission is not execution or proof that any record loaded.
    }

    [Theory]
    [InlineData("source")] [InlineData("network")] [InlineData("executor")]
    [InlineData("index")] [InlineData("schema")] [InlineData("catalogue")] [InlineData("range")]
    public void SourceSubstitutionCannotPassAnOtherwiseFreshCheckpoint(string change)
    {
        var substituted=change switch {
            "source"=>Source(source:ProviderType.MongoDBOASIS),
            "network"=>Source(network:"genesis-B"),
            "executor"=>Source(executor:ProviderType.MongoDBOASIS),
            "index"=>Source(index:Guid.Parse("12863d5d-8889-4321-b413-6bcf3d0b942b")),
            "schema"=>Source(schema:2),
            "catalogue"=>Source(catalogue:new string('B',64)),
            _=>Source(start:1)
        };
        var result=Admit(Requirement(),Checkpoint(source:substituted),Head());
        Assert.Equal("CQL_SOURCE_BINDING_MISMATCH",result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void AHeadFromAnotherNetworkOrProviderCannotLaunderFreshness(bool wrongNetwork)
    {
        var head=wrongNetwork ? Head(network:"genesis-B") : Head(source:ProviderType.MongoDBOASIS);
        Assert.Equal("CQL_SOURCE_BINDING_MISMATCH",
            Admit(Requirement(),Checkpoint(),head).ErrorCode);
    }

    [Fact]
    public void EmptyOrMissingHistoryCannotBecomeCompleteCoverageByAdvancingTheWatermark()
    {
        Assert.Equal("CQL_SOURCE_COVERAGE_INCOMPLETE",
            Admit(Requirement(),Checkpoint(start:1),Head()).ErrorCode);
        var source=Source(start:50);
        var admitted=Admit(Requirement(source:source),
            Checkpoint(source:source,start:50),Head());
        Assert.False(admitted.IsError,admitted.Message);
        Assert.Equal(50,admitted.Result.CompleteFromSlot);
    }

    [Theory]
    [InlineData(100,111,90,"CQL_SOURCE_NOT_FRESH")]
    [InlineData(89,95,90,"CQL_SOURCE_NOT_FRESH")]
    [InlineData(111,110,90,"CQL_SOURCE_CHECKPOINT_INVALID")]
    [InlineData(100,110,111,"CQL_SOURCE_CHECKPOINT_INVALID")]
    public void MinimumAndIndependentHeadBothConstrainFreshness(long checkpoint,long head,long minimum,string code)
        => Assert.Equal(code,Admit(Requirement(minimum),Checkpoint(checkpoint),Head(head)).ErrorCode);

    [Fact]
    public void SameSlotFinalityConflictsAreNotTreatedAsFreshnessOrAbsence()
    {
        Assert.Equal("CQL_SOURCE_FINALITY_CONFLICT",Admit(
            Requirement(minimum:100),Checkpoint(),Head(100,block:"conflicting-block")).ErrorCode);
        Assert.Equal("CQL_SOURCE_FINALITY_CONFLICT",Admit(
            Requirement(minimum:100,block:"conflicting-block"),Checkpoint(),Head()).ErrorCode);
        Assert.Equal("CQL_SOURCE_FINALITY_CONFLICT",Admit(
            Requirement(),Checkpoint(),Head(),Checkpoint(block:"conflicting-block")).ErrorCode);
    }

    [Fact]
    public void PreviousAcknowledgementCannotRegressOrChangeSourceOrLoseCoverage()
    {
        Assert.Equal("CQL_SOURCE_CHECKPOINT_REGRESSED",Admit(
            Requirement(),Checkpoint(),Head(),Checkpoint(101)).ErrorCode);
        Assert.Equal("CQL_SOURCE_BINDING_MISMATCH",Admit(
            Requirement(),Checkpoint(),Head(),Checkpoint(source:Source(network:"genesis-B"))).ErrorCode);
        Assert.Equal("CQL_SOURCE_COVERAGE_INCOMPLETE",Admit(
            Requirement(),Checkpoint(),Head(),Checkpoint(start:1)).ErrorCode);
    }

    [Fact]
    public void LongSlotArithmeticCannotOverflowIntoAFalseFreshResult()
    {
        Assert.Equal("CQL_SOURCE_NOT_FRESH",Admit(
            Requirement(minimum:0,lag:long.MaxValue-1),Checkpoint(0),Head(long.MaxValue)).ErrorCode);
        Assert.False(Admit(
            Requirement(minimum:0,lag:long.MaxValue),Checkpoint(0),Head(long.MaxValue)).IsError);
    }

    [Fact]
    public void MissingEvidenceNeverAcknowledgesAnEmptySuccessfulRead()
    {
        Assert.Equal("CQL_SOURCE_EVIDENCE_MISSING",Admit(null!,Checkpoint(),Head()).ErrorCode);
        Assert.Equal("CQL_SOURCE_EVIDENCE_MISSING",Admit(Requirement(),null!,Head()).ErrorCode);
        Assert.Equal("CQL_SOURCE_EVIDENCE_MISSING",Admit(Requirement(),Checkpoint(),null!).ErrorCode);
    }

    [Fact]
    public void InvalidUnboundedAndNoncanonicalDescriptorsRejectBeforeAnyExecution()
    {
        Assert.Throws<ArgumentException>(()=>Source(source:ProviderType.Default));
        Assert.Throws<ArgumentException>(()=>Source(executor:ProviderType.All));
        Assert.Throws<ArgumentException>(()=>Source(index:Guid.Empty));
        Assert.Throws<ArgumentException>(()=>Source(network:"genesis\nA"));
        Assert.Throws<ArgumentException>(()=>Source(network:new string('A',129)));
        Assert.Throws<ArgumentException>(()=>Source(catalogue:new string('a',64)));
        Assert.Throws<ArgumentException>(()=>Source(schema:0));
        Assert.Throws<ArgumentException>(()=>Source(start:-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Position(-1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Checkpoint(start:101));
        Assert.Throws<ArgumentOutOfRangeException>(()=>Requirement(lag:-1));
        Assert.Throws<ArgumentException>(()=>Requirement(minimum:0,source:Source(start:1)));
    }
}
