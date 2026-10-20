using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass][DoNotParallelize][TestCategory("BatchRevisionNative")]
public class GoogleBatchRevisionNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OrdinaryGraphBatchPublishesNativeRevisionsForSubsequentConditionalWrite(bool synchronous)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var work=Environment.GetEnvironmentVariable("OASIS_FIRESTORE_HISTORY_WORK")!;
        Assert.IsTrue(File.Exists(Path.Combine(work,"history-durability-ready.json")));
        Assert.AreEqual(0,Directory.GetFiles(work,"*.pending.json").Length);
        var journal=Path.Combine(work,$"history-native-batch-revision-{synchronous}.pending.json");
        var root=new Holon{Id=Guid.NewGuid(),Name="owned ordinary root",IsActive=true};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned ordinary child",IsActive=true};root.Children.Add(child);
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();var key="GoogleCloudOASIS:"+operation.ToString("N");
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!
            .SetValue(provider,GoogleNativeFirestore.Database());
        var verifier=GoogleNativeFirestore.Database();
        var references=new[]{verifier.Collection("holons").Document(root.Id.ToString()),verifier.Collection("holons").Document(child.Id.ToString()),
            verifier.Collection("cosmicWriteReceipts").Document(key),verifier.Collection("cosmicOperationFences").Document(key)};
        try {
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{root=root.Id,child=child.Id,operation,actor,synchronous,state="before-native-ordinary-batch"}));file.Flush(true);
            }
            var saved=synchronous?provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});
            Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
            var rows=new[]{root,child};var revisions=new Dictionary<Guid,Guid?>();
            foreach(var row in rows) {
                var revision=CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS);
                Assert.IsTrue(revision.HasValue&&revision!=Guid.Empty,"Original caller must receive its committed native token.");revisions.Add(row.Id,revision);
                var stored=await verifier.Collection("holons").Document(row.Id.ToString()).GetSnapshotAsync();
                var canonical=OasisJson.DeserializeStored<Holon>(stored.GetValue<string>("data_json"));
                Assert.AreEqual(revision,CosmicConcurrency.GetProviderRevisionId(canonical,ProviderType.GoogleCloudOASIS));
            }
            root.Name="owned conditional follow-on";
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.GoogleCloudOASIS,
                rows.Select(row=>new HolonWriteCondition(row.Id,revisions[row.Id]))),ConditionalHolonOperation.Save,new[]{root});
            var update=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(update.IsError,update.Exception?.ToString());
            foreach(var row in new[]{update.Result.Single(),update.Result.Single().Children.Single()})
                Assert.AreNotEqual(revisions[row.Id],CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS));
            foreach(var reference in references) {
                var snapshot=await reference.GetSnapshotAsync();Assert.IsTrue(snapshot.Exists);Assert.IsTrue(snapshot.UpdateTime.HasValue);
                var ack=await reference.DeleteAsync(Precondition.LastUpdated(snapshot.UpdateTime.Value));
                Assert.AreNotEqual(default(Timestamp),ack.UpdateTime);Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            }
            File.Delete(journal);
        } finally {await provider.DeActivateProviderAsync();}
    }
}
