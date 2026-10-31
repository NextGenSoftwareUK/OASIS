using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class NativeResourceHostCaptureTests
{
    [Theory][InlineData(false)][InlineData(true)]
    public async Task MissingResourceEvidenceReconcilesWithoutReplacingAnObservedOutcome(bool closed)
    {
        var operation=new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),ExecutionOwner="owner",Policy=new(),ObservedOutcome="succeeded"};
        operation.ProviderAttempts.Add(new(){Provider="AWSOASIS",ClientRequestId="route",ProviderRequestId="route",ResourceCaptureExpected=true,ResourceCaptureCount=closed?1:null});
        operation.ResourceObservations.Add(new(){Provider="AWSOASIS",ClientRequestId="route",ProviderRequestId="route",Sequence=1,NativeOperation="GetItem",
            Observations=new(){new("AWSOASIS","route",null,"region/table/original","capacity-units","Total",null,DateTime.UtcNow)}});
        var store=new Mock<IUsageOutboxStore>();
        store.Setup(value=>value.GetAsync(operation.Id,It.IsAny<CancellationToken>())).ReturnsAsync(operation);
        store.Setup(value=>value.RecordObservedOutcomeAsync(operation.Id,"owner",It.IsAny<string>(),It.IsAny<CancellationToken>()))
            .Callback<string,string,string,CancellationToken>((_,_,outcome,_)=>Assert.Equal("succeeded",outcome)).ReturnsAsync(operation);
        var handle=new UsageExecutionHandle(operation,store.Object,new(),new(),Mock.Of<IUsageReservationAuthority>());
        await handle.CompleteOrReconcileAsync("failed");
        store.Verify(value=>value.MarkIndeterminateAsync(operation.Id,"owner",It.IsAny<string>(),It.IsAny<CancellationToken>()),Times.Once);
        Assert.Equal("succeeded",operation.ObservedOutcome);
    }

    private static (Mock<IUsageOutboxStore> Store,Mock<IOASISStorageProvider> Provider,PreparedStorageExecution Policy) Fixture()
    {
        var store=new Mock<IUsageOutboxStore>();var provider=new Mock<IOASISStorageProvider>();
        provider.SetupGet(value=>value.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.AWSOASIS));
        var handle=new UsageExecutionHandle(new(){Id=Guid.NewGuid().ToString("D"),ExecutionOwner="owner",Policy=new()},
            store.Object,new(),new(),Mock.Of<IUsageReservationAuthority>());
        return(store,provider,new PreparedStorageExecution(handle,"resource-host",new Dictionary<ProviderType,string>(),new[]{ProviderType.AWSOASIS}));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task HostAwaitsBothRawObservationPersistenceAndCaptureClosure(bool closing)
    {
        var (store,provider,policy)=Fixture();
        var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task Block(){entered.TrySetResult();return release.Task;}
        if(closing)store.Setup(value=>value.CompleteResourceCaptureAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),1,It.IsAny<CancellationToken>())).Returns(Block);
        else store.Setup(value=>value.AppendResourceObservationAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageResourceObservationBatch>(),It.IsAny<CancellationToken>())).Returns(Block);
        provider.As<INativeResourceUsageProvider>().Setup(value=>value.ObserveNativeResourceUsageAsync(It.IsAny<string>(),
                It.IsAny<Func<Task<OASISResult<int>>>>(),It.IsAny<Func<string,IReadOnlyList<NativeResourceUsageObservation>,Task>>()))
            .Returns(async(string request,Func<Task<OASISResult<int>>> body,Func<string,IReadOnlyList<NativeResourceUsageObservation>,Task> observer)=>{
                await observer("GetItem",new[]{new NativeResourceUsageObservation("AWSOASIS",request,"native-original","region/table/original","read-capacity-units","Total",0.5m,DateTime.UtcNow)});
                return await body();
            });
        var pending=policy.ExecuteAsync(provider.Object,()=>Task.FromResult(new OASISResult<int>(7)));
        try{await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));Assert.False(pending.IsCompleted);}
        finally{release.TrySetResult();}
        Assert.False((await pending.WaitAsync(TimeSpan.FromSeconds(5))).IsError);
    }

    [Theory][InlineData(false)][InlineData(true)]
    public async Task OriginalNativeFailureAndClosureFailureCannotDisappear(bool closureFails)
    {
        var (store,provider,policy)=Fixture();
        var original=new IOException("original native request failed");var closure=new TimeoutException("capture persistence deadline");
        if(closureFails)store.Setup(value=>value.CompleteResourceCaptureAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<string>(),0,It.IsAny<CancellationToken>())).ThrowsAsync(closure);
        provider.As<INativeResourceUsageProvider>().Setup(value=>value.ObserveNativeResourceUsageAsync(It.IsAny<string>(),
                It.IsAny<Func<Task<OASISResult<int>>>>(),It.IsAny<Func<string,IReadOnlyList<NativeResourceUsageObservation>,Task>>()))
            .ThrowsAsync(original);
        var result=await policy.ExecuteAsync(provider.Object,()=>Task.FromResult(new OASISResult<int>(7)));
        Assert.True(result.IsError);
        if(closureFails){var failure=Assert.IsType<InvalidDataException>(result.Exception);
            var both=Assert.IsType<AggregateException>(failure.InnerException);Assert.Equal(new Exception[]{original,closure},both.InnerExceptions);}
        else Assert.Same(original,result.Exception);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OrdinaryPreparedHostRetainsActualSdkObservationsBeforeReturning(bool incomplete)
    {
        var store=new Mock<IUsageOutboxStore>();
        UsageProviderAttempt? admitted=null;
        store.Setup(value=>value.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderAttempt>(),It.IsAny<CancellationToken>()))
            .Callback<string,string,UsageProviderAttempt,CancellationToken>((_,_,attempt,_)=>admitted=attempt).Returns(Task.CompletedTask);
        var operation=new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),ExecutionOwner="owner",Policy=new()};
        var handle=new UsageExecutionHandle(operation,store.Object,new Web4UsageLedgerOptions(),new SubscriptionAuthorizationResult(),Mock.Of<IUsageReservationAuthority>());
        var policy=new PreparedStorageExecution(handle,"resource-host",new Dictionary<ProviderType,string>(),new[]{ProviderType.AWSOASIS});
        var provider=new Mock<IOASISStorageProvider>();
        provider.SetupGet(value=>value.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.AWSOASIS));
        var observed=false;
        provider.As<INativeResourceUsageProvider>().Setup(value=>value.ObserveNativeResourceUsageAsync(
                It.IsAny<string>(),It.IsAny<Func<Task<OASISResult<int>>>>(),
                It.IsAny<Func<string,IReadOnlyList<NativeResourceUsageObservation>,Task>>()))
            .Returns(async(string request,Func<Task<OASISResult<int>>> body,Func<string,IReadOnlyList<NativeResourceUsageObservation>,Task> observer)=>{
                observed=true;
                Assert.NotNull(admitted);Assert.Equal(request,admitted.ProviderRequestId);
                await observer("GetItem",new[]{new NativeResourceUsageObservation("AWSOASIS",request,incomplete?null:"native-original",
                    "eu-west-1/table/OasisHolons","read-capacity-units","Total",incomplete?null:0.5m,DateTime.UtcNow)});
                return await body();
            });
        var bodyCalls=0;
        var result=await policy.ExecuteAsync(provider.Object,()=>{bodyCalls++;return Task.FromResult(new OASISResult<int>(7));});
        Assert.False(result.IsError,result.Exception?.ToString());Assert.Equal(7,result.Result);Assert.Equal(1,bodyCalls);
        Assert.True(observed,"Shipping prepared host must invoke the provider's existing SDK observer.");
        var recorded=Assert.IsType<UsageResourceObservationBatch>(Assert.Single(store.Invocations.Where(call=>call.Method.Name==nameof(IUsageOutboxStore.AppendResourceObservationAsync))).Arguments[2]);
        Assert.Equal(incomplete?null:0.5m,Assert.Single(recorded.Observations).Quantity);
        Assert.Equal(incomplete?null:"native-original",recorded.Observations[0].NativeRequestId);
        Assert.True(admitted!.ResourceCaptureExpected);
        Assert.Single(store.Invocations.Where(call=>call.Method.Name==nameof(IUsageOutboxStore.CompleteResourceCaptureAsync)));
        Assert.DoesNotContain(store.Invocations,call=>call.Method.Name==nameof(IUsageOutboxStore.AppendReceiptAsync));
    }
}
