using System.Reflection;
using System.Text.Json;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass][DoNotParallelize][TestCategory("PreparationNative")]
public class GooglePreparationNativeTests
{
    private sealed class Owner():GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false)
    {
        public int Commits;public bool LoseAck;
        protected override async Task<IList<WriteResult>> CommitFirestoreConditionalBatchAsync(WriteBatch batch,CancellationToken token) {
            Commits++;var results=await base.CommitFirestoreConditionalBatchAsync(batch,token);
            if(LoseAck){LoseAck=false;throw new IOException("owned preparation ACK loss after actual Firestore commit");}
            return results;
        }
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeOpaqueFirstWinnerActorIsolationReceiptGateAndAckLoss(bool synchronous,bool loseAck)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var work=Environment.GetEnvironmentVariable("OASIS_FIRESTORE_HISTORY_WORK")!;
        Assert.IsTrue(File.Exists(Path.Combine(work,"history-durability-ready.json")));
        Assert.AreEqual(0,Directory.GetFiles(work,"*.pending.json").Length);
        var journal=Path.Combine(work,$"history-native-preparation-{synchronous}-{loseAck}.pending.json");
        var op=Guid.NewGuid();var blocked=Guid.NewGuid();var actor=Guid.NewGuid();var id=Guid.NewGuid();
        string Key(Guid operation)=>"GoogleCloudOASIS:"+operation.ToString("N");
        // Storage is deliberately opaque; these are NOT authenticated runtime envelopes.
        HolonWritePreparation Candidate(Guid operation,Guid owner,string suffix)=>new(operation,owner,ProviderType.GoogleCloudOASIS,
            new[]{new ProtectedHolonPreparation(id,"oasis-preparation1:owned-native-"+suffix)});
        var verifier=GoogleNativeFirestore.Database();
        var provider=new Owner{IsProviderActivated=true,LoseAck=loseAck};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!
            .SetValue(provider,GoogleNativeFirestore.Database());
        try {
            foreach(var operation in new[]{op,blocked})foreach(var collection in new[]{"cosmicWritePreparations","cosmicWriteReceipts","cosmicOperationFences"})
                Assert.IsFalse((await verifier.Collection(collection).Document(Key(operation)).GetSnapshotAsync()).Exists);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{op,blocked,actor,id,state="before-opaque-Firestore-native-claim"}));file.Flush(true);
            }
            var candidate=Candidate(op,actor,"first");
            var first=synchronous?provider.ClaimHolonWritePreparation(candidate):await provider.ClaimHolonWritePreparationAsync(candidate);
            if(loseAck) {
                Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",first.ErrorCode);
                Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(first.Exception);Assert.AreEqual(1,provider.Commits);
                first=await provider.ClaimHolonWritePreparationAsync(Candidate(op,actor,"new-candidate"));
            }
            Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.AreEqual(candidate.Records[0].Envelope,first.Result.Records[0].Envelope);
            Assert.AreEqual(1,provider.Commits);
            foreach(var operation in new[]{op,blocked}) {
                var ack=await verifier.Collection("cosmicWriteReceipts").Document(Key(operation)).CreateAsync(new Dictionary<string,object> {
                    ["operationId"]=operation.ToString("D"),["resultJson"]="owned receipt gate sentinel"
                });
                Assert.AreNotEqual(default(Timestamp),ack.UpdateTime);
            }
            var replay=await provider.ClaimHolonWritePreparationAsync(Candidate(op,actor,"must-not-replace"));
            Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(candidate.Records[0].Envelope,replay.Result.Records[0].Envelope);
            var denied=await provider.ClaimHolonWritePreparationAsync(Candidate(op,Guid.NewGuid(),"other-actor"));
            Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH",denied.ErrorCode);Assert.IsNull(denied.Result);
            var refused=await provider.ClaimHolonWritePreparationAsync(Candidate(blocked,actor,"must-not-create"));
            Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",refused.ErrorCode);
            Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(refused.Exception);Assert.IsNull(refused.Result);
            Assert.AreEqual(1,provider.Commits);
            foreach(var collection in new[]{"cosmicWritePreparations","cosmicOperationFences"})
                Assert.IsFalse((await verifier.Collection(collection).Document(Key(blocked)).GetSnapshotAsync()).Exists);
            var stored=await verifier.Collection("cosmicWritePreparations").Document(Key(op)).GetSnapshotAsync();
            Assert.AreEqual(actor.ToString("D"),stored.GetValue<string>("actorId"));
            Assert.AreEqual(op.ToString("D"),stored.GetValue<string>("operationId"));
            Assert.AreEqual(ProviderType.GoogleCloudOASIS.ToString(),stored.GetValue<string>("authority"));
            var winner=ProtectedPreparationJson.Read(candidate,stored.GetValue<string>("payloadJson"));
            Assert.AreEqual(id,winner.Records[0].HolonId);Assert.AreEqual(candidate.Records[0].Envelope,winner.Records[0].Envelope);
            var fence=await verifier.Collection("cosmicOperationFences").Document(Key(op)).GetSnapshotAsync();
            Assert.AreEqual(op.ToString("D"),fence.GetValue<string>("operationId"));
            Assert.IsTrue(Guid.TryParseExact(fence.GetValue<string>("revision"),"D",out var revision)&&revision!=Guid.Empty);
            var receipts=new List<DocumentSnapshot>();
            foreach(var operation in new[]{op,blocked}) {
                var receipt=await verifier.Collection("cosmicWriteReceipts").Document(Key(operation)).GetSnapshotAsync();
                Assert.AreEqual(operation.ToString("D"),receipt.GetValue<string>("operationId"));
                Assert.AreEqual("owned receipt gate sentinel",receipt.GetValue<string>("resultJson"));receipts.Add(receipt);
            }
            foreach(var snapshot in new[]{stored,fence}.Concat(receipts)) {
                Assert.IsTrue(snapshot.UpdateTime.HasValue);
                var ack=await snapshot.Reference.DeleteAsync(Precondition.LastUpdated(snapshot.UpdateTime.Value));
                Assert.AreNotEqual(default(Timestamp),ack.UpdateTime);Assert.IsFalse((await snapshot.Reference.GetSnapshotAsync()).Exists);
            }
            File.Delete(journal);
        } finally {await provider.DeActivateProviderAsync();}
    }
}
