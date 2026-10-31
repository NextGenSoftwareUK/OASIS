using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jConditionalReceiptCleanupTests
{
    public class SessionProbe:DispatchProxy
    {
        public IAsyncTransaction Transaction=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name switch {
            "BeginTransactionAsync"=>Task.FromResult(Transaction),"Dispose"=>null,
            _=>throw new InvalidOperationException("Unexpected session operation: "+method.Name)};
    }
    public class TransactionProbe:DispatchProxy
    {
        public IRecord Record=null!;public Exception Cleanup=null!;public int Queries,Rollbacks,Commits;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="Dispose")return null;
            if(method.Name=="RollbackAsync"){Rollbacks++;return Task.FromException(Cleanup);}
            if(method.Name=="CommitAsync"){Commits++;throw new InvalidOperationException("Receipt replay must not commit another mutation.");}
            if(method.Name=="RunAsync")
            {Queries++;var cursor=DispatchProxy.Create<IResultCursor,CursorProbe>();((CursorProbe)cursor).Record=Record;return Task.FromResult(cursor);}
            throw new InvalidOperationException("Unexpected transaction operation: "+method.Name);
        }
    }
    public class CursorProbe:DispatchProxy
    {
        public IRecord Record=null!;private int reads;
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name switch {
            "FetchAsync"=>Task.FromResult(reads++==0),"get_Current"=>Record,
            _=>throw new InvalidOperationException("Unexpected cursor operation: "+method.Name)};
    }
    public class RecordProbe:DispatchProxy
    {
        public Neo4j.Driver.INode Node=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)=>method!.Name=="get_Item" && (string)args![0]! == "r" ? Node
            :throw new InvalidOperationException("Unexpected record operation: "+method.Name);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task KnownReceiptSurvivesReadTransactionCleanupFailureWithoutAnotherAttempt(bool synchronous)
    {
        var source=new Holon {Id=Guid.NewGuid(),Name="known committed receipt"};
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),ProviderType.Neo4jOASIS,
            new[]{new HolonWriteCondition(source.Id,null)}),ConditionalHolonOperation.Save,new[]{source},saveChildren:false);
        var plan=ConditionalHolonPlan.Create(request,ProviderType.Neo4jOASIS);
        var committed=new Holon {Id=source.Id,Name=source.Name,Version=1,VersionId=Guid.NewGuid(),CreatedByAvatarId=request.Write.ActorId};
        CosmicConcurrency.SetProviderRevisionId(committed,ProviderType.Neo4jOASIS,Guid.NewGuid());
        var json=ConditionalHolonReceipt.Write(new[]{committed},plan);
        var node=DispatchProxy.Create<Neo4j.Driver.INode,Neo4jNativeIdentityTests.NodeProbe>();
        ((Neo4jNativeIdentityTests.NodeProbe)node).Values=new Dictionary<string,object>{{"ResultJson",json},{"Fingerprint",plan.Fingerprint}};
        var record=DispatchProxy.Create<IRecord,RecordProbe>();((RecordProbe)record).Node=node;
        var transaction=DispatchProxy.Create<IAsyncTransaction,TransactionProbe>();var probe=(TransactionProbe)transaction;
        probe.Record=record;probe.Cleanup=new IOException("owned read transaction cleanup failure");
        var session=DispatchProxy.Create<IAsyncSession,SessionProbe>();((SessionProbe)session).Transaction=transaction;
        var driver=DispatchProxy.Create<IDriver,Neo4jLifecycleTests.DriverProbe>();var driverProbe=(Neo4jLifecycleTests.DriverProbe)driver;
        var provider=new Provider("bolt://owned.invalid","owned","owned",()=>driver);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);driverProbe.SessionFactory=()=>session;
        var result=synchronous ? provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.IsTrue(result.IsWarning);
        Assert.AreEqual(1,result.SavedCount);Assert.AreEqual(0,result.DeletedCount);Assert.IsTrue(result.HasAnyHolonsChanged);
        Assert.IsFalse(result.Result.Single().IsNewHolon);Assert.AreSame(probe.Cleanup,result.Exception);Assert.AreEqual(committed.VersionId,result.Result.Single().VersionId);
        Assert.AreEqual(1,probe.Queries);Assert.AreEqual(1,probe.Rollbacks);Assert.AreEqual(0,probe.Commits);Assert.AreEqual(0,source.Version);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }
}
