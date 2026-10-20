using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleSaveCountTests
{
    private sealed class Controlled(string mode,Action first):GoogleCloudOASIS("owned-count",enableStorage:false,enableFirestore:false,enableBigQuery:false)
    {
        public int Calls;private Guid _first;
        protected override async Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write){
            Calls++;await Task.Yield();
            if(Calls==2){
                if(mode=="reject")return new(){IsError=true,Exception=new InvalidDataException("owned known rejection")};
                if(mode=="unknown")return new(){IsError=true,Exception=new GoogleFirestoreMutationReconciliationException("owned/document",new IOException("owned unknown acknowledgement"))};
                if(mode=="postack")return new(){IsError=true,SavedCount=73,Exception=new GoogleFirestorePartialMutationException(new[]{_first,write.Snapshot.Id},new IOException("owned acknowledged publication failure"))};
                if(mode=="unack")return new(){Result=write.Snapshot,IsSaved=false};
            }
            write.Snapshot.Version=1;write.Snapshot.VersionId=Guid.NewGuid();
            if(Calls==1){_first=write.Snapshot.Id;first();}
            return new(){Result=write.Snapshot,IsSaved=true,SavedCount=73};
        }
    }
    [DataTestMethod]
    [DataRow("success",false)][DataRow("reject",false)][DataRow("unknown",false)][DataRow("postack",false)][DataRow("unack",false)][DataRow("caller",false)]
    [DataRow("success",true)][DataRow("reject",true)][DataRow("unknown",true)][DataRow("postack",true)][DataRow("unack",true)][DataRow("caller",true)]
    public async Task GraphCountsOnlyDistinctKnownAcknowledgements(string mode,bool synchronous)
    {
        var rows=Enumerable.Range(0,3).Select(_=>new Holon{Id=Guid.NewGuid()}).ToArray();
        var provider=new Controlled(mode,()=>{if(mode=="caller")rows[0].Name="owned caller edit";});
        var result=synchronous?provider.SaveHolons(rows,continueOnError:mode is "unknown" or "postack"):await provider.SaveHolonsAsync(rows,continueOnError:mode is "unknown" or "postack");
        Assert.AreEqual(mode!="success",result.IsError);Assert.AreEqual(mode=="success",result.IsSaved);
        Assert.AreEqual(mode=="success"?3:mode=="postack"?2:1,result.SavedCount);
        Assert.AreEqual(mode=="success"?3:mode=="caller"?1:2,provider.Calls);
        if(mode=="unack")Assert.AreEqual(0,rows[1].Version,"An unacknowledged payload is not a successful save.");
        if(mode is "unknown" or "postack" or "caller")Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
    }
    [DataTestMethod]
    [DataRow("success",false)][DataRow("unknown",false)][DataRow("postack",false)]
    [DataRow("success",true)][DataRow("unknown",true)][DataRow("postack",true)]
    public async Task ImportRetainsGraphAcknowledgementCount(string mode,bool synchronous)
    {
        var provider=new Controlled(mode,()=>{});var rows=Enumerable.Range(0,3).Select(_=>new Holon{Id=Guid.NewGuid()}).ToArray();
        var result=synchronous?provider.Import(rows):await provider.ImportAsync(rows);
        Assert.AreEqual(mode=="success",result.Result);Assert.AreEqual(mode=="success",result.IsSaved);Assert.AreEqual(mode=="success"?3:mode=="postack"?2:1,result.SavedCount);
        if(mode!="success")Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void SaveErrorRetainsKnownAcknowledgementButNeverInventsAnUnknownOne(bool known)
    {
        var id=Guid.NewGuid();Exception cause=known?new GoogleFirestorePartialMutationException(new[]{id,id},new IOException("owned publication failure")):new GoogleFirestoreMutationReconciliationException("owned/document",new IOException("owned unknown acknowledgement"));
        var arguments=new object[]{new OASISResult<IHolon>(),"owned",cause};
        typeof(GoogleCloudOASIS).GetMethod("HandleFirestoreSaveError",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(IHolon)).Invoke(null,arguments);
        var result=(OASISResult<IHolon>)arguments[0];Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreSame(cause,result.Exception);Assert.AreEqual(known?1:0,result.SavedCount);
    }
}
