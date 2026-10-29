using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Sqlite = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class NativeHostedReplicaRestartTests
{
    private static async Task VerifySqliteReplicaHeadAudit(Sqlite target, CosmicStorageIdentity binding, string file, Holon input)
    {
        var missing = Guid.NewGuid();
        var request = new HolonReplicaHeadReadRequest(binding, new[] { input.Id, missing });
        using var native = new SqliteConnection($"Data Source={file};Pooling=False");
        await native.OpenAsync();
        async Task<string> ReadDigest()
        {
            using var command = native.CreateCommand();
            command.CommandText = "SELECT SnapshotDigest FROM CosmicReplicaHead WHERE HolonId=$id";
            command.Parameters.AddWithValue("$id", input.Id.ToString("D"));
            return (string)await command.ExecuteScalarAsync();
        }
        async Task WriteDigest(string digest)
        {
            using var command = native.CreateCommand();
            command.CommandText = "UPDATE CosmicReplicaHead SET SnapshotDigest=$digest WHERE HolonId=$id";
            command.Parameters.AddWithValue("$id", input.Id.ToString("D"));
            command.Parameters.AddWithValue("$digest", digest);
            Assert.AreEqual(1, await command.ExecuteNonQueryAsync());
        }
        var originalDigest = await ReadDigest();
        var first = await target.ReadHolonReplicaHeadsAsync(request);
        Assert.IsFalse(first.IsError, first.Message + first.Exception);
        Assert.IsFalse(first.IsSaved);
        Assert.AreEqual(binding.BindingFingerprint, first.Result.StorageFingerprint);
        Assert.AreEqual(2, first.Result.Records.Count);
        var observed = first.Result.Records[0];
        Assert.AreEqual(input.Id, observed.HolonId);
        Assert.IsTrue(observed.HasCurrentRecord);
        Assert.AreEqual(ProviderType.Neo4jOASIS, observed.Head.SourceAuthority);
        Assert.AreEqual(input.VersionId, observed.Head.VersionId);
        Assert.AreEqual(input.Version, observed.Head.Version);
        Assert.AreEqual(originalDigest, observed.Head.SnapshotDigest);
        Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId((await target.LoadHolonAsync(input.Id, loadChildren:false)).Result,
            ProviderType.SQLLiteDBOASIS), observed.Head.ProviderRevisionId);
        Assert.AreEqual(missing, first.Result.Records[1].HolonId);
        Assert.IsFalse(first.Result.Records[1].HasCurrentRecord);
        Assert.IsNull(first.Result.Records[1].Head);
        Assert.AreEqual(originalDigest, await ReadDigest(), "Audit must not change the protected head.");

        var wrong = new CosmicStorageIdentity(binding.Authority, binding.EndpointFingerprint, Guid.NewGuid());
        var mismatch = await target.ReadHolonReplicaHeadsAsync(new(wrong, new[] { input.Id }));
        Assert.IsTrue(mismatch.IsError); Assert.IsNull(mismatch.Result);
        Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_STORAGE_MISMATCH", mismatch.ErrorCode);

        // Corrupt only this fixture's protected hash, verify rejection, then restore
        // the exact original bytes. No receipt or source lineage is modified.
        try
        {
            await WriteDigest("invalid-checksum");
            var corrupt = await target.ReadHolonReplicaHeadsAsync(request);
            Assert.IsTrue(corrupt.IsError); Assert.IsNull(corrupt.Result);
            Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_UNVERIFIED", corrupt.ErrorCode);
            Assert.IsNotNull(corrupt.Exception);
        }
        finally { await WriteDigest(originalDigest); }
        var restored = await target.ReadHolonReplicaHeadsAsync(request);
        Assert.IsFalse(restored.IsError, restored.Message + restored.Exception);
        Assert.AreEqual(originalDigest, restored.Result.Records[0].Head.SnapshotDigest);
    }
}
