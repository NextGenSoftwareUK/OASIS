using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GooglePreparationNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativePreparationAndGraphShareAtomicOperationFence(bool preparationFirst)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var work=Environment.GetEnvironmentVariable("OASIS_FIRESTORE_HISTORY_WORK")!;
        Assert.IsTrue(File.Exists(Path.Combine(work,"history-durability-ready.json")));
        Assert.AreEqual(0,Directory.GetFiles(work,"*.pending.json").Length);
        var journal=Path.Combine(work,$"history-native-preparation-graph-{preparationFirst}.pending.json");
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();var id=Guid.NewGuid();
        var key="GoogleCloudOASIS:"+operation.ToString("N");
        var verifier=GoogleNativeFirestore.Database();
        var references=new[]{verifier.Collection("holons").Document(id.ToString()),
            verifier.Collection("cosmicWritePreparations").Document(key),
            verifier.Collection("cosmicWriteReceipts").Document(key),
            verifier.Collection("cosmicOperationFences").Document(key)};
        var preparationReady=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var graphReady=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releasePreparation=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseGraph=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Owner Provider(Func<Task> before) {
            var provider=new Owner{IsProviderActivated=true,BeforeCommit=before};
            typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!
                .SetValue(provider,GoogleNativeFirestore.Database());return provider;
        }
        var preparationProvider=Provider(()=>{preparationReady.TrySetResult();return releasePreparation.Task.WaitAsync(TimeSpan.FromSeconds(20));});
        var graphProvider=Provider(()=>{graphReady.TrySetResult();return releaseGraph.Task.WaitAsync(TimeSpan.FromSeconds(20));});
        var candidate=new HolonWritePreparation(operation,actor,ProviderType.GoogleCloudOASIS,
            new[]{new ProtectedHolonPreparation(id,"oasis-preparation1:owned-cross-path-opaque")});
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.GoogleCloudOASIS,
            new[]{new HolonWriteCondition(id,null)}),ConditionalHolonOperation.Save,
            new[]{new Holon{Id=id,Name="owned Firestore cross-path graph",IsActive=true}});
        try {
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{operation,actor,id,preparationFirst,state="before-cross-path-native-batches"}));file.Flush(true);
            }
            var preparationTask=preparationProvider.ClaimHolonWritePreparationAsync(candidate);
            var graphTask=graphProvider.ExecuteConditionalHolonWriteAsync(request);
            await Task.WhenAll(preparationReady.Task,graphReady.Task).WaitAsync(TimeSpan.FromSeconds(15));
            if(preparationFirst){releasePreparation.TrySetResult();Assert.IsFalse((await preparationTask).IsError);releaseGraph.TrySetResult();}
            else {releaseGraph.TrySetResult();Assert.IsFalse((await graphTask).IsError);releasePreparation.TrySetResult();}
            var preparation=await preparationTask;var graph=await graphTask;
            Assert.AreEqual(1,preparationProvider.Commits);Assert.AreEqual(1,graphProvider.Commits);
            if(preparationFirst) {
                Assert.IsFalse(preparation.IsError,preparation.Exception?.ToString());
                Assert.AreEqual("COSMIC_CONCURRENCY_CONFLICT",graph.ErrorCode);
                Assert.IsFalse(graph.Exception is INativeMutationReconciliationRequired);
                Assert.IsFalse((await references[0].GetSnapshotAsync()).Exists);
                Assert.IsFalse((await references[2].GetSnapshotAsync()).Exists);
                var replay=await preparationProvider.ClaimHolonWritePreparationAsync(candidate);
                Assert.IsFalse(replay.IsError);Assert.AreEqual(candidate.Records[0].Envelope,replay.Result.Records[0].Envelope);
            } else {
                Assert.IsFalse(graph.IsError,graph.Exception?.ToString());
                Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",preparation.ErrorCode);
                Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(preparation.Exception);
                Assert.IsFalse((await references[1].GetSnapshotAsync()).Exists);
                var receipt=await references[2].GetSnapshotAsync();
                Assert.AreEqual(id,ConditionalHolonReceipt.Read(receipt.GetValue<string>("resultJson")).Single().Id);
                var replay=await graphProvider.ExecuteConditionalHolonWriteAsync(request);
                Assert.IsFalse(replay.IsError);Assert.AreEqual(graph.Result.Single().VersionId,replay.Result.Single().VersionId);
            }
            Assert.AreEqual(1,preparationProvider.Commits);Assert.AreEqual(1,graphProvider.Commits);
            foreach(var reference in references) {
                var snapshot=await reference.GetSnapshotAsync();if(!snapshot.Exists)continue;
                Assert.IsTrue(snapshot.UpdateTime.HasValue);
                var ack=await reference.DeleteAsync(Precondition.LastUpdated(snapshot.UpdateTime.Value));
                Assert.AreNotEqual(default(Timestamp),ack.UpdateTime);Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            }
            File.Delete(journal);
        } finally {
            releasePreparation.TrySetResult();releaseGraph.TrySetResult();
            await preparationProvider.DeActivateProviderAsync();await graphProvider.DeActivateProviderAsync();
        }
    }
}
