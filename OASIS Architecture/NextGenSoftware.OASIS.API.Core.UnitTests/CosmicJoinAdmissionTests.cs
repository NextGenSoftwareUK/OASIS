using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicJoinAdmissionTests
{
    private static CosmicQueryCapabilities Sources=>new(CosmicQueryFeatures.Or,new[] {
        new CosmicQueryFieldCapability("IsPublic",typeof(bool),CosmicQueryComparisonOperator.Equal),
        new CosmicQueryFieldCapability("CreatedByAvatarId",typeof(Guid),CosmicQueryComparisonOperator.Equal) });
    private static CosmicJoinPlan<Holon,Holon,string,string> Plan(Guid? actor,params ProviderType[] providers)
    {
        var scope=CosmicQueryScope.Named(providers).Result;
        var left=CosmicQuery<Holon>.Create(scope);var right=CosmicQuery<Holon>.Create(scope);
        if(actor.HasValue)
        {
            var access=CosmicQueryReadAccess.OwnerOrPublic(actor.Value).Result;
            left=left.WithReadAccess(access).Result;right=right.WithReadAccess(access).Result;
        }
        var query=left.Join(right,row=>row.Name,row=>row.Name).Select((owner,member)=>owner.Description).Result;
        return CosmicJoinPlanner.Plan(query,providers[0],providers.Select(provider=>
            new CosmicJoinTarget(provider,Sources,CosmicJoinExecutorTests.Capabilities)).ToArray()).Result;
    }
    private static PreparedStorageExecution Policy(Mock<IUsageOutboxStore> store,Guid? user,params ProviderType[] allowed)
    {
        var operation=new UsageOutboxOperation {Id="join-admission",ExecutionOwner="owner",Policy=new UsageEndpointPolicy(),UserId=user?.ToString("D")??""};
        var handle=new UsageExecutionHandle(operation,store.Object,new Web4UsageLedgerOptions(),
            new SubscriptionAuthorizationResult(),Mock.Of<IUsageReservationAuthority>());
        return new PreparedStorageExecution(handle,"join-admission",new Dictionary<ProviderType,string>(),allowed,
            allowed.Select(provider=>new StorageQueryTariff(provider,0.001m,"join-test-v1")).ToArray());
    }
    private static Mock<ICosmicQueryJoinStorageProvider> Adapter(ProviderType provider,Action native)
    {
        var storage=new Mock<IOASISStorageProvider>();
        storage.SetupGet(value=>value.ProviderType).Returns(new EnumValue<ProviderType>(provider));
        var join=storage.As<ICosmicQueryJoinStorageProvider>();
        join.SetupGet(value=>value.QueryProviderType).Returns(provider);
        join.SetupGet(value=>value.QueryCapabilities).Returns(Sources);
        join.SetupGet(value=>value.JoinCapabilities).Returns(CosmicJoinExecutorTests.Capabilities);
        join.Setup(value=>value.ExecuteCosmicJoinAsync(It.IsAny<CosmicJoinPlan<Holon,Holon,string,string>>(),
            It.IsAny<CosmicJoinExecutionLimits>(),It.IsAny<CancellationToken>())).Returns(()=>{
                native();var left=Guid.NewGuid();var right=Guid.NewGuid();
                var packet=new CosmicJoinPacket(new[]{new CosmicJoinInputRow(left,true,Guid.Empty,new object[]{"coffee"},new object[]{"coffee"})},
                    new[]{new CosmicJoinInputRow(right,true,Guid.Empty,new object[]{"coffee"},Array.Empty<object>())},
                    new[]{new CosmicJoinRow(left,right,"coffee")},1,1,1);
                return Task.FromResult(new OASISResult<CosmicJoinPacket>(packet){IsLoaded=true}); });
        return join;
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task OneDurableAttemptPrecedesTheWholeJoinAndUsesTheExistingQueryReceipt(ProviderType provider)
    {
        var events=new List<string>();var admitted=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        store.Setup(value=>value.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderAttempt>(),It.IsAny<CancellationToken>()))
            .Returns((string id,string owner,UsageProviderAttempt attempt,CancellationToken token)=>{
                Assert.Equal(provider.ToString(),attempt.Provider);Assert.Equal("join-admission.route.1.non-native",attempt.ClientRequestId);
                events.Add("attempt");return admitted.Task; });
        store.Setup(value=>value.AppendReceiptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderReceipt>(),It.IsAny<CancellationToken>()))
            .Returns((string id,string owner,UsageProviderReceipt receipt,CancellationToken token)=>{
                Assert.Equal(provider.ToString(),receipt.Provider);Assert.Equal("join-admission.route.1.non-native",receipt.ProviderRequestId);
                Assert.Equal(1,receipt.Units);Assert.Equal(0.001m,receipt.ActualCostUsd);
                Assert.Equal("CQL-service-query-attempt-tariff",receipt.CostSource);events.Add("receipt");return Task.CompletedTask; });
        var join=Adapter(provider,()=>events.Add("native"));
        var pending=Policy(store,null,provider).RunAsync(()=>CosmicJoinExecutor.ExecuteAsync(Plan(null,provider),new ICosmicQueryStorageProvider[]{join.Object}));
        Assert.Equal(new[]{"attempt"},events);admitted.SetResult();
        var result=await pending;
        Assert.False(result.IsError,result.Message+result.Exception);
        Assert.Equal("coffee",result.Result.Providers.Single().Rows.Single().Value);
        Assert.Equal(new[]{"attempt","native","receipt"},events);
        join.Verify(value=>value.ExecuteCosmicJoinAsync(It.IsAny<CosmicJoinPlan<Holon,Holon,string,string>>(),
            It.IsAny<CosmicJoinExecutionLimits>(),It.IsAny<CancellationToken>()),Times.Once);
    }

    [Fact]
    public async Task AJoinCannotBypassAnInheritedProviderDenial()
    {
        var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);var calls=0;
        var join=Adapter(ProviderType.MongoDBOASIS,()=>calls++);
        var result=await Policy(store,null).RunAsync(()=>CosmicJoinExecutor.ExecuteAsync(Plan(null,ProviderType.MongoDBOASIS),new ICosmicQueryStorageProvider[]{join.Object}));
        Assert.Equal(PreparedStorageExecution.RejectedCode,result.ErrorCode);Assert.Equal(0,calls);store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AForbiddenLaterTargetPreventsTheFirstDurableAttemptToo()
    {
        var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);var calls=0;
        var first=Adapter(ProviderType.MongoDBOASIS,()=>calls++);var second=Adapter(ProviderType.Neo4jOASIS,()=>calls++);
        var result=await Policy(store,null,ProviderType.MongoDBOASIS).RunAsync(()=>CosmicJoinExecutor.ExecuteAsync(
            Plan(null,ProviderType.MongoDBOASIS,ProviderType.Neo4jOASIS),new ICosmicQueryStorageProvider[]{first.Object,second.Object}));
        Assert.Equal(PreparedStorageExecution.RejectedCode,result.ErrorCode);Assert.Equal(0,calls);store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BothBoundSourcesMustBelongToTheInheritedActor()
    {
        var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);var calls=0;
        var join=Adapter(ProviderType.MongoDBOASIS,()=>calls++);
        var result=await Policy(store,Guid.NewGuid(),ProviderType.MongoDBOASIS).RunAsync(()=>CosmicJoinExecutor.ExecuteAsync(
            Plan(Guid.NewGuid(),ProviderType.MongoDBOASIS),new ICosmicQueryStorageProvider[]{join.Object}));
        Assert.Equal("CQL_ACTOR_MISMATCH",result.ErrorCode);Assert.Equal(0,calls);store.VerifyNoOtherCalls();
    }
}