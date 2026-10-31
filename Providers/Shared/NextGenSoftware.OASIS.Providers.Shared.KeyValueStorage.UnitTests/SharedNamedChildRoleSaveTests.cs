using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class SharedNamedChildRoleSaveTests
{
    private sealed class Owner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
    }

    private sealed class EditingBackend:IKeyValueBackend
    {
        public readonly InMemoryBackend Store=new();
        public Action ReadEdit,WriteEdit;
        public async Task<string> GetAsync(string key,System.Threading.CancellationToken token=default){var value=await Store.GetAsync(key,token);var edit=ReadEdit;ReadEdit=null;edit?.Invoke();return value;}
        public async Task PutAsync(string key,string value,System.Threading.CancellationToken token=default){await Store.PutAsync(key,value,token);var edit=WriteEdit;WriteEdit=null;edit?.Invoke();}
        public Task DeleteAsync(string key,System.Threading.CancellationToken token=default)=>Store.DeleteAsync(key,token);
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix,System.Threading.CancellationToken token=default)=>Store.ListKeysAsync(prefix,token);
        public Task VerifyAsync(System.Threading.CancellationToken token=default)=>Store.VerifyAsync(token);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task ChangedNamedMembershipStopsBeforeChildDispatchAndRetainsAcknowledgedEvidence(bool sync,bool afterWrite)
    {
        var backend=new EditingBackend();var provider=new TestProvider(backend);var root=new Owner{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid()};root.Components.Add(child);
        if(afterWrite)backend.WriteEdit=()=>root.Components.Clear();else backend.ReadEdit=()=>root.Components.Clear();
        var result=sync?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreEqual(0,root.Components.Count);
        Assert.AreEqual(afterWrite?1:0,backend.Store.PutCalls);Assert.AreEqual(Guid.Empty,child.ParentHolonId);Assert.IsFalse(backend.Store.Items.ContainsKey("oasis/holon/"+child.Id.ToString("N")));
        if(afterWrite){var partial=result.Exception as KeyValuePartialMutationException;Assert.IsNotNull(partial);CollectionAssert.AreEqual(new[]{"oasis/holon/"+root.Id.ToString("N")},partial.AcknowledgedKeys.ToArray());}
        else Assert.IsInstanceOfType<InvalidOperationException>(result.Exception);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task SelectedNamedRolesAndMembersAreSavedAndRootOnlyPreservesObservation(bool sync,bool batch)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend){IsVersionControlEnabled=true};
        var root=new Owner{Id=Guid.NewGuid(),Name="named owner"};var child=new Holon{Id=Guid.NewGuid(),Name="named child"};root.Components.Add(child);
        var expected=HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id,row=>row.Id));
        if(batch){var result=sync?provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});Assert.IsFalse(result.IsError,result.Message);}
        else{var result=sync?provider.SaveHolon(root):await provider.SaveHolonAsync(root);Assert.IsFalse(result.IsError,result.Message);}
        var key="oasis/holon/"+root.Id.ToString("N");var childKey="oasis/holon/"+child.Id.ToString("N");
        Assert.AreEqual(expected,StoredRecordJson.DeserializeStored<Holon>(backend.Items[key])!.ChildRoleManifestJson);
        Assert.IsTrue(backend.Items.ContainsKey(childKey));Assert.AreEqual(root.Id,child.ParentHolonId);Assert.AreEqual(expected,root.ChildRoleManifestJson);
        var childBefore=backend.Items[childKey];var scalar=new Holon{Id=root.Id,Name="scalar edit"};
        var saved=sync?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);
        Assert.AreEqual(expected,StoredRecordJson.DeserializeStored<Holon>(backend.Items[key])!.ChildRoleManifestJson);Assert.AreEqual(expected,scalar.ChildRoleManifestJson);
        Assert.AreEqual(childBefore,backend.Items[childKey]);
        var historical=await provider.LoadHolonAsync(root.Id,loadChildren:false,version:1);Assert.IsFalse(historical.IsError,historical.Message);
        Assert.AreEqual(expected,((Holon)historical.Result).ChildRoleManifestJson);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RootOnlySaveUsesObservedLegacyNullRatherThanStaleCallerRoles(bool sync)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);var id=Guid.NewGuid();
        var key="oasis/holon/"+id.ToString("N");var seeded=await provider.SaveHolonAsync(new Holon{Id=id,Name="legacy"},saveChildren:false);Assert.IsFalse(seeded.IsError,seeded.Message);
        Assert.IsNull(StoredRecordJson.DeserializeStored<Holon>(backend.Items[key])!.ChildRoleManifestJson);
        var stale=HolonChildRoleManifestJson.Serialize(new HolonChildRoleManifest(1,id,new[]{new HolonChildRole("Children",new[]{Guid.NewGuid()})}));
        var source=new Holon{Id=id,Name="updated",ChildRoleManifestJson=stale};
        var saved=sync?provider.SaveHolon(source,saveChildren:false):await provider.SaveHolonAsync(source,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);
        Assert.IsNull(StoredRecordJson.DeserializeStored<Holon>(backend.Items[key])!.ChildRoleManifestJson);Assert.IsNull(source.ChildRoleManifestJson);
    }
}
