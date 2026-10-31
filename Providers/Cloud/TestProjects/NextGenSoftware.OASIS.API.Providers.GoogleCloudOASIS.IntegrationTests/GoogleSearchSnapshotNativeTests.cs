using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)][DataRow(4)][DataRow(5)]
    public async Task NativeSearchRetainsCapturedScopeDuringFirestoreRead(int scenario)
    {
        var work=Admit();var db=Database();var verifier=Database();var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternateParent=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=owner,ParentHolonId=parent,MetaData=new(){{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),Name="pine",Description="oak",CreatedByAvatarId=owner,ParentHolonId=alternateParent,MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=other,ParentHolonId=parent,MetaData=new(){{"zone","north"}}}};
        var journal=Path.Combine(work,$"history-native-search-snapshot-{scenario}.pending.json");Assert.IsFalse(File.Exists(journal));
        var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true};typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,db);
        try{
            foreach(var row in rows)Assert.IsFalse((await verifier.Collection("holons").Document(row.Id.ToString()).GetSnapshotAsync()).Exists);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id),owner,other,parent,alternateParent,scenario,state="before-owned-firestore-search-snapshot"}));file.Flush(true);}
            foreach(var row in rows){var saved=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
            var before=new Dictionary<Guid,DocumentSnapshot>();foreach(var row in rows)before[row.Id]=await verifier.Collection("holons").Document(row.Id.ToString()).GetSnapshotAsync();
            var task=provider.SearchAsync(query,loadChildren:false);var suspended=!task.IsCompleted;
            switch(scenario){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternateParent;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternateParent;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}
            var result=await task;
            foreach(var row in rows){var reference=verifier.Collection("holons").Document(row.Id.ToString());var actual=await reference.GetSnapshotAsync();Assert.AreEqual(before[row.Id].UpdateTime,actual.UpdateTime);Assert.AreEqual(before[row.Id].GetValue<string>("data_json"),actual.GetValue<string>("data_json"));await reference.DeleteAsync(Precondition.LastUpdated(actual.UpdateTime!.Value));Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);}
            File.Delete(journal);
            Assert.IsTrue(suspended,"Native search must be observed incomplete before the caller edit; do not retry a completed schedule.");Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
