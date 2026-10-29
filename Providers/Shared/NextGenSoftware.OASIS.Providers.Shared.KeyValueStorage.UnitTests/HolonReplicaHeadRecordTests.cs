using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class HolonReplicaHeadRecordTests
{
    [DataTestMethod]
    [DataRow(false, ProviderType.SQLLiteDBOASIS)] [DataRow(true, ProviderType.SQLLiteDBOASIS)]
    [DataRow(false, ProviderType.MongoDBOASIS)] [DataRow(true, ProviderType.MongoDBOASIS)]
    [DataRow(false, ProviderType.Neo4jOASIS)] [DataRow(true, ProviderType.Neo4jOASIS)]
    public void ProtectedHeadRetainsLineageAndDeletionWithoutExposingTheGraph(bool deleted, ProviderType target)
    {
        var record = new Holon { Id = Guid.NewGuid(), Version = 2, VersionId = Guid.NewGuid(),
            PreviousVersionId = Guid.NewGuid(), DeletedDate = deleted ? new DateTime(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc) : DateTime.MinValue, Name = "private business content" };
        var revision = Guid.NewGuid();
        CosmicConcurrency.SetProviderRevisionId(record, target, revision);
        var json = StoredRecordJson.Serialize(record);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var validated = HolonReplicaHeadRecord.Read(record.Id, target, "original-source",
            ProviderType.Neo4jOASIS.ToString(), json, digest, revision.ToString("D"), deleted);
        Assert.AreEqual(record.VersionId, validated.Evidence.VersionId);
        Assert.AreEqual(record.PreviousVersionId, validated.Evidence.PreviousVersionId);
        Assert.AreEqual(revision, validated.Evidence.ProviderRevisionId);
        Assert.AreEqual(deleted, validated.Evidence.HardDeleted);
        Assert.AreEqual(digest, validated.Evidence.SnapshotDigest);
        Assert.IsFalse(typeof(HolonReplicaHeadEvidence).GetProperties().Any(value => value.Name is "Snapshot" or "Name" or "MetaData"));
    }
    [DataTestMethod]
    [DataRow("digest")] [DataRow("identity")] [DataRow("revision")] [DataRow("logical-revision")]
    [DataRow("authority")] [DataRow("source")] [DataRow("deleted")] [DataRow("version")]
    public void CorruptProtectedEvidenceCannotBecomeMigrationProof(string fault)
    {
        var record = new Holon { Id = Guid.NewGuid(), Version = fault == "version" ? 0 : 1, VersionId = Guid.NewGuid() };
        var revision = fault == "logical-revision" ? record.VersionId : Guid.NewGuid();
        CosmicConcurrency.SetProviderRevisionId(record, ProviderType.SQLLiteDBOASIS, revision);
        var json = StoredRecordJson.Serialize(record);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        Assert.ThrowsException<Exception>(() => {
            try {
                HolonReplicaHeadRecord.Read(fault == "identity" ? Guid.NewGuid() : record.Id, ProviderType.SQLLiteDBOASIS,
                    fault == "source" ? " padded " : "original-source", fault == "authority" ? "Default" : "Neo4jOASIS",
                    json, fault == "digest" ? new string('A', 64) : digest,
                    (fault == "revision" ? Guid.NewGuid() : revision).ToString("D"), fault == "deleted");
            }
            catch (Exception cause) when (cause is InvalidDataException or ArgumentException) { throw new Exception("Rejected protected corruption.", cause); }
        });
    }
    [TestMethod]
    public void ExactReadIntentFreezesItsBoundedIdentitySet()
    {
        var storage = new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.NewGuid());
        var first = Guid.NewGuid(); var values = new[] { first };
        var request = new HolonReplicaHeadReadRequest(storage, values); values[0] = Guid.NewGuid();
        Assert.AreEqual(first, request.HolonIds[0]);
        Assert.ThrowsException<ArgumentException>(() => new HolonReplicaHeadReadRequest(storage, new[] { first, first }));
        Assert.ThrowsException<ArgumentException>(() => new HolonReplicaHeadReadRequest(storage, Enumerable.Range(0,257).Select(_ => Guid.NewGuid())));
        Assert.ThrowsException<ArgumentException>(() => new HolonReplicaHeadReadRequest(storage, Array.Empty<Guid>()));
        Assert.ThrowsException<ArgumentException>(() => new HolonReplicaHeadReadRequest(storage, new[] { Guid.Empty }));
    }
    [TestMethod]
    public void HeadAuditConsistencyIsExplicitAndStrictByDefault()
    {
        var storage = new CosmicStorageIdentity(ProviderType.Neo4jOASIS, new string('A',64), Guid.NewGuid());
        var ids = new[] { Guid.NewGuid() };
        Assert.AreEqual(HolonReplicaHeadReadConsistency.NativeSnapshot, new HolonReplicaHeadReadRequest(storage,ids).RequiredConsistency);
        Assert.AreEqual(HolonReplicaHeadReadConsistency.ReadCommittedValidated,
            new HolonReplicaHeadReadRequest(storage,ids,HolonReplicaHeadReadConsistency.ReadCommittedValidated).RequiredConsistency);
        var rows = new[] { new HolonReplicaHeadObservation(ids[0],false,null) };
        Assert.AreEqual(HolonReplicaHeadReadConsistency.NativeSnapshot,new HolonReplicaHeadReadResult(storage,rows).Consistency);
        Assert.AreEqual(HolonReplicaHeadReadConsistency.ReadCommittedValidated,
            new HolonReplicaHeadReadResult(storage,rows,HolonReplicaHeadReadConsistency.ReadCommittedValidated).Consistency);
        Assert.ThrowsException<ArgumentOutOfRangeException>(()=>new HolonReplicaHeadReadRequest(storage,ids,(HolonReplicaHeadReadConsistency)99));
        Assert.ThrowsException<ArgumentOutOfRangeException>(()=>new HolonReplicaHeadReadResult(storage,rows,(HolonReplicaHeadReadConsistency)99));
    }

}
