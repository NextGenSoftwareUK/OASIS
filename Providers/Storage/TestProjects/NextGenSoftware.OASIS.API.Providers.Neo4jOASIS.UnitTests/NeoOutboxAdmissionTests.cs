using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class NeoOutboxAdmissionTests
{
    public class NoIoDriver : DispatchProxy
    {
        public int Calls;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {Calls++;throw new InvalidOperationException("Admission must precede driver I/O.");}
    }
    [DataTestMethod]
    [DataRow(0)] [DataRow(1)] [DataRow(2)] [DataRow(3)] [DataRow(4)] [DataRow(5)] [DataRow(6)]
    public async Task InvalidDeliveryInputIsRejectedBeforeSessionCreation(int scenario)
    {
        var driver=DispatchProxy.Create<IDriver,NoIoDriver>();var probe=(NoIoDriver)driver;
        var store=new NeoHolonReplicationOutbox(driver);
        var claim=new HolonDeliveryClaim(1,Guid.NewGuid(),"target","worker",Guid.NewGuid(),"payload",1,DateTime.UtcNow.AddMinutes(1));
        bool error;
        switch(scenario)
        {
            case 0:error=(await store.ClaimNextAsync(" ","worker",TimeSpan.FromSeconds(1))).IsError;break;
            case 1:error=(await store.ClaimNextAsync("target","worker",TimeSpan.Zero)).IsError;break;
            case 2:error=(await store.RenewAsync(null!,TimeSpan.FromSeconds(1))).IsError;break;
            case 3:error=(await store.FinishAsync(claim,HolonDeliveryOutcome.Completed,"contradictory")).IsError;break;
            case 4:error=(await store.FinishWithRecoveryAsync(claim,HolonDeliveryOutcome.Completed,new(true,new[]{"native"}))).IsError;break;
            case 5:error=(await store.InspectAsync(0)).IsError;break;
            default:error=(await store.PeekNextAsync(" ")).IsError;break;
        }
        Assert.IsTrue(error);Assert.AreEqual(0,probe.Calls);
    }
    public class ResourceProbe : DispatchProxy
    {
        public object? Next;
        public bool RunFails,RollbackFails,DisposeFails;
        public int Rollbacks,Disposals;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            switch(method!.Name)
            {
                case "AsyncSession": return Next;
                case "BeginTransactionAsync": return Task.FromResult((IAsyncTransaction)Next!);
                case "RunAsync": return RunFails ? Task.FromException<IResultCursor>(new IOException("owned read failure")):Task.FromResult((IResultCursor)Next!);
                case "FetchAsync": return Task.FromResult(false);
                case "RollbackAsync": Rollbacks++;return RollbackFails ? Task.FromException(new IOException("owned rollback failure")):Task.CompletedTask;
                case "Dispose": Disposals++;if(DisposeFails)throw new IOException("owned disposal failure");return null;
                default:throw new InvalidOperationException("Unexpected outbox resource call "+method.Name);
            }
        }
    }
    [DataTestMethod]
    [DataRow(false,true,false)] [DataRow(false,false,true)] [DataRow(false,true,true)]
    [DataRow(true,false,true)] [DataRow(true,true,true)]
    public async Task ReadOutcomeAndRecoveryRetainAllCleanupFailures(bool runFails,bool rollbackFails,bool disposalFails)
    {
        var cursor=DispatchProxy.Create<IResultCursor,ResourceProbe>();
        var tx=DispatchProxy.Create<IAsyncTransaction,ResourceProbe>();var transaction=(ResourceProbe)tx;
        transaction.Next=cursor;transaction.RunFails=runFails;transaction.RollbackFails=rollbackFails;transaction.DisposeFails=disposalFails;
        var session=DispatchProxy.Create<IAsyncSession,ResourceProbe>();((ResourceProbe)session).Next=tx;((ResourceProbe)session).DisposeFails=disposalFails;
        var driver=DispatchProxy.Create<IDriver,ResourceProbe>();((ResourceProbe)driver).Next=session;
        var result=await new NeoHolonReplicationOutbox(driver).InspectAsync(1);
        Assert.AreEqual(1,transaction.Rollbacks,"Rollback has exactly one owner even when it fails.");
        Assert.AreEqual(1,transaction.Disposals);Assert.AreEqual(1,((ResourceProbe)session).Disposals);
        Assert.IsNotNull(result.Exception);var causes=result.Exception is AggregateException aggregate ? aggregate.Flatten().InnerExceptions.ToArray():new[]{result.Exception};
        if(!runFails)
        {
            Assert.IsFalse(result.IsError);Assert.IsTrue(result.IsWarning);Assert.IsNull(result.Result);
            Assert.AreEqual((rollbackFails ? 1:0)+(disposalFails ? 2:0),causes.Length);
        }
        else
        {
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(rollbackFails ? "COSMIC_OUTBOX_RECONCILIATION_REQUIRED":"COSMIC_OUTBOX_OPERATION_FAILED",result.ErrorCode);
            Assert.AreEqual(rollbackFails,NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
            StringAssert.Contains(result.Exception.ToString(),"owned read failure");
            StringAssert.Contains(result.Exception.ToString(),"owned disposal failure");
            if(rollbackFails)StringAssert.Contains(result.Exception.ToString(),"owned rollback failure");
        }
    }

    [TestMethod]
    public void InactiveSourceCaptureDoesNotVerifyOrOpenTheDriver()
    {
        var driver=DispatchProxy.Create<IDriver,NoIoDriver>();var probe=(NoIoDriver)driver;
        var provider=new NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS("bolt://127.0.0.1:17687","unused","unused",()=>driver);
        var result=provider.GetHolonReplicationOutbox();Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_OUTBOX_SOURCE_NOT_ACTIVATED",result.ErrorCode);Assert.AreEqual(0,probe.Calls);
    }

}
