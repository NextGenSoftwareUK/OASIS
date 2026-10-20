using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

// Actual SDK writes are delegated. Only the returned SnapshotJson is changed, not native state.
public class Neo4jPayloadFaultProxy:DispatchProxy
{
    public object Target=null!;
    public static T Wrap<T>(T target) where T:class
    {var proxy=Create<T,Neo4jPayloadFaultProxy>();((Neo4jPayloadFaultProxy)(object)proxy).Target=target;return proxy;}
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        object? result;
        try{result=method!.Invoke(Target,args);}
        catch(TargetInvocationException cause) when(cause.InnerException!=null){ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;}
        if(Target is INode && method!.Name=="get_Properties")
        {
            var fields=new Dictionary<string,object>((IReadOnlyDictionary<string,object>)result!);
            if(fields.TryGetValue("SnapshotJson",out var payload))
            {
                var json=JObject.Parse((string)payload);var metadata=json["MetaData"] as JObject ?? new JObject();
                metadata["controlled-ack-only"]=true;json["MetaData"]=metadata;fields["SnapshotJson"]=json.ToString();
            }
            return fields;
        }
        if(result is IAsyncSession session)return Wrap(session);
        if(result is Task<IAsyncTransaction> transaction)return WrapTransaction(transaction);
        if(result is Task<IResultCursor> cursor)return WrapCursor(cursor);
        if(result is IRecord record)return Wrap(record);
        if(result is INode node)return Wrap(node);
        return result;
    }
    private static async Task<IAsyncTransaction> WrapTransaction(Task<IAsyncTransaction> value)=>Wrap(await value);
    private static async Task<IResultCursor> WrapCursor(Task<IResultCursor> value)=>Wrap(await value);
}

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task NativeSaveRejectsChangedSameIdentityAcknowledgement(int kind,bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var field=typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!;var healthy=(IDriver)field.GetValue(provider)!;
        var id=Guid.NewGuid();
        try
        {
            field.SetValue(provider,Neo4jPayloadFaultProxy.Wrap(healthy));
            bool failed,saved;Exception? cause;
            if(kind==0){var r=synchronous?provider.SaveAvatar(new Avatar{Id=id,Username="owned-payload-ack"}):await provider.SaveAvatarAsync(new Avatar{Id=id,Username="owned-payload-ack"});failed=r.IsError;saved=r.IsSaved;cause=r.Exception;}
            else if(kind==1){var r=synchronous?provider.SaveAvatarDetail(new AvatarDetail{Id=id,XP=456}):await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,XP=456});failed=r.IsError;saved=r.IsSaved;cause=r.Exception;}
            else{var r=synchronous?provider.SaveHolon(new Holon{Id=id,Name="owned-payload-ack"},saveChildren:false):await provider.SaveHolonAsync(new Holon{Id=id,Name="owned-payload-ack"},saveChildren:false);failed=r.IsError;saved=r.IsSaved;cause=r.Exception;}
            field.SetValue(provider,healthy);using var check=healthy.AsyncSession();
            var rows=await(await check.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail OR n:Holon RETURN n.SnapshotJson AS json",new{id=id.ToString()})).ToListAsync();
            if(rows.Count>0)
            {
                Assert.AreEqual(1,rows.Count);Assert.IsFalse(rows.Single()["json"].As<string>().Contains("controlled-ack-only",StringComparison.Ordinal));
                await(await check.RunAsync("MATCH (n {Id:$id}) WHERE n:Avatar OR n:AvatarDetail OR n:Holon DETACH DELETE n",new{id=id.ToString()})).ConsumeAsync();
            }
            Assert.IsTrue(failed);Assert.IsFalse(saved);
            Assert.AreEqual(kind!=2,NativeMutationReconciliation.IsRequired(cause),
                "Holon writes explicitly await pre-commit rollback; avatar-specific owners remain a separate certification gate.");
            Assert.IsInstanceOfType<InvalidDataException>(kind==2 ? cause:cause!.InnerException);
            Assert.AreEqual(0,rows.Count,"Mismatch is detected before commit; native transaction must roll back.");
        }
        finally{field.SetValue(provider,healthy);await provider.DeActivateProviderAsync();}
    }
}
