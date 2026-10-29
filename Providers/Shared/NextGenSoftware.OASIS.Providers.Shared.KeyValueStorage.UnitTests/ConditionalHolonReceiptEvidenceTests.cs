using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class ConditionalHolonReceiptEvidenceTests
{
    private static Holon Record() {
        var row = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(), Name = "private-value" };
        CosmicConcurrency.SetProviderRevisionId(row, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        return row;
    }
    private static ConditionalHolonReceiptEvidenceRequest Request(Holon[] records, Guid[] selected = null)
    {
        var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(),
            ProviderType.SQLLiteDBOASIS, records.Select(r => new HolonWriteCondition(r.Id, null))),
            ConditionalHolonOperation.Save, records, saveChildren: false);
        return new(original, new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.NewGuid()),
            new CosmicStorageIdentity(ProviderType.Neo4jOASIS, new string('B', 64), Guid.NewGuid()), selected ?? records.Select(r => r.Id).ToArray());
    }

    [TestMethod]
    public void ProjectionBindsPhysicalComparisonScopeAndPreservesRequestedOrderWithoutValues()
    {
        var first = Record(); var second = Record(); var request = Request(new[] { first, second }, new[] { second.Id, first.Id });
        var result = ConditionalHolonReceiptEvidenceProjection.Create(request, new[] { first, second });
        CollectionAssert.AreEqual(request.HolonIds.ToArray(), result.Records.Select(r => r.HolonId).ToArray());
        Assert.AreEqual(request.ExpectedSource.BindingFingerprint, result.SourceStorageFingerprint);
        Assert.AreEqual(request.ComparisonTarget.BindingFingerprint, result.ComparisonTargetFingerprint);
        Assert.AreEqual(2, result.OriginalSelectedRecordCount);
        Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId(second, ProviderType.SQLLiteDBOASIS), result.Records[0].SourceProviderRevisionId);
        Assert.IsFalse(JsonSerializer.Serialize(result).Contains("private-value", StringComparison.Ordinal));
        Assert.AreEqual(CommittedHolonReplicaPlan.PortableContentDigest(second, ProviderType.Neo4jOASIS), result.Records[0].PortableContentDigest);
    }

    [TestMethod]
    public void SubsetDoesNotClaimToBeTheWholeOriginalGraph()
    {
        var first = Record(); var second = Record(); var request = Request(new[] { first, second }, new[] { second.Id });
        var result = ConditionalHolonReceiptEvidenceProjection.Create(request, new[] { first, second });
        Assert.AreEqual(2, result.OriginalSelectedRecordCount); Assert.AreEqual(1, result.Records.Count);
        Assert.ThrowsException<System.IO.InvalidDataException>(() => ConditionalHolonReceiptEvidenceProjection.Create(request, new[] { second }));
    }

    [DataTestMethod]
    [DataRow("empty")] [DataRow("duplicate")] [DataRow("foreign")] [DataRow("zero")] [DataRow("over-limit")]
    public void ProjectionSelectionMustBeBoundedDistinctAndPartOfOriginalWrite(string fault)
    {
        var record = Record();
        var ids = fault switch { "empty" => Array.Empty<Guid>(), "duplicate" => new[] { record.Id, record.Id },
            "foreign" => new[] { Guid.NewGuid() }, "zero" => new[] { Guid.Empty }, _ => Enumerable.Range(0, 257).Select(_ => Guid.NewGuid()).ToArray() };
        Assert.ThrowsException<ArgumentException>(() => Request(new[] { record }, ids));
    }

    [TestMethod]
    public void PortableDigestUsesReplicaAcknowledgementSemanticsWithoutNativeRevisionOrTargetMetadata()
    {
        var record = Record(); var original = CommittedHolonReplicaPlan.PortableContentDigest(record, ProviderType.Neo4jOASIS);
        record.ProviderUniqueStorageKey[ProviderType.Neo4jOASIS] = "local-native-key";
        record.ProviderMetaData[ProviderType.Neo4jOASIS] = new Dictionary<string, string> { ["local-only"] = "target-secret" };
        CosmicConcurrency.SetProviderRevisionId(record, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        Assert.AreEqual(original, CommittedHolonReplicaPlan.PortableContentDigest(record, ProviderType.Neo4jOASIS));
        record.Name = "different";
        Assert.AreNotEqual(original, CommittedHolonReplicaPlan.PortableContentDigest(record, ProviderType.Neo4jOASIS));
    }

    [TestMethod]
    public void PortableDigestBindsComparisonAuthorityAndLogicalLineage()
    {
        var record = Record(); var before = CommittedHolonReplicaPlan.PortableContentDigest(record, ProviderType.Neo4jOASIS);
        Assert.AreNotEqual(before, CommittedHolonReplicaPlan.PortableContentDigest(record, ProviderType.MongoDBOASIS));
        record.PreviousVersionId = record.VersionId; record.VersionId = Guid.NewGuid(); record.Version++;
        Assert.AreNotEqual(before, CommittedHolonReplicaPlan.PortableContentDigest(record, ProviderType.Neo4jOASIS));
    }

    [DataTestMethod]
    [DataRow(false, ProviderType.SQLLiteDBOASIS)] [DataRow(true, ProviderType.SQLLiteDBOASIS)]
    [DataRow(false, ProviderType.MongoDBOASIS)] [DataRow(true, ProviderType.MongoDBOASIS)]
    [DataRow(false, ProviderType.Neo4jOASIS)] [DataRow(true, ProviderType.Neo4jOASIS)]
    public void ReceiptAndProtectedHeadUseTheSamePortableContentProof(bool deleted, ProviderType target)
    {
        var source = new Holon { Id = Guid.NewGuid(), Version = 2, VersionId = Guid.NewGuid(),
            PreviousVersionId = Guid.NewGuid(), Name = "private-comparison-value", IsActive = !deleted,
            DeletedDate = deleted ? new DateTime(2026, 10, 29, 0, 0, 0, DateTimeKind.Utc) : DateTime.MinValue };
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.MongoDBOASIS, Guid.NewGuid());
        var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(),
            ProviderType.MongoDBOASIS, new[] { new HolonWriteCondition(source.Id, Guid.NewGuid()) }),
            deleted ? ConditionalHolonOperation.HardDelete : ConditionalHolonOperation.Save, new[] { source }, saveChildren: false);
        var request = new ConditionalHolonReceiptEvidenceRequest(original,
            new CosmicStorageIdentity(ProviderType.MongoDBOASIS, new string('A', 64), Guid.NewGuid()),
            new CosmicStorageIdentity(target, new string('B', 64), Guid.NewGuid()), new[] { source.Id });
        var evidence = ConditionalHolonReceiptEvidenceProjection.Create(request, new[] { source });
        var replica = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(
            NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(source));
        var revision = Guid.NewGuid(); CosmicConcurrency.SetProviderRevisionId(replica, target, revision);
        replica.ProviderUniqueStorageKey[target] = "receiver-local-key";
        var json = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(replica);
        var checksum = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json)));
        var head = HolonReplicaHeadRecord.Read(source.Id, target, "original-lineage", ProviderType.MongoDBOASIS.ToString(),
            json, checksum, revision.ToString("D"), deleted).Evidence;
        Assert.AreEqual(evidence.Records[0].PortableContentDigest, head.PortableContentDigest);
        Assert.AreEqual(ConditionalHolonReceiptEvidence.ContentFormat, head.PortableContentFormat);
        Assert.AreEqual(deleted, evidence.Records[0].HardDeleted);
    }

    [TestMethod]
    public void EvidenceCannotBeAttachedToAnOutcomeForAnotherActorOrOperation()
    {
        var record = Record(); var request = Request(new[] { record });
        var evidence = ConditionalHolonReceiptEvidenceProjection.Create(request, new[] { record });
        Assert.ThrowsException<ArgumentException>(() => new ConditionalHolonWriteOutcome(Guid.NewGuid(),
            request.Original.Write.ActorId, request.Original.Write.Authority, ConditionalHolonOperation.Save,
            ConditionalHolonReceiptState.Committed, null, evidence));
        Assert.ThrowsException<ArgumentException>(() => new ConditionalHolonWriteOutcome(request.Original.Write.OperationId,
            Guid.NewGuid(), request.Original.Write.Authority, ConditionalHolonOperation.Save,
            ConditionalHolonReceiptState.Committed, null, evidence));
    }

    [TestMethod]
    public void AbsentOutcomeCannotCarryCommittedEvidence()
    {
        var record = Record(); var request = Request(new[] { record });
        var evidence = ConditionalHolonReceiptEvidenceProjection.Create(request, new[] { record });
        Assert.ThrowsException<ArgumentException>(() => new ConditionalHolonWriteOutcome(request.Original.Write.OperationId,
            request.Original.Write.ActorId, request.Original.Write.Authority, ConditionalHolonOperation.Save,
            ConditionalHolonReceiptState.Absent, null, evidence));
    }
}
