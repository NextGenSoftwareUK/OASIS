using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeExportRootVersionIsIndependentOfAvatarHistory(bool sync,bool email)
    {
        var work=Admit();var owner=Guid.NewGuid();var own=Guid.NewGuid();var foreign=Guid.NewGuid();var name="owned-export-"+owner.ToString("N");
        var journal=Path.Combine(work,$"export-independent-{sync}-{email}.pending.json");
        var db=Database();var verifier=Database();
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,db);
        var references=new[]{verifier.Collection("avatars").Document(owner.ToString()),verifier.Collection("holons").Document(own.ToString()),verifier.Collection("holons").Document(foreign.ToString())};
        try{
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{owner,own,foreign,name,sync,email,state="before-owned-independent-export"}));file.Flush(true);
            }
            var avatar=new Avatar{Id=owner,Username=name,Email=name+"@example.invalid",IsActive=true};
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);avatar.Username=name+"-current";avatar.Email=avatar.Username+"@example.invalid";
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);Assert.AreEqual(2,avatar.Version);
            var holon=new Holon{Id=own,CreatedByAvatarId=owner,IsActive=true};var other=new Holon{Id=foreign,CreatedByAvatarId=Guid.NewGuid(),IsActive=true};
            for(var version=1;version<=3;version++){
                holon.Name=name+"-v"+version;other.Name=name+"-foreign-v"+version;
                var saved=await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
                Assert.IsFalse((await provider.SaveHolonAsync(other,saveChildren:false)).IsError);
            }
            var originals=new List<DocumentSnapshot>();foreach(var reference in references)originals.Add(await reference.GetSnapshotAsync());
            foreach(var version in new[]{0,1,2,3}){
                var exported=email
                    ?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,version):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,version)
                    :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,version):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,version);
                Assert.IsFalse(exported.IsError,exported.Exception?.ToString());Assert.IsTrue(exported.IsLoaded);Assert.AreEqual(1,exported.LoadedCount);
                var expected=version==0?3:version;Assert.AreEqual(own,exported.Result.Single().Id);Assert.AreEqual(expected,exported.Result.Single().Version);
                Assert.AreEqual(name+"-v"+expected,exported.Result.Single().Name);
            }
            var allVersions=email
                ?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,-1):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,-1)
                :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,-1):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,-1);
            Assert.IsFalse(allVersions.IsError,allVersions.Exception?.ToString());Assert.IsTrue(allVersions.IsLoaded);Assert.AreEqual(3,allVersions.LoadedCount);
            var versions=allVersions.Result.ToArray();CollectionAssert.AreEqual(new[]{1,2,3},versions.Select(row=>row.Version).ToArray());
            Assert.IsTrue(versions.All(row=>row.Id==own&&row.CreatedByAvatarId==owner));
            for(var index=0;index<versions.Length;index++)Assert.AreEqual(name+"-v"+(index+1),versions[index].Name);
            var oldAvatar=email?await provider.LoadAvatarByEmailAsync(name+"@example.invalid",1):await provider.LoadAvatarByUsernameAsync(name,1);
            Assert.IsFalse(oldAvatar.IsError);Assert.AreEqual(owner,oldAvatar.Result.Id);Assert.AreEqual(1,oldAvatar.Result.Version);
            var oldExport=email?await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1):await provider.ExportAllDataForAvatarByUsernameAsync(name,1);
            Assert.IsTrue(oldExport.IsError);Assert.IsNull(oldExport.Result);
            var cleanup=verifier.StartBatch();
            for(var index=0;index<references.Length;index++){
                var current=await references[index].GetSnapshotAsync();Assert.AreEqual(originals[index].UpdateTime,current.UpdateTime);
                Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(originals[index].ToDictionary()),JToken.FromObject(current.ToDictionary())));
                cleanup.Delete(references[index],Precondition.LastUpdated(originals[index].UpdateTime!.Value));
            }
            var removed=await cleanup.CommitAsync();Assert.AreEqual(3,removed.Count);
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
