using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    private sealed class CountFaultClient(Google.Cloud.Firestore.V1.FirestoreClient inner,string mode,Action afterFirst):Google.Cloud.Firestore.V1.FirestoreClient
    {
        public int Commits;
        public override Google.Cloud.Firestore.V1.FirestoreSettings Settings=>inner.Settings;
        public override BatchGetDocumentsStream BatchGetDocuments(Google.Cloud.Firestore.V1.BatchGetDocumentsRequest request,Google.Api.Gax.Grpc.CallSettings settings=null)=>inner.BatchGetDocuments(request,settings);
        public override async Task<Google.Cloud.Firestore.V1.CommitResponse> CommitAsync(Google.Cloud.Firestore.V1.CommitRequest request,Google.Api.Gax.Grpc.CallSettings settings=null){
            Commits++;Assert.AreEqual(1,request.Writes.Count);var committed=await inner.CommitAsync(request,settings);
            if(mode=="publication"&&Commits==1)afterFirst();
            if(mode=="unknown"&&Commits==2)throw new IOException("Owned second native commit acknowledgement withheld.");
            return committed;
        }
    }

    [DataTestMethod]
    [DataRow("unknown",false,false)][DataRow("unknown",true,false)][DataRow("unknown",false,true)][DataRow("unknown",true,true)]
    [DataRow("publication",false,false)][DataRow("publication",true,false)][DataRow("publication",false,true)][DataRow("publication",true,true)]
    public async Task NativeGraphAndImportCountKnownAcknowledgementsWithoutReplay(string mode,bool synchronous,bool import)
    {
        var work=Admit();var marker="owned-google-count-"+Guid.NewGuid().ToString("N");
        var rows=Enumerable.Range(0,3).Select(index=>new Holon{Id=Guid.NewGuid(),Name=marker+index,MetaData=new(){["owned_history_native"]=marker}}).ToArray();
        var originalNames=rows.ToDictionary(row=>row.Id,row=>row.Name);var db=Database();var verifier=Database();
        var fault=new CountFaultClient(db.Client,mode,()=>rows[0].Name="owned caller edit");
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,FirestoreDb.Create("oasis-google-native",fault));
        var references=rows.Select(row=>verifier.Collection("holons").Document(row.Id.ToString())).ToArray();
        var journal=Path.Combine(work,$"history-native-save-count-{mode}-{synchronous}-{import}.pending.json");var observed=new List<DocumentSnapshot>();var safe=false;
        try{
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id),marker,mode,import,synchronous,state="before-owned-native-count-fault"}));file.Flush(true);
            }
            Exception cause;int count;bool saved,failed;
            if(import){var result=synchronous?provider.Import(rows):await provider.ImportAsync(rows);cause=result.Exception;count=result.SavedCount;saved=result.IsSaved;failed=result.IsError;Assert.IsFalse(result.Result);}
            else{var result=synchronous?provider.SaveHolons(rows,continueOnError:true):await provider.SaveHolonsAsync(rows,continueOnError:true);cause=result.Exception;count=result.SavedCount;saved=result.IsSaved;failed=result.IsError;}
            // Read ORIGINAL records, never replay the mutation to determine its outcome.
            var actualCount=mode=="unknown"?2:1;
            for(var index=0;index<references.Length;index++){
                var actual=await references[index].GetSnapshotAsync();
                if(index>=actualCount){Assert.IsFalse(actual.Exists);continue;}
                Assert.IsTrue(actual.Exists);var canonical=OasisJson.DeserializeStored<Holon>(actual.GetValue<string>("data_json"));
                Assert.AreEqual(rows[index].Id,canonical.Id);Assert.AreEqual(originalNames[canonical.Id],canonical.Name);Assert.AreEqual(marker,canonical.MetaData["owned_history_native"]);Assert.AreEqual(1,canonical.Version);observed.Add(actual);
            }
            safe=true;
            Assert.IsTrue(failed);Assert.IsFalse(saved);Assert.AreEqual(1,count,"Only the first native write was acknowledged to this operation.");
            Assert.AreEqual(actualCount,fault.Commits);Assert.IsTrue(NativeMutationReconciliation.IsRequired(cause));
            Assert.AreEqual(mode=="unknown"?1:0,rows[0].Version);Assert.AreEqual(0,rows[1].Version);Assert.AreEqual(0,rows[2].Version);
            if(mode=="publication")Assert.AreEqual("owned caller edit",rows[0].Name);
        }finally{
            if(safe){
                foreach(var original in observed){
                    var current=await original.Reference.GetSnapshotAsync();Assert.AreEqual(original.UpdateTime,current.UpdateTime);Assert.AreEqual(original.GetValue<string>("data_json"),current.GetValue<string>("data_json"));
                    var removed=await original.Reference.DeleteAsync(Precondition.LastUpdated(original.UpdateTime!.Value));Assert.AreNotEqual(default(Timestamp),removed.UpdateTime);
                }
                foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);File.Delete(journal);
            }
            await provider.DeActivateProviderAsync();
        }
    }
}
