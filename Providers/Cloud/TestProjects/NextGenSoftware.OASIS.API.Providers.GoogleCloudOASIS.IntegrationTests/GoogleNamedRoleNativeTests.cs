using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    private sealed class NamedOwner:Holon
    {
        public List<Holon> Components {get;set;}=new();
        public override ReadOnlyCollection<IHolon> AllChildren=>Children.Concat<IHolon>(Components).ToList().AsReadOnly();
        public override IReadOnlyList<HolonChildCollection> CaptureChildCollections()=>base.CaptureChildCollections()
            .Concat(new[]{new HolonChildCollection(nameof(Components),Components,rows=>Components=rows.Cast<Holon>().ToList())}).ToArray();
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeNamedRolesPreserveObservedMembershipAndHistory(bool sync)
    {
        var work=Admit();var db=Database();var verifier=Database();
        var root=new NamedOwner{Id=Guid.NewGuid(),Name="owned named owner"};var child=new Holon{Id=Guid.NewGuid(),Name="owned named child"};root.Components.Add(child);
        var expected=HolonChildRoleManifestJson.Serialize(HolonChildCollectionState.Capture(root).CreateManifest(root.Id,row=>row.Id));
        var journal=Path.Combine(work,$"history-native-named-role-{sync}.pending.json");Assert.IsFalse(File.Exists(journal));
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,db);
        try{
            foreach(var id in new[]{root.Id,child.Id})Assert.IsFalse((await verifier.Collection("holons").Document(id.ToString()).GetSnapshotAsync()).Exists);
            using(var stream=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=new[]{root.Id,child.Id},collection="holons",project="oasis-google-native",state="before-owned-named-role-graph"}));stream.Flush(true);
            }
            var saved=sync?provider.SaveHolon(root):await provider.SaveHolonAsync(root);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            var reference=verifier.Collection("holons").Document(root.Id.ToString());var childReference=verifier.Collection("holons").Document(child.Id.ToString());
            var initial=await reference.GetSnapshotAsync();var childBefore=await childReference.GetSnapshotAsync();
            var initialManifest=OasisJson.DeserializeStored<Holon>(initial.GetValue<string>("data_json"))!.ChildRoleManifestJson;
            var scalar=new Holon{Id=root.Id,Name="owned scalar update"};var updated=sync?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(updated.IsError,updated.Exception?.ToString());
            var current=await reference.GetSnapshotAsync();var preserved=OasisJson.DeserializeStored<Holon>(current.GetValue<string>("data_json"))!.ChildRoleManifestJson;
            var history=await provider.LoadHolonAsync(root.Id,loadChildren:false,version:1);Assert.IsFalse(history.IsError,history.Message);
            var legacy=JObject.Parse(current.GetValue<string>("data_json"));legacy[nameof(Holon.ChildRoleManifestJson)]=JValue.CreateNull();
            await reference.UpdateAsync(new Dictionary<string,object>{{"data_json",legacy.ToString(Newtonsoft.Json.Formatting.None)}},Precondition.LastUpdated(current.UpdateTime!.Value));
            scalar.Name="owned legacy update";scalar.ChildRoleManifestJson=expected;
            var legacySaved=sync?provider.SaveHolon(scalar,saveChildren:false):await provider.SaveHolonAsync(scalar,saveChildren:false);Assert.IsFalse(legacySaved.IsError,legacySaved.Exception?.ToString());
            current=await reference.GetSnapshotAsync();var legacyAfter=OasisJson.DeserializeStored<Holon>(current.GetValue<string>("data_json"))!;
            var childAfter=await childReference.GetSnapshotAsync();Assert.AreEqual(childBefore.GetValue<string>("data_json"),childAfter.GetValue<string>("data_json"));Assert.AreEqual(childBefore.UpdateTime,childAfter.UpdateTime);
            await reference.DeleteAsync(Precondition.LastUpdated(current.UpdateTime!.Value));await childReference.DeleteAsync(Precondition.LastUpdated(childAfter.UpdateTime!.Value));
            Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);Assert.IsFalse((await childReference.GetSnapshotAsync()).Exists);File.Delete(journal);
            Assert.AreEqual(expected,initialManifest);Assert.AreEqual(expected,preserved);Assert.AreEqual(expected,((Holon)history.Result).ChildRoleManifestJson);Assert.IsNull(legacyAfter.ChildRoleManifestJson);Assert.IsNull(scalar.ChildRoleManifestJson);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
