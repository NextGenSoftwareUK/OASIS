using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
    [DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    public async Task NativeScopedGraphsExcludeForeignSubtreesAndCannotReplaceStoredMembership(int operation,bool sync)
    {
        var work=Admit();var db=Database();var verifier=Database();var owner=Guid.NewGuid();var name="owned-scoped-graph-"+owner.ToString("N");
        var root=new Holon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=owner,Version=1,IsActive=true};
        var allowed=new Holon{Id=Guid.NewGuid(),Name=name+"-allowed",CreatedByAvatarId=owner,Version=7,IsActive=true};
        var foreign=new Holon{Id=Guid.NewGuid(),Name="foreign scoped graph",CreatedByAvatarId=Guid.NewGuid(),Version=3,IsActive=true};
        var buried=new Holon{Id=Guid.NewGuid(),Name="owned behind foreign",CreatedByAvatarId=owner,Version=9,IsActive=true};
        root.Children.Add(allowed);root.Children.Add(foreign);foreign.Children.Add(buried);
        var ids=new[]{owner,root.Id,allowed.Id,foreign.Id,buried.Id};
        var references=ids.Select((id,index)=>verifier.Collection(index==0?"avatars":"holons").Document(id.ToString())).ToArray();
        var journal=Path.Combine(work,$"scoped-graph-{operation}-{sync}.pending.json");
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,db);
        try{
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,name,operation,sync,state="before-owned-scoped-graph"}));file.Flush(true);}
            var avatar=new Avatar{Id=owner,Username=name,Email=name+"@example.invalid",IsActive=true};
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
            root.Description="second root version";Assert.IsFalse((await provider.SaveHolonAsync(root,saveChildren:false)).IsError);Assert.AreEqual(2,root.Version);
            var originals=new List<DocumentSnapshot>();foreach(var reference in references)originals.Add(await reference.GetSnapshotAsync());
            foreach(var version in new[]{0,1}){
                IHolon projected;
                if(operation==3){
                    var search=new SearchParams{AvatarId=owner,SearchOnlyForCurrentAvatar=true,SearchGroups=new(){new SearchTextGroup{SearchHolons=true,SearchQuery=name,HolonSearchParams=new SearchHolonParams{Name=true}}}};
                    var found=sync?provider.Search(search,version:version):await provider.SearchAsync(search,version:version);
                    Assert.IsFalse(found.IsError,found.Exception?.ToString());projected=found.Result.SearchResultHolons.Single(row=>row.Id==root.Id);
                }else{
                    var found=operation switch{
                        0=>sync?provider.ExportAllDataForAvatarById(owner,version):await provider.ExportAllDataForAvatarByIdAsync(owner,version),
                        1=>sync?provider.ExportAllDataForAvatarByUsername(name,version):await provider.ExportAllDataForAvatarByUsernameAsync(name,version),
                        _=>sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,version):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,version)};
                    Assert.IsFalse(found.IsError,found.Exception?.ToString());projected=found.Result.Single(row=>row.Id==root.Id);
                }
                Assert.AreEqual(version==0?2:1,projected.Version);
                CollectionAssert.AreEquivalent(new[]{allowed.Id},projected.Children.Select(row=>row.Id).ToArray());Assert.AreEqual(7,projected.Children.Single().Version);
                ((Holon)projected).AcceptChanges();
                var rejected=sync?provider.SaveHolon(projected):await provider.SaveHolonAsync(projected);
                Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType<InvalidOperationException>(rejected.Exception);Assert.IsFalse(rejected.IsSaved);Assert.AreEqual(0,rejected.SavedCount);
            }
            var unscoped=await provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true);Assert.IsFalse(unscoped.IsError,unscoped.Exception?.ToString());
            CollectionAssert.AreEquivalent(new[]{allowed.Id,foreign.Id},unscoped.Result.Children.Select(row=>row.Id).ToArray());
            Assert.AreEqual(buried.Id,unscoped.Result.Children.Single(row=>row.Id==foreign.Id).Children.Single().Id);
            var cleanup=verifier.StartBatch();
            foreach(var original in originals){
                var current=await original.Reference.GetSnapshotAsync();Assert.AreEqual(original.UpdateTime,current.UpdateTime);
                Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(original.ToDictionary()),JToken.FromObject(current.ToDictionary())));
                cleanup.Delete(original.Reference,Precondition.LastUpdated(original.UpdateTime!.Value));
            }
            Assert.AreEqual(references.Length,(await cleanup.CommitAsync()).Count);
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
