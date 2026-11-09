using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationPayloadReceiptProofTests
{
    public class PayloadProbe : DispatchProxy
    {
        public int Reads;
        public OASISResult<string> Outcome;
        protected override object Invoke(MethodInfo method, object[] args)
        {
            Assert.AreEqual("ReadFrozenPayloadAsync", method.Name, "Proof orchestration must never claim or replay.");
            Reads++; return Task.FromResult(Outcome);
        }
    }

    [DataTestMethod]
    [DataRow("matched")] [DataRow("error")] [DataRow("wrong-store")] [DataRow("cancelled")] [DataRow("cleanup-warning")]
    public async Task BoundProofReaderPublishesOnlyRedactedExactEvidenceAndRetainsNativeFailures(string mode)
    {
        var f = new Fixture(); var native = DispatchProxy.Create<IHolonReplicationPayloadAuditOutbox, PayloadProbe>();
        var probe = (PayloadProbe)native; var cause = new InvalidOperationException("native-proof-read-cause");
        probe.Outcome = mode == "error" ? new() { IsError = true, ErrorCode = "NATIVE_PAYLOAD_REJECTED", ErrorCount = 3, Exception = cause }
            : new(f.Payload) { IsWarning = mode == "cleanup-warning", WarningCount = mode == "cleanup-warning" ? 1 : 0,
                Exception = mode == "cleanup-warning" ? cause : null };
        using var cancel = new CancellationTokenSource(); if (mode == "cancelled") cancel.Cancel();
        var expected = mode == "wrong-store" ? new CosmicStorageIdentity(f.Source.Authority, f.Source.EndpointFingerprint, Guid.NewGuid()) : f.Source;
        var result = await HolonReplicationPayloadReceiptVerifier.ReadAsync(native, expected, f.Row, f.Original, f.Receipt, cancellationToken: cancel.Token);
        Assert.AreEqual(mode is "error" or "wrong-store" or "cancelled", result.IsError);
        Assert.AreEqual(mode is "wrong-store" or "cancelled" ? 0 : 1, probe.Reads);
        if (result.IsError) Assert.IsNull(result.Result);
        else { Assert.IsNotNull(result.Result); Assert.IsFalse(JsonSerializer.Serialize(result.Result).Contains("private-root-value")); }
        if (mode == "error") { Assert.AreEqual("NATIVE_PAYLOAD_REJECTED", result.ErrorCode); Assert.AreEqual(3, result.ErrorCount); Assert.AreSame(cause, result.Exception); }
        if (mode == "cleanup-warning") { Assert.IsTrue(result.IsWarning); Assert.AreSame(cause, result.Exception); }
    }

    private sealed class Fixture
    {
        public readonly CosmicStorageIdentity Source = new(ProviderType.SQLLiteDBOASIS, new string('A',64), Guid.NewGuid());
        public readonly CosmicStorageIdentity Target = new(ProviderType.Neo4jOASIS, new string('B',64), Guid.NewGuid());
        public readonly Guid Operation = Guid.NewGuid(), Actor = Guid.NewGuid();
        public readonly Holon Root;
        public ConditionalHolonWriteRequest Original;
        public HolonReplicationReceiptPreflightReport Receipt;
        public string Payload;
        public HolonReplicationOperationObligation Row;
        public string TargetId = "target";
        public Fixture(bool native = false)
        {
            Root = Record("private-root-value"); var child = Record("private-child-value");
            child.ParentHolonId = Root.Id; Root.Children.Add(child);
            var route = new HolonReplicationRouteBinding(Source, Target, "native-policy");
            if (native) TargetId = route.TargetId;
            var enrollment = native ? new HolonReplicationEnrollment(route.SourceId, new[] { route.TargetId }, Source, new[] { route })
                : new HolonReplicationEnrollment("lineage", new[] { "target", "other" });
            Original = new(new ConditionalHolonWrite(Operation, Actor, Source.Authority,
                new[] { new HolonWriteCondition(Root.Id, null), new HolonWriteCondition(child.Id, null) }, enrollment),
                ConditionalHolonOperation.Save, new[] { Root });
            Receipt = Report(false); Payload = Capture(new[] { Root }); Row = Observation(Payload);
        }
        private Holon Record(string name)
        {
            var record = new Holon { Id = Guid.NewGuid(), Name = name, Version = 1, VersionId = Guid.NewGuid() };
            CosmicConcurrency.SetProviderRevisionId(record, Source.Authority, Guid.NewGuid()); return record;
        }
        public HolonReplicationReceiptPreflightReport Report(bool partial)
        {
            var ids = partial ? new[] { Root.Id } : new[] { Root.Id, Root.Children.Single().Id };
            var request = new ConditionalHolonReceiptEvidenceRequest(Original, Source, Target, ids);
            var evidence = ConditionalHolonReceiptEvidenceProjection.Create(request, new[] { Root });
            var outcome = new ConditionalHolonWriteOutcome(Operation, Actor, Source.Authority, Original.Operation,
                ConditionalHolonReceiptState.Committed, null, evidence);
            var read = HolonReplicationReceiptPreflight.Compare(request, outcome,
                new HolonReplicaHeadReadResult(Target, ids.Select(id => new HolonReplicaHeadObservation(id, false, null))));
            Assert.IsFalse(read.IsError, read.Message); return read.Result;
        }
        public string Capture(IHolon[] roots, bool saveChildren = true, string source = null)
            => CommittedHolonReplicaPayload.Capture(new(Operation, source ?? Original.Write.ReplicationEnrollment.SourceId, Source.Authority, Actor,
                ConditionalHolonOperation.Save, roots, saveChildren, true, 0,
                Original.Write.ReplicationEnrollment.NativeRoutes.Count == 0 ? null : Original.Write.ReplicationEnrollment.NativeRoutes));
        public HolonReplicationOperationObligation Observation(string payload, string lease = null)
            => HolonReplicationOperationObservation.Capture(new HolonDeliveryInspection(1, Operation, TargetId,
                HolonDeliveryState.Pending, 0, null, null, null), Original.Write.ReplicationEnrollment.SourceId, Source.Authority, Actor,
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))), lease, null, 0, 0, 0, null);
        public Holon Clone()
        {
            var root = StoredRecordJson.Project<Holon>(Root);
            root.Children = Root.Children.Select(c => (IHolon)StoredRecordJson.Project<Holon>(c)).ToList(); return root;
        }
    }

    [TestMethod]
    public void OriginalSelectedGraphAndCommittedRevisionsProduceStableRedactedProof()
    {
        var f = new Fixture(); var before = f.Payload;
        var proof = HolonReplicationPayloadReceiptVerifier.Verify(f.Payload, f.Row, f.Original, f.Receipt);
        Assert.AreEqual(before, f.Payload); Assert.AreEqual(f.Row.PayloadDigest, proof.PayloadDigest);
        Assert.AreEqual(f.Receipt.OriginalRequestFingerprint, proof.OriginalRequestFingerprint);
        Assert.AreEqual(proof.ProofDigest, HolonReplicationPayloadReceiptVerifier.Verify(f.Payload, f.Row, f.Original, f.Receipt).ProofDigest);
        Assert.IsFalse(JsonSerializer.Serialize(proof).Contains("private-root-value"));
        Assert.IsFalse(JsonSerializer.Serialize(proof).Contains("private-child-value"));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public void DescriptorBearingOriginalRouteMustRetainNativePolicyAndPhysicalMeaning(bool alterPolicy)
    {
        var f = new Fixture(native: true);
        var payload = f.Payload;
        if (alterPolicy)
        {
            var changed = new HolonReplicationRouteBinding(f.Source, f.Target, "changed-native-policy");
            payload = CommittedHolonReplicaPayload.Capture(new(f.Operation, changed.SourceId, f.Source.Authority, f.Actor,
                ConditionalHolonOperation.Save, new[] { f.Root }, true, true, 0, new[] { changed }));
            Assert.ThrowsException<InvalidDataException>(() => HolonReplicationPayloadReceiptVerifier.Verify(payload, f.Observation(payload), f.Original, f.Receipt));
        }
        else
            Assert.AreEqual(f.Source.BindingFingerprint, HolonReplicationPayloadReceiptVerifier.Verify(payload, f.Row, f.Original, f.Receipt).SourceStorageFingerprint);
    }

    [DataTestMethod]
    [DataRow("content")] [DataRow("source-revision")] [DataRow("logical-version")]
    [DataRow("selection")] [DataRow("roots")] [DataRow("parent")] [DataRow("source")]
    public void ValidlyEncodedRehashedPayloadCannotChangeSourceReceiptMeaning(string change)
    {
        var f = new Fixture(); var root = f.Clone();
        if (change == "content") root.Name = "different-value";
        if (change == "source-revision") CosmicConcurrency.SetProviderRevisionId(root, f.Source.Authority, Guid.NewGuid());
        if (change == "logical-version") { root.Version = 2; root.PreviousVersionId = root.VersionId; root.VersionId = Guid.NewGuid(); }
        if (change == "parent") root.ParentHolonId = Guid.NewGuid();
        IHolon[] roots = new[] { root };
        if (change == "roots") { roots = new IHolon[] { root, root.Children.Single() }; root.Children.Clear(); }
        var payload = f.Capture(roots, change != "selection", change == "source" ? "other-source" : "lineage");
        var observed = f.Observation(payload);
        Assert.ThrowsException<InvalidDataException>(() => HolonReplicationPayloadReceiptVerifier.Verify(payload, observed, f.Original, f.Receipt));
    }

    [TestMethod]
    public void RawPayloadChecksumAndCompleteOriginalReceiptAreMandatory()
    {
        var f = new Fixture();
        Assert.ThrowsException<InvalidDataException>(() => HolonReplicationPayloadReceiptVerifier.Verify(
            f.Payload + " ", f.Row, f.Original, f.Receipt));
        Assert.ThrowsException<InvalidDataException>(() => HolonReplicationPayloadReceiptVerifier.Verify(
            f.Payload, f.Row, f.Original, f.Report(true)));
        var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(f.Operation, Guid.NewGuid(), f.Source.Authority,
            f.Original.Write.Conditions, f.Original.Write.ReplicationEnrollment), f.Original.Operation, f.Original.Roots);
        Assert.ThrowsException<InvalidDataException>(() => HolonReplicationPayloadReceiptVerifier.Verify(f.Payload, f.Row, original, f.Receipt));
    }

    [TestMethod]
    public void ExactNativeObservationMustNotChangeDuringPayloadRead()
    {
        var f = new Fixture(); HolonReplicationPayloadReceiptVerifier.ValidateObservation(f.Row, f.Row);
        Assert.ThrowsException<InvalidDataException>(() => HolonReplicationPayloadReceiptVerifier.ValidateObservation(f.Row, f.Observation(f.Payload, "changed-lease")));
    }
}
