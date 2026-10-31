using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativePartialReadCannotReplaceMembershipAndCompleteSaveKeepsObservation(bool synchronous)
    {
        var work=Admit();var database=Database();var verifier=Database();
        var root=new Holon{Id=Guid.NewGuid(),Name="owned partial graph root",IsActive=true};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned partial graph child",IsActive=true};root.Children.Add(child);
        var ids=new[]{root.Id,child.Id};var references=ids.Select(id=>verifier.Collection("holons").Document(id.ToString())).ToArray();
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,database);
        var journal=Path.Combine(work,$"history-native-partial-graph-{synchronous}.pending.json");
        var originals=new List<DocumentSnapshot>();var verified=false;
        try
        {
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            using(var stream=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
            {await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,collection="holons",state="before-owned-partial-graph"}));stream.Flush(true);}
            var saved=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
            Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.AreEqual(2,saved.SavedCount);
            Assert.AreEqual(2,HolonGraphWritePlan.CreateForSave(new[]{root},true,true,0,0).Nodes.Count,
                "The complete native acknowledgement must retain next-save membership observation.");
            foreach(var reference in references){var snapshot=await reference.GetSnapshotAsync();Assert.IsTrue(snapshot.Exists);originals.Add(snapshot);}
            var read=synchronous?provider.LoadHolon(root.Id,loadChildren:false):await provider.LoadHolonAsync(root.Id,loadChildren:false);
            Assert.IsFalse(read.IsError,read.Exception?.ToString());var partial=(Holon)read.Result;
            Assert.AreEqual(0,partial.Children.Count);
            var manifest=HolonChildRoleManifestJson.Deserialize(partial.ChildRoleManifestJson);
            CollectionAssert.AreEquivalent(new[]{child.Id},manifest.Roles.SelectMany(role=>role.ChildIds).ToArray());
            partial.AcceptChanges();
            var rejected=synchronous?provider.SaveHolon(partial):await provider.SaveHolonAsync(partial);
            Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<InvalidOperationException>(rejected.Exception);
            Assert.IsFalse(rejected.IsSaved);Assert.AreEqual(0,rejected.SavedCount);
            foreach(var original in originals)
            {
                var current=await original.Reference.GetSnapshotAsync();Assert.AreEqual(original.UpdateTime,current.UpdateTime);
                Assert.AreEqual(original.GetValue<string>("data_json"),current.GetValue<string>("data_json"));
            }
            verified=true;
        }
        finally
        {
            if(verified)
            {
                foreach(var original in originals)
                {
                    var current=await original.Reference.GetSnapshotAsync();Assert.AreEqual(original.UpdateTime,current.UpdateTime);
                    Assert.AreEqual(original.GetValue<string>("data_json"),current.GetValue<string>("data_json"));
                    var deleted=await original.Reference.DeleteAsync(Precondition.LastUpdated(original.UpdateTime!.Value));Assert.AreNotEqual(default(Timestamp),deleted.UpdateTime);
                }
                foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
                File.Delete(journal);
            }
            await provider.DeActivateProviderAsync();
        }
    }
}
