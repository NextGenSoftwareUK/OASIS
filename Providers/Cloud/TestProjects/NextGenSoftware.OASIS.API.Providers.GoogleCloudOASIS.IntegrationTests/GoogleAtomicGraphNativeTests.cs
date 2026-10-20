using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass][DoNotParallelize][TestCategory("AtomicGraphNative")]
public class GoogleAtomicGraphNativeTests
{
    private sealed class Owner():GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false)
    {
        public int Commits;public bool LoseAck;
        protected override async Task<IList<WriteResult>> CommitFirestoreConditionalBatchAsync(WriteBatch batch,CancellationToken token) {
            Commits++;var result=await base.CommitFirestoreConditionalBatchAsync(batch,token);
            if(LoseAck){LoseAck=false;throw new IOException("owned atomic graph ACK lost after native Firestore commit");}
            return result;
        }
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeAtomicGraphRevisionsReceiptReplayAndDeletes(bool synchronous,bool loseAck)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var work=Environment.GetEnvironmentVariable("OASIS_FIRESTORE_HISTORY_WORK")!;
        Assert.IsTrue(File.Exists(Path.Combine(work,"history-durability-ready.json")));
        Assert.AreEqual(0,Directory.GetFiles(work,"*.pending.json").Length);
        var journal=Path.Combine(work,$"history-native-atomic-{synchronous}-{loseAck}.pending.json");
        var root=new Holon{Id=Guid.NewGuid(),Name="owned Firestore atomic root",IsActive=true};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned Firestore atomic child",IsActive=true};
        root.Children.Add(child);root.MetaData["exact"]=0.1234567890123456789012345678m;
        var ids=new[]{root.Id,child.Id};var operations=Enumerable.Range(0,4).Select(_=>Guid.NewGuid()).ToArray();
        var provider=new Owner{IsProviderActivated=true,IsVersionControlEnabled=true,LoseAck=loseAck};
        var verifier=GoogleNativeFirestore.Database();
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,GoogleNativeFirestore.Database());
        string Key(Guid op)=>"GoogleCloudOASIS:"+op.ToString("N");
        ConditionalHolonWriteRequest Request(Holon current,int index,ConditionalHolonOperation kind,bool initial=false)
            =>new(new ConditionalHolonWrite(operations[index],Guid.Empty,ProviderType.GoogleCloudOASIS,
                new[]{current}.Concat(current.Children).Select(row=>new HolonWriteCondition(row.Id,initial?null:CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS)))),
                kind,new[]{current});
        try {
            foreach(var id in ids)Assert.IsFalse((await verifier.Collection("holons").Document(id.ToString()).GetSnapshotAsync()).Exists);
            foreach(var op in operations)foreach(var collection in new[]{"cosmicWriteReceipts","cosmicOperationFences"})
                Assert.IsFalse((await verifier.Collection(collection).Document(Key(op)).GetSnapshotAsync()).Exists);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,operations,synchronous,loseAck,state="before-native-Firestore-atomic-graph"}));file.Flush(true);
            }
            var request=Request(root,0,ConditionalHolonOperation.Save,true);
            var first=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
            if(loseAck) {
                Assert.AreEqual("COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED",first.ErrorCode);
                Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(first.Exception);Assert.AreEqual(1,provider.Commits);
                first=await provider.ExecuteConditionalHolonWriteAsync(request);
            }
            Assert.IsFalse(first.IsError,first.Exception?.ToString());var current=(Holon)first.Result.Single();
            var version=current.VersionId;Assert.AreEqual(1,current.Version);Assert.AreEqual(1,current.Children.Single().Version);
            var replay=await provider.ExecuteConditionalHolonWriteAsync(request);
            Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(version,replay.Result.Single().VersionId);
            Assert.AreEqual(1,provider.Commits);
            var observed=await verifier.Collection("holons").Document(root.Id.ToString()).GetSnapshotAsync();
            using(var json=JsonDocument.Parse(observed.GetValue<string>("data_json")))
                Assert.AreEqual(0.1234567890123456789012345678m,json.RootElement.GetProperty("MetaData").GetProperty("exact").GetDecimal());
            root.Name="different intent";
            Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",(await provider.ExecuteConditionalHolonWriteAsync(Request(root,0,ConditionalHolonOperation.Save,true))).ErrorCode);
            Assert.AreEqual(1,provider.Commits);
            Assert.AreEqual("COSMIC_CONCURRENCY_CONFLICT",(await provider.ExecuteConditionalHolonWriteAsync(Request(root,1,ConditionalHolonOperation.Save,true))).ErrorCode);
            Assert.AreEqual(1,provider.Commits);
            current.Name="owned replacement";
            for(var index=1;index<4;index++) {
                var kind=index==1?ConditionalHolonOperation.Save:index==2?ConditionalHolonOperation.SoftDelete:ConditionalHolonOperation.HardDelete;
                var result=await provider.ExecuteConditionalHolonWriteAsync(Request(current,index,kind));
                Assert.IsFalse(result.IsError,result.Exception?.ToString());current=(Holon)result.Result.Single();
                Assert.AreEqual(index+1,current.Version);Assert.AreEqual(index+1,current.Children.Single().Version);
            }
            Assert.AreEqual(4,provider.Commits);
            foreach(var id in ids)Assert.IsFalse((await verifier.Collection("holons").Document(id.ToString()).GetSnapshotAsync()).Exists);
            foreach(var op in operations) {
                var receipt=await verifier.Collection("cosmicWriteReceipts").Document(Key(op)).GetSnapshotAsync();
                Assert.AreEqual(op.ToString("D"),receipt.GetValue<string>("operationId"));
                var acknowledged=ConditionalHolonReceipt.Read(receipt.GetValue<string>("resultJson"));
                Assert.AreEqual(root.Id,acknowledged.Single().Id);Assert.AreEqual(child.Id,acknowledged.Single().Children.Single().Id);
                var fence=await verifier.Collection("cosmicOperationFences").Document(Key(op)).GetSnapshotAsync();
                Assert.AreEqual(op.ToString("D"),fence.GetValue<string>("operationId"));
                Assert.IsTrue(Guid.TryParseExact(fence.GetValue<string>("revision"),"D",out var revision)&&revision!=Guid.Empty);
                foreach(var snapshot in new[]{receipt,fence}) {
                    Assert.IsTrue(snapshot.UpdateTime.HasValue);
                    var ack=await snapshot.Reference.DeleteAsync(Precondition.LastUpdated(snapshot.UpdateTime.Value));
                    Assert.AreNotEqual(default(Timestamp),ack.UpdateTime);
                    Assert.IsFalse((await snapshot.Reference.GetSnapshotAsync()).Exists);
                }
            }
            File.Delete(journal);
        } finally {await provider.DeActivateProviderAsync();}
    }
}
