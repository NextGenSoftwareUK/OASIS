using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    private sealed class CardinalityReader(CosmicChainQuerySource source,CosmicChainQueryPosition position,string mode):ICosmicChainQueryIndexScalarReader
    {
        public int Calls,Physical;
        public CosmicChainQuerySource Source=>source;
        public CosmicQueryCapabilities IndexedRowCapabilities=>Capabilities;
        public CosmicScalarCapabilities IndexedScalarCapabilities=>new(new[]{CosmicScalarOperation.Count,CosmicScalarOperation.LongCount,CosmicScalarOperation.Any},
            Array.Empty<CosmicScalarSelectionCapability>(),false,false,false,false,false);
        public Task<OASISResult<CosmicChainIndexRows>> QueryRowsAsync<T>(CosmicQueryPlan<T> plan,ICosmicChainQuerySourceObserver observer,
            int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon=>throw new InvalidOperationException("Do not count a row page.");
        public async Task<OASISResult<CosmicChainIndexCardinality>> QueryCardinalityAsync<T,TResult>(CosmicScalarPlan<T,TResult> plan,
            ICosmicChainQuerySourceObserver observer,CancellationToken cancellationToken=default) where T:IHolon
        {
            Calls++;
            if(mode!="unmetered"){
                await CosmicIndexedReadAttempts.ExecuteIndexSnapshotAsync(source,()=>Task.FromResult(++Physical),cancellationToken);
                foreach(var operation in new[]{CosmicIndexedReadOperation.SourceGenesis,CosmicIndexedReadOperation.SourceSlot,CosmicIndexedReadOperation.SourceBlock})
                    await CosmicIndexedReadAttempts.ExecuteSourceRpcAsync(source.SourceProvider,operation,()=>Task.FromResult(++Physical),cancellationToken);
            }
            return new(new(new(source,position,0),new(source.SourceProvider,source.NetworkIdentity,position,
                DateTimeOffset.UtcNow-(mode=="stale"?TimeSpan.FromHours(1):TimeSpan.Zero)),mode=="operation"?CosmicScalarOperation.Any:plan.Query.Operation,
                mode=="operation"?1:mode=="overflow"?(long)int.MaxValue+1:2,mode=="overflow"?(long)int.MaxValue+1:3,new string('A',64))){IsLoaded=mode!="incomplete",IsWarning=mode=="warning"};
        }
    }

    [Theory]
    [InlineData("valid",null)] [InlineData("operation","CQL_PROVIDER_RESULT_INVALID")]
    [InlineData("warning","CQL_PROVIDER_RESULT_INVALID")] [InlineData("incomplete","CQL_PROVIDER_RESULT_INVALID")]
    [InlineData("stale","CQL_SOURCE_OBSERVATION_EXPIRED")] [InlineData("window","CQL_PROVIDER_RESULT_INVALID")]
    [InlineData("overflow","CQL_AGGREGATE_OVERFLOW")]
    public async Task IndexedCardinalityRechecksExactOperationAcknowledgementFreshnessAndWindow(string mode,string? error)
    {
        var reader=new CardinalityReader(source,position,mode);
        var query=Plan().Query;if(mode=="window")query=query.Take(1).Result;
        var plan=CosmicScalarPlanner.Plan(query.Count().Result,source.SourceProvider,new[]{
            new CosmicScalarTarget(source.SourceProvider,reader.IndexedRowCapabilities,reader.IndexedScalarCapabilities,source)});
        Assert.False(plan.IsError,plan.Message);
        var result=await CosmicIndexedScalarExecutor.ExecuteAsync(plan.Result,reader,Mock.Of<ICosmicChainQuerySourceObserver>());
        Assert.Equal(error,result.ErrorCode);Assert.Equal(1,reader.Calls);
        if(error==null){
            Assert.Equal(2,result.Result.Value);
            var json=System.Text.Json.JsonSerializer.Serialize(result.Result);
            Assert.DoesNotContain("HistoryCount",json);Assert.DoesNotContain("HistoryHash",json);
        }else Assert.Null(result.Result);
    }

    [Fact]
    public async Task IndexedLongCountRetainsWideIntegerDomainWithoutMaterializedRows()
    {
        var reader=new CardinalityReader(source,position,"overflow");
        var plan=CosmicScalarPlanner.Plan(Plan().Query.LongCount().Result,source.SourceProvider,new[]{
            new CosmicScalarTarget(source.SourceProvider,reader.IndexedRowCapabilities,reader.IndexedScalarCapabilities,source)});
        Assert.False(plan.IsError,plan.Message);
        var result=await CosmicIndexedScalarExecutor.ExecuteAsync(plan.Result,reader,Mock.Of<ICosmicChainQuerySourceObserver>());
        Assert.False(result.IsError,result.Message);Assert.Equal((long)int.MaxValue+1,result.Result.Value);
    }

    [Theory]
    [InlineData("valid",null)] [InlineData("missing","CQL_INDEXED_BILLING_UNSUPPORTED")]
    [InlineData("unmetered","CQL_INDEXED_CAPTURE_INCOMPLETE")]
    [InlineData("begin","CQL_INDEXED_ATTEMPT_UNCERTAIN")] [InlineData("receipt","CQL_INDEXED_ATTEMPT_UNCERTAIN")]
    public async Task PreparedCardinalityCapturesSqlAndHeadWithoutInventingSourceRecordReceipts(string mode,string? error)
    {
        var reader=new CardinalityReader(source,position,mode);var receipts=new List<UsageProviderReceipt>();
        var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        store.Setup(s=>s.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderAttempt>(),It.IsAny<CancellationToken>())).Returns(()=>mode=="begin"?Task.FromException(new IOException("unknown begin")):Task.CompletedTask);
        store.Setup(s=>s.AppendReceiptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderReceipt>(),It.IsAny<CancellationToken>()))
            .Returns((string id,string owner,UsageProviderReceipt receipt,CancellationToken token)=>{
                if(mode=="receipt")throw new IOException("unknown acknowledgement");receipts.Add(receipt);return Task.CompletedTask;});
        var handle=new UsageExecutionHandle(new UsageOutboxOperation{Id="index-cardinality",UserId=actor.ToString("D"),
            Policy=new UsageEndpointPolicy{RequiresProviderMeasurement=true,ReservedUnits=100,EstimatedCostUsd=1,MaximumProviderAttempts=100}},store.Object,new(),new(),Mock.Of<IUsageReservationAuthority>());
        var prepared=new PreparedStorageExecution(handle,"index-cardinality",new Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string>(),
            Array.Empty<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(),null,null,false,mode=="missing"?Array.Empty<CosmicIndexedReadTariffPolicy>():new[]{Tariffs()});
        var plan=CosmicScalarPlanner.Plan(Plan().Query.Count().Result,source.SourceProvider,new[]{
            new CosmicScalarTarget(source.SourceProvider,reader.IndexedRowCapabilities,reader.IndexedScalarCapabilities,source)}).Result;
        var result=await prepared.RunAsync(()=>CosmicIndexedScalarExecutor.ExecuteAsync(plan,reader,Mock.Of<ICosmicChainQuerySourceObserver>()));
        Assert.Equal(error,result.ErrorCode);
        Assert.Equal(mode=="missing"?0:1,reader.Calls);
        Assert.Equal(mode=="valid"?4:0,receipts.Count);
        Assert.Equal(mode=="missing"||mode=="unmetered"||mode=="begin"?0:mode=="receipt"?1:4,reader.Physical);
        Assert.DoesNotContain(receipts,r=>r.Model.EndsWith(":"+CosmicIndexedReadOperation.SourceTransaction));
        if(error==null)Assert.Equal(2,result.Result.Value);else Assert.Null(result.Result);
    }
}
