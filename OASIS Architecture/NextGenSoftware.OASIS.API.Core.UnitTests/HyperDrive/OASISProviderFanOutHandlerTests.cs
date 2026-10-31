using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class OASISProviderFanOutHandlerTests
{
    [Fact]
    public async Task AppliesSameOperationToEveryExplicitTarget()
    {
        var first = new Target("first", true);
        var second = new Target("second", true);
        var item = NewItem();

        var result = await new OASISProviderFanOutHandler(new[] { first, second }).ApplyAsync(item, default);

        Assert.False(result.IsError, result.Message);
        Assert.Equal(item.OperationId, Assert.Single(first.Operations));
        Assert.Equal(item.OperationId, Assert.Single(second.Operations));
    }

    [Fact]
    public async Task PartialTargetFailureIsVisibleAndRetryable()
    {
        var successful = new Target("successful", true);
        var failed = new Target("failed", false);

        var result = await new OASISProviderFanOutHandler(new[] { successful, failed })
            .ApplyAsync(NewItem(), default);

        Assert.True(result.IsError);
        Assert.Equal("HOSTED_FANOUT_TARGETS_INCOMPLETE", result.ErrorCode);
        Assert.Contains("failed", result.Message);
    }

    [Fact]
    public void DuplicateTargetIdsAreRejectedAtComposition()
    {
        Assert.Throws<ArgumentException>(() => new OASISProviderFanOutHandler(new[]
        {
            new Target("same", true), new Target("same", true)
        }));
    }

    [Fact]
    public async Task TargetMutationCannotChangeQueuedIntentOrLaterDeliveries()
    {
        var item=NewItem();item.AttemptCount=7;
        var expected=JsonConvert.SerializeObject(item);
        HostedSyncFanOutItem? firstCopy=null;
        var first=new CallbackTarget("mutating",(received,_)=> {
            Assert.NotSame(item,received);firstCopy=received;
            received.OperationId=Guid.NewGuid();received.AvatarId=Guid.NewGuid();received.EntityId=Guid.NewGuid();
            received.EntityType="changed";received.Kind=SyncOperationKind.Delete;received.VersionId=Guid.NewGuid();
            received.PayloadJson="changed";received.AttemptCount=99;
            return Task.FromResult(new OASISResult<bool>(true));
        });
        var second=new CallbackTarget("observing",(received,_)=> {
            Assert.NotSame(item,received);Assert.NotSame(firstCopy,received);
            Assert.Equal(expected,JsonConvert.SerializeObject(received));
            return Task.FromResult(new OASISResult<bool>(true));
        });
        var result=await new OASISProviderFanOutHandler(new[]{first,second}).ApplyAsync(item,default);
        Assert.False(result.IsError);Assert.Equal(expected,JsonConvert.SerializeObject(item));
        Assert.All(Outcomes(result),outcome=>Assert.True(outcome.IsApplied));
    }

    [Fact]
    public async Task ThrowingAndReturnedFailuresDoNotStarveLaterTargetsAndKeepBothCauses()
    {
        var firstCause=new IOException("first target threw");var secondCause=new IOException("second target returned failure");
        var lastCalls=0;
        var targets=new[]{new CallbackTarget("first",(_,_)=>throw firstCause),
            new CallbackTarget("second",(_,_)=>Task.FromResult(new OASISResult<bool>{IsError=true,ErrorCode="TARGET_DOWN",Exception=secondCause})),
            new CallbackTarget("last",(_,_)=>{lastCalls++;return Task.FromResult(new OASISResult<bool>(true));})};
        var result=await new OASISProviderFanOutHandler(targets).ApplyAsync(NewItem(),default);
        Assert.True(result.IsError);Assert.False(result.Result);Assert.Equal(2,result.ErrorCount);Assert.Equal(1,lastCalls);
        Assert.Equal(new[]{firstCause,secondCause},Assert.IsType<AggregateException>(result.Exception).InnerExceptions);
        var outcomes=Outcomes(result);Assert.Equal(new[]{"first","second","last"},outcomes.Select(value=>value.TargetId));
        Assert.False(outcomes[0].IsApplied);Assert.False(outcomes[1].IsApplied);Assert.True(outcomes[2].IsApplied);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeUncertaintyCannotBePromotedToTargetSuccess(bool wrapped)
    {
        Exception cause=new UncertainTarget();if(wrapped)cause=new AggregateException(new IOException("outer",cause));
        var calls=0;
        var result=await new OASISProviderFanOutHandler(new[]{
            new CallbackTarget("uncertain",(_,_)=>Task.FromResult(new OASISResult<bool>(true){Exception=cause})),
            new CallbackTarget("next",(_,_)=>{calls++;return Task.FromResult(new OASISResult<bool>(true));})}).ApplyAsync(NewItem(),default);
        Assert.True(result.IsError);Assert.Same(cause,result.Exception);Assert.Equal(1,calls);
        var uncertain=Outcomes(result)[0];Assert.False(uncertain.IsApplied);Assert.True(uncertain.RequiresReconciliation);
        Assert.Equal("HOSTED_FANOUT_TARGET_RECONCILIATION_REQUIRED",uncertain.ErrorCode);
    }

    [Fact]
    public async Task IndependentTargetCancellationIsFailureAndDoesNotCancelOtherTargets()
    {
        var cause=new OperationCanceledException("independent target timeout");var calls=0;
        var result=await new OASISProviderFanOutHandler(new[]{new CallbackTarget("timeout",(_,_)=>throw cause),
            new CallbackTarget("next",(_,_)=>{calls++;return Task.FromResult(new OASISResult<bool>(true));})}).ApplyAsync(NewItem(),default);
        Assert.True(result.IsError);Assert.Same(cause,result.Exception);Assert.Equal(1,calls);
        Assert.Equal("HOSTED_FANOUT_TARGET_CANCELLED",Outcomes(result)[0].ErrorCode);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CallerCancellationStopsFanOutBeforeFurtherDelivery(bool alreadyCancelled)
    {
        using var cancellation=new CancellationTokenSource();if(alreadyCancelled)cancellation.Cancel();
        var firstCalls=0;var nextCalls=0;
        var handler=new OASISProviderFanOutHandler(new[]{new CallbackTarget("first",(_,token)=> {
            Assert.Equal(cancellation.Token,token);firstCalls++;cancellation.Cancel();
            return Task.FromResult(new OASISResult<bool>(true));}),new CallbackTarget("next",(_,_)=> {
                nextCalls++;return Task.FromResult(new OASISResult<bool>(true));})});
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>handler.ApplyAsync(NewItem(),cancellation.Token));
        Assert.Equal(alreadyCancelled ? 0:1,firstCalls);Assert.Equal(0,nextCalls);
    }

    [Fact]
    public async Task KnownAppliedWarningPreservesCauseWithoutDeferringDurableSuccess()
    {
        var cause=new IOException("known acknowledgement cleanup warning");
        var result=await new OASISProviderFanOutHandler(new[]{new CallbackTarget("applied",(_,_)=>
            Task.FromResult(new OASISResult<bool>(true){IsWarning=true,Exception=cause,Message="cleanup warning"}))}).ApplyAsync(NewItem(),default);
        Assert.False(result.IsError);Assert.True(result.Result);Assert.True(result.IsWarning);Assert.Equal(1,result.WarningCount);
        Assert.Same(cause,result.Exception);Assert.True(Outcomes(result)[0].IsApplied);Assert.Single(result.InnerMessages);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MissingOrNegativeAcknowledgementRemainsIncompleteWhileLaterTargetRuns(bool negative)
    {
        var calls=0;
        var result=await new OASISProviderFanOutHandler(new[]{new CallbackTarget("missing",(_,_)=>Task.FromResult<OASISResult<bool>>(negative ? new OASISResult<bool>(false):null!)),
            new CallbackTarget("next",(_,_)=>{calls++;return Task.FromResult(new OASISResult<bool>(true));})}).ApplyAsync(NewItem(),default);
        Assert.True(result.IsError);Assert.Equal(1,calls);
        Assert.Equal(negative ? "HOSTED_FANOUT_TARGET_NOT_APPLIED":"NO_RESULT",Outcomes(result)[0].ErrorCode);
    }

    private static HostedFanOutTargetOutcome[] Outcomes(OASISResult<bool> result)=>
        JsonConvert.DeserializeObject<HostedFanOutTargetOutcome[]>(result.MetaData[OASISProviderFanOutHandler.TargetOutcomesMetadataKey])!;
    private sealed class UncertainTarget:Exception,INativeMutationReconciliationRequired
    {public string OriginalTransactionId=>"owned-original-transaction";}
    private sealed class CallbackTarget(string id,Func<HostedSyncFanOutItem,CancellationToken,Task<OASISResult<bool>>> apply):IHyperDriveIdempotentReplicationTarget
    {
        public string ReplicationTargetId=>id;
        public Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,CancellationToken cancellationToken)=>apply(item,cancellationToken);
    }

    private static HostedSyncFanOutItem NewItem() => new()
    {
        OperationId = Guid.NewGuid(), AvatarId = Guid.NewGuid(), EntityId = Guid.NewGuid(),
        EntityType = "holon", Kind = SyncOperationKind.Upsert,
        VersionId = Guid.NewGuid(), PayloadJson = "{}"
    };

    private sealed class Target : IHyperDriveIdempotentReplicationTarget
    {
        private readonly bool _success;
        public Target(string id, bool success) { ReplicationTargetId = id; _success = success; }
        public string ReplicationTargetId { get; }
        public List<Guid> Operations { get; } = new();
        public Task<OASISResult<bool>> ApplyReplicatedMutationAsync(HostedSyncFanOutItem item,
            CancellationToken cancellationToken)
        {
            Operations.Add(item.OperationId);
            return Task.FromResult(_success ? new OASISResult<bool>(true) : new OASISResult<bool>
            { IsError = true, ErrorCode = "TARGET_DOWN", Message = "down" });
        }
    }
}
