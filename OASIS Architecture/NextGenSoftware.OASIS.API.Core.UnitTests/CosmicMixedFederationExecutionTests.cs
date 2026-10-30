using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    private CosmicFederationPlan<Holon> MixedPlan(CosmicFederationOptions? options=null,bool access=true,bool indexFirst=false)
    {
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(indexFirst?new[]{source.SourceProvider,ProviderType.MongoDBOASIS}:new[]{ProviderType.MongoDBOASIS,source.SourceProvider}).Result);
        if(access)query=query.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var plan=CosmicFederationPlanner.Plan(query,options??CosmicFederationOptions.Create(maximumProviderAttempts:2).Result,
            ProviderType.MongoDBOASIS,new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,Capabilities),
                new CosmicQueryTarget(source.SourceProvider,Capabilities,source)},null,
            new[]{new CosmicFederationIndexRequirement(new(source,position,0,TimeSpan.FromMinutes(1)))});
        Assert.False(plan.IsError,plan.Message);return plan.Result;
    }

    private Mock<ICosmicQueryStorageProvider> MixedMongo(bool empty=false)
    {
        var mongo=new Mock<ICosmicQueryStorageProvider>();
        mongo.SetupGet(adapter=>adapter.QueryProviderType).Returns(ProviderType.MongoDBOASIS);
        mongo.SetupGet(adapter=>adapter.QueryCapabilities).Returns(Capabilities);
        var rows=empty?Array.Empty<IHolon>():new IHolon[]{new Holon{Id=Guid.NewGuid(),IsPublic=true,Name="native Mongo"}};
        mongo.Setup(adapter=>adapter.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),It.IsAny<int>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IReadOnlyList<IHolon>>(rows){IsLoaded=true,LoadedCount=rows.Length});
        return mongo;
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task MixedDispatchPreservesIndexedReceiptAndSourceThroughMergeAndManager(bool typed)
    {
        var fixture=Fixture("rows");var mongo=MixedMongo();var plan=MixedPlan();
        var bindings=new[]{new CosmicFederationIndexedReader(fixture.Reader,Mock.Of<ICosmicChainQuerySourceObserver>())};
        var result=typed?await Manager().ExecuteMixedFederationQueryAsync(plan,new[]{mongo.Object},bindings)
            :await CosmicFederationExecutor.ExecuteMixedAsync(plan,new[]{mongo.Object},bindings);
        Assert.False(result.IsError,result.Message+result.Exception);Assert.Equal(2,result.Result.ObservedInputRows);
        var indexed=Assert.Single(result.Result.Rows,row=>row.Selected.Provider==source.SourceProvider).Selected;
        Assert.NotSame(fixture.Row,indexed.Row);Assert.Equal(fixture.Row.Id,indexed.IndexedEvidence.Receipt.Record.Id);
        Assert.Equal(source.IndexId,indexed.IndexedEvidence.Read.Checkpoint.Source.IndexId);
        Assert.Equal(source.ExecutionProvider,indexed.IndexedEvidence.Read.Checkpoint.Source.ExecutionProvider);
        Assert.Same(indexed.IndexedEvidence.Read,Assert.Single(result.Result.IndexedReads));
        Assert.Null(Assert.Single(result.Result.Rows,row=>row.Selected.Provider==ProviderType.MongoDBOASIS).Selected.IndexedEvidence);
        Assert.Equal(1,fixture.Reader.Calls);
        Assert.DoesNotContain("IndexedEvidence",indexed.Row.MetaData.Keys);
    }

    [Fact]
    public async Task EmptyIndexedReadRetainsFreshCheckpointWithoutInventingRows()
    {
        var fixture=Fixture("empty");var mongo=MixedMongo();
        var result=await CosmicFederationExecutor.ExecuteMixedAsync(MixedPlan(),new[]{mongo.Object},
            new[]{new CosmicFederationIndexedReader(fixture.Reader,Mock.Of<ICosmicChainQuerySourceObserver>())});
        Assert.False(result.IsError,result.Message);Assert.Single(result.Result.Rows);
        Assert.Equal(source.IndexId,Assert.Single(result.Result.IndexedReads).Checkpoint.Source.IndexId);
    }

    [Theory]
    [InlineData("missing")] [InlineData("changed-source")] [InlineData("duplicate")]
    [InlineData("access")] [InlineData("cancel")]
    public async Task MixedPreflightRejectsInvalidReadSetBeforeEitherLeafReads(string mode)
    {
        var fixture=Fixture("rows");var mongo=MixedMongo();
        var binding=new CosmicFederationIndexedReader(fixture.Reader,Mock.Of<ICosmicChainQuerySourceObserver>());
        if(mode=="changed-source")fixture.Reader.Source=new(source.SourceProvider,source.NetworkIdentity,source.ExecutionProvider,Guid.NewGuid(),2,source.CatalogueHash,0);
        var bindings=mode=="missing"?Array.Empty<CosmicFederationIndexedReader>():mode=="duplicate"?new[]{binding,binding}:new[]{binding};
        var result=await CosmicFederationExecutor.ExecuteMixedAsync(MixedPlan(access:mode!="access"),new[]{mongo.Object},bindings,
            new CancellationToken(mode=="cancel"));
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(0,fixture.Reader.Calls);
        mongo.Verify(adapter=>adapter.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),It.IsAny<int>(),It.IsAny<CancellationToken>()),Times.Never);
    }

    [Theory]
    [InlineData("stale-empty")] [InlineData("mismatch")] [InlineData("hidden")]
    [InlineData("duplicate")] [InlineData("incomplete")]
    public async Task IndexedFailureAfterNativeSuccessNeverPublishesPartialFederation(string mode)
    {
        var fixture=Fixture(mode);var mongo=MixedMongo();
        var result=await CosmicFederationExecutor.ExecuteMixedAsync(MixedPlan(),new[]{mongo.Object},
            new[]{new CosmicFederationIndexedReader(fixture.Reader,Mock.Of<ICosmicChainQuerySourceObserver>())});
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Equal(1,fixture.Reader.Calls);
        mongo.Verify(adapter=>adapter.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),It.IsAny<int>(),It.IsAny<CancellationToken>()),Times.Once);
    }

    [Fact]
    public async Task IndexedEvidenceConsumesInputBudgetEvenWhenBothRowSetsAreEmpty()
    {
        var fixture=Fixture("empty");var mongo=MixedMongo(empty:true);
        var options=CosmicFederationOptions.Create(maximumInputBytes:1,maximumProviderAttempts:2).Result;
        var result=await CosmicFederationExecutor.ExecuteMixedAsync(MixedPlan(options),new[]{mongo.Object},
            new[]{new CosmicFederationIndexedReader(fixture.Reader,Mock.Of<ICosmicChainQuerySourceObserver>())});
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.Contains("LIMIT",result.ErrorCode);
        Assert.Equal(1,fixture.Reader.Calls);
    }

    [Fact]
    public async Task MixedPreparedReadRequiresExactIndexedBillingBeforeNativeDispatch()
    {
        var fixture=Fixture("rows");var mongo=MixedMongo();
        var store=new Mock<NextGenSoftware.OASIS.API.Core.Services.Subscriptions.IUsageOutboxStore>(MockBehavior.Strict);
        var handle=new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.UsageExecutionHandle(
            new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.UsageOutboxOperation{Id="mixed-index-owner",UserId=actor.ToString("D"),ExecutionOwner="owner",
                Policy=new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.UsageEndpointPolicy()},
            store.Object,new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.Web4UsageLedgerOptions(),
            new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.SubscriptionAuthorizationResult(),
            Mock.Of<NextGenSoftware.OASIS.API.Core.Services.Subscriptions.IUsageReservationAuthority>());
        var prepared=new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.PreparedStorageExecution(
            handle,"mixed-index-owner",new Dictionary<ProviderType,string>(),Array.Empty<ProviderType>());
        var result=await prepared.RunAsync(()=>CosmicFederationExecutor.ExecuteMixedAsync(MixedPlan(indexFirst:true),new[]{mongo.Object},
            new[]{new CosmicFederationIndexedReader(fixture.Reader,Mock.Of<ICosmicChainQuerySourceObserver>())}));
        Assert.Equal("CQL_INDEXED_BILLING_UNSUPPORTED",result.ErrorCode);Assert.Null(result.Result);
        Assert.Equal(0,fixture.Reader.Calls);
        mongo.Verify(adapter=>adapter.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),It.IsAny<int>(),It.IsAny<CancellationToken>()),Times.Never);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EmptyIndexedAssuranceIsIncludedInOutputByteBudget()
    {
        var fixture=Fixture("empty");var mongo=MixedMongo(empty:true);
        var options=CosmicFederationOptions.Create(maximumOutputBytes:500,maximumProviderAttempts:2).Result;
        var result=await CosmicFederationExecutor.ExecuteMixedAsync(MixedPlan(options),new[]{mongo.Object},
            new[]{new CosmicFederationIndexedReader(fixture.Reader,Mock.Of<ICosmicChainQuerySourceObserver>())});
        Assert.Equal("CQL_FEDERATION_OUTPUT_LIMIT",result.ErrorCode);Assert.Null(result.Result);
        Assert.Equal(1,fixture.Reader.Calls);
    }
}
