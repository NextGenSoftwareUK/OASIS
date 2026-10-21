using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoOutboxAdmissionTests
{
    public class NoIoProxy : DispatchProxy
    {
        public int Calls;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method?.Name=="GetCollection")return Create<IMongoCollection<BsonDocument>,NoIoProxy>();
            Interlocked.Increment(ref Calls);throw new InvalidOperationException("Invalid admission reached native I/O.");
        }
    }
    [DataTestMethod]
    [DataRow("target")] [DataRow("worker")] [DataRow("duration")]
    [DataRow("claim")] [DataRow("outcome")] [DataRow("cancel")] [DataRow("peek")]
    public async Task InvalidDeliveryAdmissionDoesNotStartANativeSession(string fault)
    {
        var context=(MongoDbContext)RuntimeHelpers.GetUninitializedObject(typeof(MongoDbContext));
        context.MongoClient=DispatchProxy.Create<IMongoClient,NoIoProxy>();context.MongoDB=DispatchProxy.Create<IMongoDatabase,NoIoProxy>();
        var store=new MongoHolonReplicationOutbox(context);var client=(NoIoProxy)(object)context.MongoClient;
        using var cancellation=new CancellationTokenSource();if(fault=="cancel")cancellation.Cancel();
        if(fault=="cancel")
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1),cancellation.Token));
        else if(fault=="peek")Assert.IsTrue((await store.PeekNextAsync(" ")).IsError);
        else if(fault=="claim" || fault=="outcome")
        {
            var claim=new HolonDeliveryClaim(fault=="claim" ? 0:1,Guid.NewGuid(),"a","worker",Guid.NewGuid(),"{}",1,DateTime.UtcNow.AddMinutes(1));
            var result=fault=="claim" ? await store.RenewAsync(claim,TimeSpan.FromMinutes(1))
                :await store.FinishWithRecoveryAsync(claim,HolonDeliveryOutcome.Completed,new HolonDeliveryRecoveryEvidence(true,Array.Empty<string>()));
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        }
        else
        {
            var result=await store.ClaimNextAsync(fault=="target" ? " ":"a",fault=="worker" ? " ":"worker",fault=="duration" ? TimeSpan.Zero:TimeSpan.FromMinutes(1));
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        }
        Assert.AreEqual(0,client.Calls);
    }
    [TestMethod]
    public void InactiveSourceCaptureCannotConstructOrOpenANativeQueue()
    {
        var provider=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","unused");
        var result=provider.GetHolonReplicationOutbox();Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_OUTBOX_SOURCE_NOT_ACTIVATED",result.ErrorCode);
    }

}
