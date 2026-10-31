using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GooglePartialGraphAdmissionTests
{
    // Controlled native-writer boundary, not an emulator or live SDK claim.
    private sealed class Writer:GoogleCloudOASIS
    {
        public int Calls;public bool Reject=true;public bool FailChild;
        public Writer():base("owned-admission",enableStorage:false,enableFirestore:false,enableBigQuery:false){}
        protected override Task<OASISResult<IHolon>> SavePlannedHolonAsync(PlannedHolonWrite write)
        {
            Calls++;
            if(Reject||FailChild&&write.Snapshot.ParentHolonId!=Guid.Empty)
                return Task.FromResult(new OASISResult<IHolon>{IsError=true,Exception=new InvalidOperationException("Synthetic native-writer rejection.")});
            write.Snapshot.Version=1;write.Snapshot.VersionId=Guid.NewGuid();
            write.Snapshot.ChildRoleManifestJson=HolonChildRolePersistence.Resolve(write,HolonChildRoleWriteKind.SourceSave,false,null);
            return Task.FromResult(new OASISResult<IHolon>{IsSaved=true,SavedCount=1,Result=write.Snapshot});
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
        var provider=new Writer();
        var result=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<InvalidOperationException>(result.Exception);
        Assert.AreEqual(0,provider.Calls,"Incomplete caller membership must reject before native writer dispatch.");
        Assert.AreEqual(0,root.Version);Assert.IsFalse(result.IsSaved);
        var scalar=synchronous?provider.SaveHolons(new[]{root},saveChildren:false):await provider.SaveHolonsAsync(new[]{root},saveChildren:false);
        Assert.IsTrue(scalar.IsError);Assert.AreEqual(1,provider.Calls,"Root-only admission reaches the deliberately rejecting writer.");
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task OnlyCompleteAcknowledgementPublishesSelectedObservation(bool synchronous,bool failChild)
    {
        var root=new Holon{Id=Guid.NewGuid()};root.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id});
        var provider=new Writer{Reject=false,FailChild=failChild};
        var result=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
        Assert.AreEqual(failChild,result.IsError,result.Exception?.ToString());Assert.AreEqual(!failChild,result.IsSaved);
        Assert.IsNotNull(root.ChildRoleManifestJson);Assert.AreEqual(2,provider.Calls);
        if(failChild)Assert.ThrowsException<InvalidOperationException>(()=>HolonGraphWritePlan.CreateForSave(new[]{root},true,true,0,0));
        else Assert.AreEqual(2,HolonGraphWritePlan.CreateForSave(new[]{root},true,true,0,0).Nodes.Count);
    }
}
