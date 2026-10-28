using System.Security.Cryptography;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChainQueryIngestionTests
{
    [Theory]
    [InlineData("publish-throw")] [InlineData("publish-cancel-throw")] [InlineData("publish-null")] [InlineData("valid")] [InlineData("range-error")] [InlineData("record-error")] [InlineData("mismatch")]
    [InlineData("hash")] [InlineData("stale")] [InlineData("network")] [InlineData("pre-cancel")]
    [InlineData("record-cancel")] [InlineData("publish-cancel")] [InlineData("head-error")]
    [InlineData("empty")] [InlineData("receipt")] [InlineData("publish-warning")] [InlineData("publish-false")]
    [InlineData("range-warning")] [InlineData("record-warning")] [InlineData("head-warning")]
    public async Task EvidenceAndCancellationGateTheSoleCommitDispatch(string mode)
    {
        using var cancellation=new CancellationTokenSource();var observer=new Observer(mode,cancellation);var publisher=new Publisher{Mode=mode};
        if(mode=="pre-cancel")cancellation.Cancel();if(mode=="publish-cancel")publisher.DuringPublish=cancellation.Cancel;
        var result=await CosmicChainQueryIngestion.PublishAsync(observer,publisher,observer.Requirement,null,cancellation.Token);
        if(mode is "valid" or "publish-cancel" or "empty"){
            Assert.False(result.IsError,result.Message);Assert.True(result.IsSaved);Assert.Equal(1,publisher.Calls);
            Assert.Equal(mode=="empty"?0:1,publisher.Entries);Assert.Equal(1,observer.HeadCalls);
            Assert.Equal(mode=="empty"?0:1,observer.RecordCalls);
        }else{
            Assert.True(result.IsError);Assert.False(result.IsSaved);
            Assert.Equal(mode.StartsWith("publish-")?1:0,publisher.Calls);
            if(mode.StartsWith("publish-"))Assert.Equal("CQL_INDEX_PUBLICATION_UNCERTAIN",result.ErrorCode);
            if(mode=="pre-cancel")Assert.Equal(0,observer.RangeCalls);
        }
    }
    [Theory]
    [InlineData("valid")] [InlineData("replay")]
    public async Task SessionRetainsOnlyAcknowledgedCheckpointIncludingExactReplay(string mode)
    {
        using var cancellation=new CancellationTokenSource();
        var observer=new Observer("valid",cancellation);var publisher=new Publisher{Mode=mode};
        var session=new CosmicChainQueryIngestionSession(observer,publisher,observer.Requirement,null);
        CosmicChainQueryPosition? prior=null;
        publisher.BeforePublish=value=>Assert.Same(prior,value);
        for(var step=0;step<2;step++)
        {
            var result=await session.AdvanceAsync();
            Assert.False(result.IsError,result.Message);
            Assert.Same(result.Result.Position,session.AcknowledgedPosition);
            Assert.Equal(mode!="replay",result.IsSaved);
            prior=session.AcknowledgedPosition;
        }
        Assert.Null(session.FailureCode);Assert.Equal(2,publisher.Calls);
    }

    [Theory]
    [InlineData("publish-throw")] [InlineData("publish-warning")] [InlineData("publish-false")]
    [InlineData("range-error")] [InlineData("pre-cancel")]
    public async Task FailedSessionCannotRetryOrAdvanceItsAcknowledgedCursor(string mode)
    {
        using var cancellation=new CancellationTokenSource();
        var observer=new Observer(mode,cancellation);var publisher=new Publisher{Mode=mode};
        var session=new CosmicChainQueryIngestionSession(observer,publisher,observer.Requirement,null);
        if(mode=="pre-cancel")cancellation.Cancel();
        var first=await session.AdvanceAsync(cancellation.Token);
        Assert.True(first.IsError);Assert.Null(session.AcknowledgedPosition);
        var reads=observer.RangeCalls;var writes=publisher.Calls;
        var second=await session.AdvanceAsync();
        Assert.True(second.IsError);Assert.Equal(first.ErrorCode,second.ErrorCode);
        Assert.Equal(reads,observer.RangeCalls);Assert.Equal(writes,publisher.Calls);
        Assert.Equal(first.ErrorCode,session.FailureCode);
    }

    [Fact]
    public async Task ConcurrentSessionCallCannotQueueAnotherTraversal()
    {
        using var cancellation=new CancellationTokenSource();
        var observer=new Observer("valid",cancellation);var publisher=new Publisher();
        var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        observer.BeforeRange=async()=>{entered.SetResult();await release.Task;};
        var session=new CosmicChainQueryIngestionSession(observer,publisher,observer.Requirement,null);
        var first=session.AdvanceAsync();await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        try
        {
            var concurrent=await session.AdvanceAsync();
            Assert.Equal("CQL_INDEX_INGESTION_BUSY",concurrent.ErrorCode);
            Assert.Equal(1,observer.RangeCalls);Assert.Equal(0,publisher.Calls);
        }
        finally{release.SetResult();}
        Assert.False((await first).IsError);Assert.Equal(1,publisher.Calls);
    }

    private sealed class Observer:ICosmicChainQuerySourceObserver
    {
        private readonly string mode;private readonly CancellationTokenSource cancellation;
        private readonly CosmicChainQuerySource source=new(ProviderType.SolanaOASIS,"network",ProviderType.SQLLiteDBOASIS,Guid.NewGuid(),1,new string('A',64),0);
        private readonly CosmicChainQueryPosition position=new(1,"block");
        private readonly CosmicChainQueryObservedRecord record;
        public int RangeCalls,RecordCalls,HeadCalls;public Func<Task>? BeforeRange;
        public CosmicChainQueryReadRequirement Requirement=>new(source,position,0,TimeSpan.FromMinutes(1));
        public Observer(string mode,CancellationTokenSource cancellation){
            this.mode=mode;this.cancellation=cancellation;
            var json="canonical-source";var identity=new CosmicChainQueryRecordIdentity(Guid.NewGuid(),1,Guid.NewGuid(),false,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))));
            record=new(new(source,position,"original-commit",identity),json);
        }
        public async Task<OASISResult<CosmicChainQueryObservedRange>> ObserveRangeAsync(CosmicChainQuerySource selected,CancellationToken token){
            RangeCalls++;if(BeforeRange!=null)await BeforeRange();if(mode=="range-error")return new OASISResult<CosmicChainQueryObservedRange>{IsError=true,ErrorCode="controlled-source-error"};
            var head=new CosmicChainQueryFinalizedHead(source.SourceProvider,mode=="network"?"wrong":source.NetworkIdentity,position,DateTimeOffset.UtcNow);
            var rows=mode=="empty"?Array.Empty<CosmicChainQueryObservedRecord>():new[]{mode=="hash"?record with{CanonicalJson="altered"}:record};
            return new OASISResult<CosmicChainQueryObservedRange>(new(new(source,position,0),head,rows)){IsWarning=mode=="range-warning"};
        }
        public Task<OASISResult<CosmicChainQueryObservedRecord>> ObserveRecordAsync(CosmicChainQuerySource selected,string commit,CancellationToken token){
            RecordCalls++;if(mode=="record-cancel")cancellation.Cancel();
            if(mode=="record-error")return Task.FromResult(new OASISResult<CosmicChainQueryObservedRecord>{IsError=true,ErrorCode="source-absent"});
            var value=mode=="mismatch"?record with{CanonicalJson="changed"}:record;
            if(mode=="receipt")value=record with{Receipt=new(source,position,"another-commit",record.Receipt.Record)};
            return Task.FromResult(new OASISResult<CosmicChainQueryObservedRecord>(value){IsWarning=mode=="record-warning"});
        }
        public Task<OASISResult<CosmicChainQueryFinalizedHead>> ObserveHeadAsync(CosmicChainQuerySource selected,IReadOnlyList<long> requiredSlots,CancellationToken token){
            HeadCalls++;if(mode=="head-error")return Task.FromResult(new OASISResult<CosmicChainQueryFinalizedHead>{IsError=true,ErrorCode="head-unavailable"});
            return Task.FromResult(new OASISResult<CosmicChainQueryFinalizedHead>(new(source.SourceProvider,source.NetworkIdentity,position,DateTimeOffset.UtcNow-(mode=="stale"?TimeSpan.FromHours(1):TimeSpan.Zero))){IsWarning=mode=="head-warning"});
        }
    }
    private sealed class Publisher:ICosmicChainQueryIndexPublisher
    {
        public int Calls,Entries;public Action<CosmicChainQueryPosition>? BeforePublish;public Action? DuringPublish;public string Mode="";
        public OASISResult<bool> Publish(CosmicChainQueryReadRequirement requirement,CosmicChainQueryCheckpoint checkpoint,
            CosmicChainQueryFinalizedHead head,DateTimeOffset time,CosmicChainQueryPosition previous,IReadOnlyList<CosmicChainQueryIndexEntry> entries){
            Calls++;Entries=entries.Count;BeforePublish?.Invoke(previous);DuringPublish?.Invoke();
            if(Mode=="publish-throw")throw new IOException("controlled unknown outcome");
            if(Mode=="publish-cancel-throw")throw new OperationCanceledException();
            if(Mode=="publish-null")return null!;
            if(Mode=="publish-warning")return new(true){IsWarning=true};
            if(Mode=="publish-false")return new(false);
            if(Mode=="replay")return new(true);
            return new(true){IsSaved=true,SavedCount=entries.Count};
        }
    }
}
