using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using INode=Neo4j.Driver.INode;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class Neo4jDeleteOwnershipTests
{
    public class Probe:DispatchProxy
    {
        public object State=null!;public string Surface="";
        public sealed class Data
        {
            public string Mode="";public int Commits,Rollbacks;public INode Profile=null!,Detail=null!,Holon=null!;
            public readonly IOException Failure=new("owned delete acknowledgement failure");
        }
        public static T Make<T>(Data data,string surface) where T:class
        {var result=Create<T,Probe>();var p=(Probe)(object)result;p.State=data;p.Surface=surface;return result;}
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            var state=(Data)State;
            switch(method!.Name)
            {
                case "AsyncSession":return Make<IAsyncSession>(state,"session");
                case "BeginTransactionAsync":return Task.FromResult(Make<IAsyncTransaction>(state,"transaction"));
                case "Dispose":if(state.Mode=="cleanup")throw state.Failure;return null;
                case "CommitAsync":state.Commits++;return state.Mode=="commitLost"?Task.FromException(state.Failure):Task.CompletedTask;
                case "RollbackAsync":state.Rollbacks++;return state.Mode=="rollbackLost"?Task.FromException(state.Failure):Task.CompletedTask;
                case "RunAsync":
                    var query=(string)args![0]!;var cursor=Create<IResultCursor,Cursor>();var c=(Cursor)cursor;
                    if(query.Contains("RETURN 1 AS affected",StringComparison.Ordinal))
                    {
                        if(state.Mode is "rollback" or "rollbackLost")return Task.FromException<IResultCursor>(state.Failure);
                        c.Value=1;c.Key="affected";
                    }
                    else if(query.Contains("OasisRecordVersion",StringComparison.Ordinal)||state.Mode=="missing")c.Empty=true;
                    else {c.Key=query.Contains("RETURN h",StringComparison.Ordinal)?"h":"a";c.Value=c.Key=="h"?state.Holon:query.Contains("AvatarDetail",StringComparison.Ordinal)?state.Detail:state.Profile;}
                    return Task.FromResult<IResultCursor>(cursor);
                default:throw new InvalidOperationException("Unexpected delete call: "+method.Name);
            }
        }
    }
    public class Cursor:DispatchProxy
    {
        public bool Empty;public string Key="";public object Value=null!;private int reads;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="FetchAsync")return Task.FromResult(!Empty&&reads++==0);
            if(method.Name=="ConsumeAsync")return Task.FromResult<IResultSummary>(null!);
            if(method.Name=="get_Current"){var r=Create<IRecord,Neo4jPreparationTransactionTests.RecordProbe>();var p=(Neo4jPreparationTransactionTests.RecordProbe)r;p.Key=Key;p.Value=Value;return r;}
            throw new InvalidOperationException("Unexpected delete cursor call.");
        }
    }
    public static IEnumerable<object[]> Cases()
    {
        foreach(var kind in new[]{"soft","hard","holon"})foreach(var sync in new[]{false,true})foreach(var mode in new[]{"normal","rollback","rollbackLost","commitLost","cleanup","missing"})yield return new object[]{kind,sync,mode};
        foreach(var sync in new[]{false,true})yield return new object[]{"soft",sync,"repeat"};
    }
    [DataTestMethod,DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task DeletePublishesOnlyKnownOutcomesAndOwnsRollback(string kind,bool synchronous,string mode)
    {
        var id=Guid.NewGuid();var profile=new Avatar{Id=id,Username="owned-delete",Email="owned-delete@example.invalid"};
        var detail=new AvatarDetail{Id=id,Username=profile.Username,Email=profile.Email};var holon=new Holon{Id=id,Name="owned-delete",Description="owned"};
        if(mode=="repeat"){profile.IsActive=false;detail.IsActive=false;profile.DeletedDate=detail.DeletedDate=new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc);}
        INode Node(object value,Dictionary<string,object> lookup){var n=DispatchProxy.Create<INode,Neo4jNativeIdentityTests.NodeProbe>();lookup["Id"]=id.ToString();lookup["SnapshotJson"]=OasisJson.Serialize(value);((Neo4jNativeIdentityTests.NodeProbe)n).Values=lookup;return n;}
        var state=new Probe.Data{Mode=mode,Profile=Node(profile,new(){{"Username",profile.Username},{"Email",profile.Email}}),Detail=Node(detail,new(){{"Username",detail.Username},{"Email",detail.Email}}),Holon=Node(holon,new(){{"Name",holon.Name},{"Description",holon.Description},{"HolonType",holon.HolonType.ToString()}})};
        var provider=new Provider("bolt://owned.invalid","owned","owned"){IsProviderActivated=true};
        typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,Probe.Make<IDriver>(state,"driver"));
        bool error,saved,deleted,warning,changed;int count;string code;Exception? exception;
        if(kind=="holon")
        {var r=synchronous?provider.DeleteHolon(id):await provider.DeleteHolonAsync(id);error=r.IsError;saved=r.IsSaved;deleted=r.IsDeleted;warning=r.IsWarning;changed=r.HasAnyHolonsChanged;count=r.DeletedCount;code=r.ErrorCode;exception=r.Exception;}
        else
        {var r=synchronous?provider.DeleteAvatar(id,kind=="soft"):await provider.DeleteAvatarAsync(id,kind=="soft");error=r.IsError;saved=r.IsSaved;deleted=r.IsDeleted;warning=r.IsWarning;changed=r.HasAnyHolonsChanged;count=r.DeletedCount;code=r.ErrorCode;exception=r.Exception;}
        if(mode=="missing"){Assert.IsTrue(error);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,code);Assert.IsFalse(deleted);Assert.AreEqual(0,count);Assert.AreEqual(0,state.Commits);return;}
        if(mode=="repeat"){Assert.IsFalse(error);Assert.IsTrue(deleted);Assert.IsFalse(saved);Assert.IsFalse(changed);Assert.AreEqual(0,count);Assert.AreEqual(0,state.Commits);Assert.AreEqual(1,state.Rollbacks);return;}
        if(mode is "normal" or "cleanup")
        {Assert.IsFalse(error,exception?.ToString());Assert.IsTrue(saved);Assert.IsTrue(deleted);Assert.IsTrue(changed);Assert.AreEqual(1,count);Assert.AreEqual(mode=="cleanup",warning);Assert.AreEqual(1,state.Commits);}
        else
        {
            Assert.IsTrue(error);Assert.IsFalse(saved);Assert.IsFalse(deleted);Assert.IsFalse(changed);Assert.AreEqual(0,count);
            Assert.AreEqual(mode!="rollback",NativeMutationReconciliation.IsRequired(exception));
            if(mode!="rollback")Assert.AreEqual("COSMIC_WRITE_RECONCILIATION_REQUIRED",code);
            Assert.AreEqual(mode=="commitLost"?1:0,state.Commits);Assert.AreEqual(mode=="commitLost"?0:1,state.Rollbacks);
        }
    }
}
