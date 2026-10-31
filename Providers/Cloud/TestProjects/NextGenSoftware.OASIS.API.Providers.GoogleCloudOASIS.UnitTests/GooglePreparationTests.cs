using Google.Cloud.Firestore.V1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

public partial class GoogleAtomicGraphTests
{
    private static HolonWritePreparation Candidate(Guid operation,Guid actor,string suffix="first",ProviderType authority=ProviderType.GoogleCloudOASIS)
        =>new(operation,actor,authority,new[]{new ProtectedHolonPreparation(Guid.NewGuid(),"oasis-preparation1:"+suffix)});
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task PreparationReturnsOriginalWinnerWithReceiptAndRefusesOtherActor(bool synchronous) {
        var client=new Client();var provider=Provider(client);var op=Guid.NewGuid();var actor=Guid.NewGuid();
        var candidate=Candidate(op,actor);
        var first=synchronous?provider.ClaimHolonWritePreparation(candidate):await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.AreEqual(2,client.Submitted!.Writes.Count);
        var key="projects/owned-atomic/databases/(default)/documents/cosmicWriteReceipts/GoogleCloudOASIS:"+op.ToString("N");
        client.Documents.Add(key,new Document{Name=key});
        var replay=await provider.ClaimHolonWritePreparationAsync(Candidate(op,actor,"different"));
        Assert.IsFalse(replay.IsError,replay.Exception?.ToString());
        Assert.AreEqual(candidate.Records[0].HolonId,replay.Result.Records[0].HolonId);
        Assert.AreEqual(candidate.Records[0].Envelope,replay.Result.Records[0].Envelope);
        var refused=await provider.ClaimHolonWritePreparationAsync(Candidate(op,Guid.NewGuid()));
        Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH",refused.ErrorCode);Assert.IsNull(refused.Result);Assert.AreEqual(1,client.Commits);
        var preparationKey="projects/owned-atomic/databases/(default)/documents/cosmicWritePreparations/GoogleCloudOASIS:"+op.ToString("N");
        client.Documents[preparationKey].Fields["payloadJson"]=new Value{StringValue="malformed protected transport"};
        var otherActor=await provider.ClaimHolonWritePreparationAsync(Candidate(op,Guid.NewGuid()));
        Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH",otherActor.ErrorCode,"Ownership must be checked before parsing a stored payload.");
        Assert.IsNull(otherActor.Result);
        var corrupt=await provider.ClaimHolonWritePreparationAsync(Candidate(op,actor));
        Assert.IsTrue(corrupt.IsError);Assert.IsNull(corrupt.Result);Assert.AreEqual(1,client.Commits);
    }
    [TestMethod]
    public async Task PreparationReceiptGateDoesNotBackfillAndLostAckIsNotResubmitted() {
        var client=new Client{LoseAck=true};var provider=Provider(client);
        var candidate=Candidate(Guid.NewGuid(),Guid.NewGuid());
        var unknown=await provider.ClaimHolonWritePreparationAsync(candidate);
        Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",unknown.ErrorCode);
        Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(unknown.Exception);
        Assert.IsFalse((await provider.ClaimHolonWritePreparationAsync(candidate)).IsError);Assert.AreEqual(1,client.Commits);
        var blocked=Candidate(Guid.NewGuid(),Guid.NewGuid());
        var key="projects/owned-atomic/databases/(default)/documents/cosmicWriteReceipts/GoogleCloudOASIS:"+blocked.OperationId.ToString("N");
        client.Documents.Add(key,new Document{Name=key});var before=client.Documents.Count;
        var refused=await provider.ClaimHolonWritePreparationAsync(blocked);
        Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",refused.ErrorCode);Assert.IsNull(refused.Result);
        Assert.AreEqual(before,client.Documents.Count);Assert.AreEqual(1,client.Commits);
    }
    [DataTestMethod][DataRow("authority")][DataRow("capacity")][DataRow("cancel")]
    public async Task PreparationAdmissionHasNoNativeReadOrWrite(string kind) {
        var client=new Client();var provider=Provider(client);
        var candidate=Candidate(Guid.NewGuid(),Guid.NewGuid(),kind=="capacity"?new string('x',1024*1024):"opaque",
            kind=="authority"?ProviderType.OracleDBOASIS:ProviderType.GoogleCloudOASIS);
        var result=await provider.ClaimHolonWritePreparationAsync(candidate,kind=="cancel"?new CancellationToken(true):default);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Commits);
    }
    [TestMethod]
    public async Task PreparationNativeContentionWithoutWinnerIsExplicitAndNotRetried() {
        var client=new Client{Reject=true};var provider=Provider(client);
        var result=await provider.ClaimHolonWritePreparationAsync(Candidate(Guid.NewGuid(),Guid.NewGuid()));
        Assert.AreEqual("COSMIC_PREPARATION_CONCURRENT_CLAIM",result.ErrorCode);Assert.IsNull(result.Result);
        Assert.IsFalse(result.Exception is INativeMutationReconciliationRequired);Assert.AreEqual(1,client.Commits);
    }
}
