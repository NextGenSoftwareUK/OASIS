using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class NativeResourceAttemptBindingTests
{
    [Theory]
    [InlineData("receipt")]
    [InlineData("outer-list")]
    [InlineData("resource-list")]
    public async Task SettlementRetainsValidatedEvidenceUntilDriverSerialization(string mutation)
    {
        var receipt=new UsageProviderReceipt{Provider="AWSOASIS",ClientRequestId="route-original",ProviderRequestId="request-original",
            NativeResourceUsage=new(){new("AWSOASIS","request-original","sdk-request-original","table-original",
                "read-capacity-units","Total",0.5m,DateTime.UtcNow)}};
        var settlement=new UsageSettlementRequest{Outcome="succeeded",ProviderReceipts=new(){receipt}};
        var operation=new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),ExecutionOwner="owner",State="executing"};
        operation.ProviderAttempts.Add(new(){Provider="AWSOASIS",ClientRequestId="route-original",ProviderRequestId="request-original"});
        var collection=new Mock<IMongoCollection<UsageOutboxOperation>>(MockBehavior.Strict);
        collection.Setup(value=>value.FindAsync(It.IsAny<FilterDefinition<UsageOutboxOperation>>(),
            It.IsAny<FindOptions<UsageOutboxOperation,UsageOutboxOperation>>(),It.IsAny<CancellationToken>()))
            .Returns(()=>{
                var cursor=new Mock<IAsyncCursor<UsageOutboxOperation>>();
                cursor.SetupGet(value=>value.Current).Returns(new[]{operation});
                cursor.SetupSequence(value=>value.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);
                return Task.FromResult(cursor.Object);
            });
        MongoDB.Bson.BsonDocument persisted=null!;
        collection.Setup(value=>value.UpdateOneAsync(It.IsAny<FilterDefinition<UsageOutboxOperation>>(),
            It.IsAny<UpdateDefinition<UsageOutboxOperation>>(),It.IsAny<UpdateOptions>(),It.IsAny<CancellationToken>()))
            .Returns((FilterDefinition<UsageOutboxOperation> filter,UpdateDefinition<UsageOutboxOperation> update,UpdateOptions options,CancellationToken token)=>{
                // The driver may serialize after validation yields to its asynchronous transport.
                if(mutation=="receipt")receipt.ProviderRequestId="caller-replacement";
                else if(mutation=="outer-list")settlement.ProviderReceipts.Clear();
                else receipt.NativeResourceUsage.Clear();
                persisted=update.Render(new RenderArgs<UsageOutboxOperation>(
                    BsonSerializer.SerializerRegistry.GetSerializer<UsageOutboxOperation>(),BsonSerializer.SerializerRegistry)).AsBsonDocument;
                return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1,1,null));
            });
        var store=(MongoUsageOutboxStore)RuntimeHelpers.GetUninitializedObject(typeof(MongoUsageOutboxStore));
        typeof(MongoUsageOutboxStore).GetField("_operations",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(store,collection.Object);
        await store.EnqueueAsync(operation.Id,"owner",settlement,0,default);
        var saved=persisted["$set"]["Settlement"]["ProviderReceipts"].AsBsonArray;
        Assert.Single(saved);
        Assert.Equal("request-original",saved[0]["ProviderRequestId"].AsString);
        Assert.Single(saved[0]["NativeResourceUsage"].AsBsonArray);
        Assert.Equal("sdk-request-original",saved[0]["NativeResourceUsage"][0]["NativeRequestId"].AsString);
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach(var boundary in new[]{"aggregate","append","enqueue"})
        foreach(var mode in new[]{"orphan","changed-original","unacknowledged","duplicate","provider-mismatch","incomplete","bound","ordinary"})
            yield return new object[]{boundary,mode};
    }

    [Theory][MemberData(nameof(Cases))]
    public async Task ResourceEvidenceRequiresOneOriginalAdmittedAttemptAtEveryBoundary(string boundary,string mode)
    {
        var receipt=new UsageProviderReceipt{Provider="AWSOASIS",ClientRequestId="route-original",ProviderRequestId="request-original",
            Units=1,ActualCostUsd=0.25m,CostSource="controlled-service-tariff",PricingCatalogueVersion="controlled-v1"};
        if(mode!="ordinary")receipt.NativeResourceUsage=new(){new("AWSOASIS","request-original","sdk-request-original",
            "eu-west-1/table/OasisHolons","read-capacity-units","Total",mode=="incomplete"?null:0.5m,DateTime.UtcNow)};
        var operation=new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),UserId=Guid.NewGuid().ToString("D"),
            Service="resource-binding-tests",ExecutionOwner="owner-original",State="executing",
            Policy=new UsageEndpointPolicy{RequiresProviderMeasurement=false},Receipts=new(){receipt}};
        if(mode is not ("orphan" or "ordinary")){
            var attempt=new UsageProviderAttempt{Provider=mode=="provider-mismatch"?"other-provider":"AWSOASIS",
                ClientRequestId="route-original",ProviderRequestId=mode=="changed-original"?"different-request":mode=="unacknowledged"?null:"request-original",
                StartedAtUtc=DateTime.UtcNow};
            operation.ProviderAttempts.Add(attempt);
            if(mode=="duplicate")operation.ProviderAttempts.Add(attempt);
        }
        var collection=new Mock<IMongoCollection<UsageOutboxOperation>>(MockBehavior.Strict);
        collection.SetupGet(value=>value.DocumentSerializer).Returns(BsonSerializer.SerializerRegistry.GetSerializer<UsageOutboxOperation>());
        collection.SetupGet(value=>value.Settings).Returns(new MongoCollectionSettings());
        collection.SetupGet(value=>value.CollectionNamespace).Returns(new CollectionNamespace("resource-binding-tests","outbox"));
        collection.Setup(value=>value.FindAsync(It.IsAny<FilterDefinition<UsageOutboxOperation>>(),
            It.IsAny<FindOptions<UsageOutboxOperation,UsageOutboxOperation>>(),It.IsAny<CancellationToken>()))
            .Returns((FilterDefinition<UsageOutboxOperation> filter,FindOptions<UsageOutboxOperation,UsageOutboxOperation> options,CancellationToken token)=>{
                var cursor=new Mock<IAsyncCursor<UsageOutboxOperation>>();
                cursor.SetupGet(value=>value.Current).Returns(new[]{operation});
                cursor.SetupSequence(value=>value.MoveNextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true).ReturnsAsync(false);
                return Task.FromResult(cursor.Object);
            });
        var writes=0;
        collection.Setup(value=>value.UpdateOneAsync(It.IsAny<FilterDefinition<UsageOutboxOperation>>(),
            It.IsAny<UpdateDefinition<UsageOutboxOperation>>(),It.IsAny<UpdateOptions>(),It.IsAny<CancellationToken>()))
            .Callback(()=>writes++).ReturnsAsync(new UpdateResult.Acknowledged(1,1,null));
        // Isolate the shipping store boundary from network/constructor effects.
        // This is our own repository field, never an SDK private API.
        var store=(MongoUsageOutboxStore)RuntimeHelpers.GetUninitializedObject(typeof(MongoUsageOutboxStore));
        typeof(MongoUsageOutboxStore).GetField("_operations",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(store,collection.Object);
        async Task Invoke()
        {
            if(boundary=="aggregate"){
                var method=typeof(UsageProviderReceipt).Assembly.GetType("NextGenSoftware.OASIS.API.Core.Services.Subscriptions.UsageCanonical")!.GetMethod("Aggregate")!;
                try{method.Invoke(null,new object[]{operation,"succeeded"});}
                catch(TargetInvocationException failure) when(failure.InnerException!=null){ExceptionDispatchInfo.Capture(failure.InnerException).Throw();}
            }else if(boundary=="append"){
                operation.Receipts.Clear();
                await store.AppendReceiptAsync(operation.Id,operation.ExecutionOwner,receipt,default);
            }else{
                var settlement=new UsageSettlementRequest{OperationId=operation.Id,UserId=operation.UserId,ConsumingService=operation.Service,
                    Outcome="succeeded",ActualCostUsd=receipt.ActualCostUsd,Units=receipt.Units,Provider=receipt.Provider,
                    ProviderRequestId=receipt.ProviderRequestId,ProviderReceipts=new(){receipt}};
                await store.EnqueueAsync(operation.Id,operation.ExecutionOwner,settlement,operation.EvidenceRevision,default);
            }
        }
        if(mode is "bound" or "ordinary"){
            await Invoke();
            Assert.Equal(boundary=="aggregate"?0:1,writes);
        }else{
            await Assert.ThrowsAsync<UsageProtocolException>(Invoke);
            Assert.Equal(0,writes);
        }
    }
}
