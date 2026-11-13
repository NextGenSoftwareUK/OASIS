using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    private static async Task VerifyNeoReplicaHeadAudit(Provider target,CosmicStorageIdentity binding,
        IAsyncSession inspection,IHolon input,string sourceId,bool deleted,bool corruptHash)
    {
        var missing=Guid.NewGuid();var ids=new[]{input.Id,missing};
        var strict=await target.ReadHolonReplicaHeadsAsync(new(binding,ids));
        Assert.IsTrue(strict.IsError);Assert.IsNull(strict.Result);
        Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_CONSISTENCY_UNSUPPORTED",strict.ErrorCode);
        var request=new HolonReplicaHeadReadRequest(binding,ids,HolonReplicaHeadReadConsistency.ReadCommittedValidated);
        var read=await target.ReadHolonReplicaHeadsAsync(request);
        Assert.IsFalse(read.IsError,read.Message+read.Exception);
        Assert.AreEqual(HolonReplicaHeadReadConsistency.ReadCommittedValidated,read.Result.Consistency);
        Assert.AreEqual(binding.BindingFingerprint,read.Result.StorageFingerprint);
        Assert.AreEqual(2,read.Result.Records.Count);
        var observed=read.Result.Records[0];Assert.AreEqual(input.Id,observed.HolonId);
        Assert.AreEqual(!deleted,observed.HasCurrentRecord);Assert.AreEqual(deleted,observed.Head.HardDeleted);
        Assert.AreEqual(sourceId,observed.Head.SourceId);Assert.AreEqual(ProviderType.SQLLiteDBOASIS,observed.Head.SourceAuthority);
        Assert.AreEqual(input.VersionId,observed.Head.VersionId);Assert.AreEqual(input.Version,observed.Head.Version);
        Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId(input,ProviderType.Neo4jOASIS),observed.Head.ProviderRevisionId);
        Assert.AreEqual(missing,read.Result.Records[1].HolonId);Assert.IsNull(read.Result.Records[1].Head);
        Assert.IsFalse(read.Result.Records[1].HasCurrentRecord);
        var wrong=new CosmicStorageIdentity(binding.Authority,binding.EndpointFingerprint,Guid.NewGuid());
        var mismatch=await target.ReadHolonReplicaHeadsAsync(new(wrong,ids,HolonReplicaHeadReadConsistency.ReadCommittedValidated));
        Assert.IsTrue(mismatch.IsError);Assert.IsNull(mismatch.Result);
        Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_STORAGE_MISMATCH",mismatch.ErrorCode);
        if(!corruptHash)return;
        var original=observed.Head.SnapshotDigest;
        async Task Change(string expected,string replacement)
        {
            var result=await(await inspection.RunAsync("MATCH (r:CosmicReplicaHead {HolonId:$id,SnapshotDigest:$expected}) "
                +"SET r.SnapshotDigest=$replacement RETURN count(r) AS count",new{id=input.Id.ToString("D"),expected,replacement})).SingleAsync();
            Assert.AreEqual(1L,result["count"].As<long>());
        }
        try
        {
            await Change(original,"invalid-checksum");
            var corrupt=await target.ReadHolonReplicaHeadsAsync(request);
            Assert.IsTrue(corrupt.IsError);Assert.IsNull(corrupt.Result);
            Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED",corrupt.ErrorCode);
        }
        finally{await Change("invalid-checksum",original);}
        var restored=await target.ReadHolonReplicaHeadsAsync(request);
        Assert.IsFalse(restored.IsError,restored.Message+restored.Exception);
        Assert.AreEqual(original,restored.Result.Records[0].Head.SnapshotDigest);
    }
}
