using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public sealed class CosmosPartialGraphAdmissionTests
{
    private sealed class Writer:AzureCosmosDBOASIS
    {
        public int Calls;public bool Reject=true;public bool FailChild;public string Mode="success";
        public Writer():base(new Uri("https://localhost:8081"),"unused","owned-admission",new()){}
        protected override Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write)
        {
            Calls++;
            if(Reject||FailChild&&write.Snapshot.ParentHolonId!=Guid.Empty)
                return Task.FromResult(new OASISResult<IHolon>{IsError=true,Exception=new InvalidOperationException("Synthetic native-writer rejection.")});
            if(Mode=="null")return Task.FromResult<OASISResult<IHolon>>(null!);
            if(Mode=="missing")return Task.FromResult(new OASISResult<IHolon>{IsSaved=true});
            write.Snapshot.Version=Mode=="unack"?17:1;write.Snapshot.VersionId=Guid.NewGuid();
            write.Snapshot.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(write,HolonChildRoleWriteKind.SourceSave,false,null);
            return Task.FromResult(new OASISResult<IHolon>{IsSaved=Mode!="unack",Result=write.Snapshot});
        }
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task KnownPartialSelectionRejectsBeforeNativeWriter(bool persisted,bool acceptChanges,bool synchronous)
    {
        var root=new Holon{Id=Guid.NewGuid()};
        if(persisted)root.ChildRoleManifestJson=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,root.Id,
            new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}));
        HolonChangeTracking.MarkIncompleteChildSelection(root);if(acceptChanges)root.AcceptChanges();
        var provider=new Writer();var result=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<InvalidOperationException>(result.Exception);
        Assert.AreEqual(0,provider.Calls,"Incomplete membership must reject before native writer dispatch.");Assert.AreEqual(0,root.Version);
        var scalar=synchronous?provider.SaveHolons(new[]{root},saveChildren:false):await provider.SaveHolonsAsync(new[]{root},saveChildren:false);
        Assert.IsTrue(scalar.IsError);Assert.AreEqual(1,provider.Calls);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task OnlyCompleteAcknowledgementPublishesObservation(bool synchronous,bool failChild)
    {
        var root=new Holon{Id=Guid.NewGuid()};root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id});
        var provider=new Writer{Reject=false,FailChild=failChild};var result=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
        Assert.AreEqual(failChild,result.IsError,result.Exception?.ToString());Assert.AreEqual(!failChild,result.IsSaved);Assert.IsNotNull(root.ChildRoleManifestJson);
        if(failChild)Assert.ThrowsException<InvalidOperationException>(()=>HolonGraphWritePlan.CreateForSave(new[]{root},true,true,0,0));
        else Assert.AreEqual(2,HolonGraphWritePlan.CreateForSave(new[]{root},true,true,0,0).Nodes.Count);
    }

    [DataTestMethod]
    [DataRow(false,"null")][DataRow(true,"null")][DataRow(false,"missing")][DataRow(true,"missing")][DataRow(false,"unack")][DataRow(true,"unack")]
    public async Task MissingAcknowledgementStopsBeforeNextWriteOrCallerPublication(bool synchronous,string mode)
    {
        var rows=new[]{new Holon{Id=Guid.NewGuid()},new Holon{Id=Guid.NewGuid()}};var provider=new Writer{Reject=false,Mode=mode};
        var result=synchronous?provider.SaveHolons(rows,continueOnError:true):await provider.SaveHolonsAsync(rows,continueOnError:true);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception),"Malformed dispatched acknowledgements require original-outcome reconciliation, never replay.");
        Assert.AreEqual(1,provider.Calls);foreach(var row in rows){Assert.AreEqual(0,row.Version);Assert.IsNull(row.ChildRoleManifestJson);}
    }
}
