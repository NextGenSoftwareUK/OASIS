using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Mongo = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class NativeHostedReplicaRestartTests
{
    private static async Task VerifyMongoReplicaHeadAudit(Mongo target, CosmicStorageIdentity binding,
        CosmicStorageIdentity source, IMongoDatabase database, IHolon input)
    {
        var missing = Guid.NewGuid();
        var request = new HolonReplicaHeadReadRequest(binding, new[] { input.Id, missing });
        var heads = database.GetCollection<BsonDocument>("CosmicReplicaHead");
        var key = new BsonDocument("_id", input.Id.ToString("D"));
        var original = await heads.Find(key).SingleAsync();
        var digest = original["SnapshotDigest"].AsString;
        var result = await target.ReadHolonReplicaHeadsAsync(request);
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(binding.BindingFingerprint, result.Result.StorageFingerprint);
        Assert.AreEqual(2, result.Result.Records.Count);
        var observed = result.Result.Records[0];
        Assert.AreEqual(input.Id, observed.HolonId);
        Assert.IsTrue(observed.HasCurrentRecord);
        Assert.AreEqual(ProviderType.SQLLiteDBOASIS, observed.Head.SourceAuthority);
        Assert.AreEqual(new HolonReplicationRouteBinding(source, binding, HolonReplicationRoutes.DefaultPolicyBinding).SourceId, observed.Head.SourceId);
        Assert.AreEqual(input.VersionId, observed.Head.VersionId);
        Assert.AreEqual(input.Version, observed.Head.Version);
        Assert.AreEqual(digest, observed.Head.SnapshotDigest);
        Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId((await target.LoadHolonAsync(input.Id, loadChildren: false)).Result,
            ProviderType.MongoDBOASIS), observed.Head.ProviderRevisionId);
        Assert.AreEqual(missing, result.Result.Records[1].HolonId);
        Assert.IsNull(result.Result.Records[1].Head);
        Assert.IsFalse(result.Result.Records[1].HasCurrentRecord);
        Assert.AreEqual(original.ToJson(), (await heads.Find(key).SingleAsync()).ToJson(), "Audit cannot alter native head state.");

        var wrong = new CosmicStorageIdentity(binding.Authority, binding.EndpointFingerprint, Guid.NewGuid());
        var mismatch = await target.ReadHolonReplicaHeadsAsync(new(wrong, new[] { input.Id }));
        Assert.IsTrue(mismatch.IsError); Assert.IsNull(mismatch.Result);
        Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_STORAGE_MISMATCH", mismatch.ErrorCode);

        // Only the journaled fixture's hash is corrupted. CAS restoration cannot
        // overwrite an unrelated concurrent head change or touch a receipt.
        try
        {
            var changed = await heads.UpdateOneAsync(new BsonDocument { { "_id", input.Id.ToString("D") }, { "SnapshotDigest", digest } },
                new BsonDocument("$set", new BsonDocument("SnapshotDigest", "invalid-checksum")));
            Assert.AreEqual(1L, changed.ModifiedCount);
            var corrupt = await target.ReadHolonReplicaHeadsAsync(request);
            Assert.IsTrue(corrupt.IsError); Assert.IsNull(corrupt.Result);
            Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED", corrupt.ErrorCode);
            Assert.IsNotNull(corrupt.Exception);
        }
        finally
        {
            var restored = await heads.UpdateOneAsync(new BsonDocument { { "_id", input.Id.ToString("D") }, { "SnapshotDigest", "invalid-checksum" } },
                new BsonDocument("$set", new BsonDocument("SnapshotDigest", digest)));
            Assert.AreEqual(1L, restored.ModifiedCount);
        }
        var verified = await target.ReadHolonReplicaHeadsAsync(request);
        Assert.IsFalse(verified.IsError, verified.Message + verified.Exception);
        Assert.AreEqual(digest, verified.Result.Records[0].Head.SnapshotDigest);
    }
}
