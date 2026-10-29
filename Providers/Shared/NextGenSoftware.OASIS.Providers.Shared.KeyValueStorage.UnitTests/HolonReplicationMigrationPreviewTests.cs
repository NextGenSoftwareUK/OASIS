using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationMigrationPreviewTests
{
    public class SourceProbe : DispatchProxy
    {
        public readonly List<HolonReplicationLineageReadRequest> Requests = new();
        public Func<HolonReplicationLineageReadRequest, OASISResult<HolonReplicationLineagePage>> Read;
        protected override object Invoke(MethodInfo method, object[] args)
        {
            Assert.AreEqual("ReadLineageObligationsAsync", method.Name, "Review may not claim, mutate, initialize or replay.");
            var request = (HolonReplicationLineageReadRequest)args[0]; Requests.Add(request);
            return Task.FromResult(Read(request));
        }
    }

    internal sealed class Fixture
    {
        public readonly CosmicStorageIdentity Source = new(ProviderType.SQLLiteDBOASIS, new string('A',64), Guid.NewGuid());
        public readonly CosmicStorageIdentity Target = new(ProviderType.MongoDBOASIS, new string('B',64), Guid.NewGuid());
        public readonly IHolonReplicationLineageInventoryOutbox Native = DispatchProxy.Create<IHolonReplicationLineageInventoryOutbox, SourceProbe>();
        public SourceProbe Probe => (SourceProbe)Native;
        public readonly List<HolonReplicationOperationObligation> Rows = new();
        public readonly Dictionary<Guid, HolonReplicationReceiptPreflightReport> Receipts = new();
        public readonly Dictionary<Guid, ConditionalHolonWriteRequest> Originals = new();
        public readonly Dictionary<Guid, string> Payloads = new();
        public readonly List<Guid> ReceiptReads = new();
        public Func<HolonReplicationOperationObligation, OASISResult<HolonReplicationReceiptPreflightReport>> Receipt;
        public Func<HolonReplicationOperationObligation, OASISResult<CommittedHolonReplicaReceiptEvidence>> ReplicaReceipt;
        public HolonReplicaHeadReadConsistency ReplicaConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot;
        public HolonReplicaHeadReadConsistency SourceConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot;
        public Func<HolonReplicationOperationObligation, HolonReplicationReceiptPreflightReport, OASISResult<HolonReplicationPayloadReceiptProof>> PayloadProof;
        public CosmicStorageIdentity ReplacementTarget;
        public HolonReplicationRouteBinding Replacement => new(Source, ReplacementTarget ?? Target, "new-route-policy");

        public Fixture()
        {
            Add(1, "selected", HolonDeliveryState.Completed);
            Add(2, "other", HolonDeliveryState.Pending, Rows[0].OperationId, Rows[0].ActorId);
            Add(3, "selected", HolonDeliveryState.Pending);
            Probe.Read = request => {
                var upper = request.ThroughSequence ?? Rows.Max(r => r.Sequence);
                var eligible = Rows.Where(r => r.Sequence > request.AfterSequence && r.Sequence <= upper).ToArray();
                return new(new HolonReplicationLineagePage(request, upper, eligible.Take(request.PageSize),
                    eligible.Length > request.PageSize, request.RequiredConsistency));
            };
            Receipt = row => new(Receipts[row.OperationId]);
        }

        public void Add(long sequence, string targetId, HolonDeliveryState state, Guid? operation = null, Guid? actor = null)
        {
            var op = operation ?? Guid.NewGuid(); var owner = actor ?? Guid.NewGuid();
            if (!Receipts.ContainsKey(op)) Receipts.Add(op, Report(op, owner));
            Rows.Add(HolonReplicationOperationObservation.Capture(new HolonDeliveryInspection(sequence, op, targetId, state,
                state == HolonDeliveryState.Pending ? 0 : 1, null, null, null), "legacy-source", Source.Authority,
                owner, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Payloads[op]))), "secret-lease", "secret-worker", 10, 0, state == HolonDeliveryState.Completed ? 1 : 0, null));
        }

        public HolonReplicationReceiptPreflightReport Report(Guid operation, Guid actor, string fault = null)
        {
            var source = fault == "source-store" ? new CosmicStorageIdentity(Source.Authority, Source.EndpointFingerprint, Guid.NewGuid()) : Source;
            var target = fault == "target-store" ? new CosmicStorageIdentity(Target.Authority, Target.EndpointFingerprint, Guid.NewGuid()) : Target;
            var row = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(), Name = "private-business-value" };
            CosmicConcurrency.SetProviderRevisionId(row, Source.Authority, Guid.NewGuid());
            var records = new List<IHolon> { row };
            if (fault == "partial")
            {
                var second = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid() };
                CosmicConcurrency.SetProviderRevisionId(second, Source.Authority, Guid.NewGuid()); records.Add(second);
            }
            var request = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(
                fault == "operation" ? Guid.NewGuid() : operation, fault == "actor" ? Guid.NewGuid() : actor,
                Source.Authority, records.Select(r => new HolonWriteCondition(r.Id, null)),
                new HolonReplicationEnrollment(fault == "lineage" ? "other-lineage" : "legacy-source", new[] { "selected", "other" })),
                ConditionalHolonOperation.Save, records, saveChildren: false);
            Originals[operation] = request;
            Payloads[operation] = CommittedHolonReplicaPayload.Capture(new(request.Write.OperationId, request.Write.ReplicationEnrollment.SourceId,
                Source.Authority, request.Write.ActorId, request.Operation, records, saveChildren: false));
            var evidenceRequest = new ConditionalHolonReceiptEvidenceRequest(request, source, target, new[] { row.Id });
            var evidence = ConditionalHolonReceiptEvidenceProjection.Create(evidenceRequest, records);
            var outcome = new ConditionalHolonWriteOutcome(request.Write.OperationId, request.Write.ActorId, Source.Authority,
                request.Operation, ConditionalHolonReceiptState.Committed, null, evidence);
            var head = new HolonReplicaHeadEvidence(request.Write.ReplicationEnrollment.SourceId, Source.Authority,
                row.VersionId, row.PreviousVersionId, row.Version, Guid.NewGuid(), false, new string('D',64),
                fault == "conflict" ? new string('E',64) : evidence.Records[0].PortableContentDigest);
            var result = HolonReplicationReceiptPreflight.Compare(evidenceRequest, outcome,
                new HolonReplicaHeadReadResult(target, new[] { new HolonReplicaHeadObservation(row.Id, true, head) }));
            Assert.IsFalse(result.IsError, result.Message); return result.Result;
        }

        public Task<OASISResult<HolonReplicationMigrationPreview>> Read(int max = 10, CancellationToken token = default)
            => HolonReplicationMigrationPreviewReader.ReadAsync(Native, Source, "legacy-source", "selected", Target, Replacement,
                (row, cancellation) => { ReceiptReads.Add(row.OperationId); return Task.FromResult(Receipt(row)); },
                maxObligations: max, pageSize: 1, cancellationToken: token, sourceConsistency: SourceConsistency,
                readOriginalReplicaReceipt: ReplicaReceipt == null ? null : (row, cancellation) => Task.FromResult(ReplicaReceipt(row)),
                replicaReceiptConsistency: ReplicaConsistency,
                readPayloadReceiptProof: PayloadProof == null ? null : (row, receipt, cancellation) => Task.FromResult(PayloadProof(row, receipt)));

        public HolonReplicationPayloadReceiptProof Proof(HolonReplicationOperationObligation row, HolonReplicationReceiptPreflightReport receipt,
            HolonReplicaHeadReadConsistency consistency = HolonReplicaHeadReadConsistency.NativeSnapshot)
            => HolonReplicationPayloadReceiptVerifier.Verify(Payloads[row.OperationId], row, Originals[row.OperationId], receipt, consistency);

        public CommittedHolonReplicaReceiptEvidence Delivered(HolonReplicationOperationObligation row, string fault = null)
        {
            var target = fault == "target" ? new CosmicStorageIdentity(Target.Authority, Target.EndpointFingerprint, Guid.NewGuid()) : Target;
            var sourceAuthority = fault == "authority" ? ProviderType.Neo4jOASIS : Source.Authority;
            var transport = new CommittedHolonReplicaRequest(fault == "operation" ? Guid.NewGuid() : row.OperationId,
                fault == "lineage" ? "other-source" : row.SourceId, sourceAuthority,
                fault == "actor" ? Guid.NewGuid() : row.ActorId,
                fault == "kind" ? ConditionalHolonOperation.HardDelete : ConditionalHolonOperation.Save,
                new[] { new Holon { Id = Guid.NewGuid() } }, saveChildren: false);
            var consistency = fault == "weak" ? HolonReplicaHeadReadConsistency.ReadCommittedValidated : HolonReplicaHeadReadConsistency.NativeSnapshot;
            var request = new CommittedHolonReplicaReceiptReadRequest(transport, target, consistency);
            var intent = fault == "intent" ? new string('F',64)
                : CommittedHolonReplicaPlan.Create(CommittedHolonReplicaPayload.Read(Payloads[row.OperationId]), Target.Authority).Fingerprint;
            return new(request, intent, fault == "absent" ? ConditionalHolonReceiptState.Absent : ConditionalHolonReceiptState.Committed,
                fault == "absent" ? null : new string(fault == "changed-digest" ? 'C' : 'B',64), fault == "graph" ? 2 : 1, consistency);
        }
    }

    [TestMethod]
    public async Task CompleteSourcePayloadAndHistoricalReceiverChainIsBoundIntoReview()
    {
        var f = new Fixture(); f.ReplicaReceipt = row => new(f.Delivered(row));
        var withoutProof = await f.Read();
        f.PayloadProof = (row, receipt) => new(f.Proof(row, receipt));
        var result = await f.Read(); Assert.IsFalse(result.IsError, result.Message + result.Exception);
        Assert.AreEqual(2, result.Result.PayloadProofs.Count);
        Assert.IsFalse(result.Result.Findings.Contains(HolonReplicationMigrationFinding.OriginalPayloadReceiptBindingUnverified));
        Assert.IsFalse(result.Result.Findings.Contains(HolonReplicationMigrationFinding.OriginalReplicaReceiptsUnverified));
        Assert.IsTrue(result.Result.Findings.Contains(HolonReplicationMigrationFinding.NativeWriteAndDeliveryFencesRequired));
        Assert.IsTrue(result.Result.Findings.Contains(HolonReplicationMigrationFinding.ReplacementTargetEvidenceUnverified));
        Assert.AreNotEqual(withoutProof.Result.PreviewDigest, result.Result.PreviewDigest);
        Assert.AreEqual(result.Result.PreviewDigest, (await f.Read()).Result.PreviewDigest);
        Assert.IsFalse(JsonSerializer.Serialize(result.Result).Contains("private-business-value"));
    }

    [DataTestMethod]
    [DataRow("state")] [DataRow("target")]
    public async Task GenuineProofForAnotherObservedObligationCannotBeSubstituted(string change)
    {
        var f = new Fixture(); f.PayloadProof = (row, receipt) => {
            var alternate = HolonReplicationOperationObservation.Capture(new HolonDeliveryInspection(row.Sequence, row.OperationId,
                change == "target" ? "other" : row.TargetId, row.State, row.AttemptCount, null, null, null),
                row.SourceId, row.SourceAuthority, row.ActorId, row.PayloadDigest, "different-lease", "secret-worker", 10, 0,
                row.State == HolonDeliveryState.Completed ? 1 : 0, null);
            return new(f.Proof(alternate, receipt));
        };
        var result = await f.Read(); Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_MIGRATION_PREVIEW_PAYLOAD_PROOF_SCOPE_MISMATCH", result.ErrorCode);
    }

    [TestMethod]
    public async Task SourcePayloadAndHistoricalReceiverMustBindTheSameCanonicalIntent()
    {
        var f = new Fixture(); f.PayloadProof = (row, receipt) => new(f.Proof(row, receipt));
        f.ReplicaReceipt = row => new(f.Delivered(row, "intent"));
        var result = await f.Read(); Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_MIGRATION_PREVIEW_PAYLOAD_REPLICA_INTENT_MISMATCH", result.ErrorCode);
    }

    [TestMethod]
    public async Task WeakerSourceProofRequiresExplicitSourceReviewOptIn()
    {
        var f = new Fixture(); f.PayloadProof = (row, receipt) => new(f.Proof(row, receipt, HolonReplicaHeadReadConsistency.ReadCommittedValidated));
        var rejected = await f.Read(); Assert.IsNull(rejected.Result);
        Assert.AreEqual("COSMIC_MIGRATION_PREVIEW_PAYLOAD_PROOF_CONSISTENCY_UNSUPPORTED", rejected.ErrorCode);
        f.SourceConsistency = HolonReplicaHeadReadConsistency.ReadCommittedValidated;
        Assert.IsFalse((await f.Read()).IsError);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task MissingOrFailedPayloadProofNeverPublishesPartialReview(bool nativeError)
    {
        var f = new Fixture(); var count = 0; var cause = new InvalidOperationException("native-payload-proof-failure");
        f.PayloadProof = (row, receipt) => ++count == 1 ? new(f.Proof(row, receipt))
            : nativeError ? new() { IsError = true, ErrorCode = "NATIVE_PAYLOAD_UNVERIFIED", Exception = cause } : null;
        var result = await f.Read(); Assert.IsNull(result.Result); Assert.IsTrue(result.IsError);
        if (nativeError) { Assert.AreEqual("NATIVE_PAYLOAD_UNVERIFIED", result.ErrorCode); Assert.AreSame(cause, result.Exception); }
    }

    [TestMethod]
    public async Task CompleteWindowRetainsOtherRoutesCompletedRowsAndExactReviewBinding()
    {
        var f = new Fixture(); var result = await f.Read();
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        var preview = result.Result;
        Assert.AreEqual(3, preview.Obligations.Count); Assert.AreEqual(2, preview.Receipts.Count);
        Assert.AreEqual(3L, preview.ThroughSequence);
        Assert.IsTrue(preview.Obligations.Any(r => r.State == HolonDeliveryState.Completed));
        Assert.IsTrue(preview.Obligations.Any(r => r.TargetId == "other"));
        Assert.AreEqual(3, f.Probe.Requests.Count);
        Assert.IsNull(f.Probe.Requests[0].ThroughSequence);
        Assert.IsTrue(f.Probe.Requests.Skip(1).All(r => r.ThroughSequence == 3));
        CollectionAssert.AreEquivalent(new[] { f.Rows[0].OperationId, f.Rows[2].OperationId }, f.ReceiptReads);
        Assert.IsTrue(preview.Findings.Contains(HolonReplicationMigrationFinding.OriginalReplicaReceiptsUnverified));
        Assert.IsTrue(preview.Findings.Contains(HolonReplicationMigrationFinding.NativeWriteAndDeliveryFencesRequired));
        var json = JsonSerializer.Serialize(preview);
        Assert.IsFalse(json.Contains("private-business-value")); Assert.IsFalse(json.Contains("secret-lease"));
        var context = preview.AuthorizationContext(Guid.NewGuid(), Guid.NewGuid(), "admin");
        Assert.AreEqual(preview.PreviewDigest, context.PreviewDigest);
        Assert.AreEqual(preview.PreviewDigest, (await f.Read()).Result.PreviewDigest);
    }

    [TestMethod]
    public async Task LaterAppendsAreExcludedAndFreshReviewChangesDigest()
    {
        var f = new Fixture(); var nativeRead = f.Probe.Read;
        f.Probe.Read = request => {
            var result = nativeRead(request);
            if (!request.ThroughSequence.HasValue) f.Add(f.Rows.Max(r => r.Sequence) + 1, "selected", HolonDeliveryState.Pending);
            return result;
        };
        var first = await f.Read(); var second = await f.Read();
        Assert.IsFalse(first.IsError); Assert.IsFalse(second.IsError);
        Assert.AreEqual(3, first.Result.Obligations.Count); Assert.AreEqual(4, second.Result.Obligations.Count);
        Assert.AreNotEqual(first.Result.PreviewDigest, second.Result.PreviewDigest);
    }

    [DataTestMethod]
    [DataRow("operation")] [DataRow("actor")] [DataRow("source-store")]
    [DataRow("target-store")] [DataRow("lineage")]
    public async Task ReceiptCannotSubstituteAnotherOperationActorStoreOrLineage(string fault)
    {
        var f = new Fixture(); f.Receipt = row => new(f.Report(row.OperationId, row.ActorId, fault));
        var result = await f.Read();
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_MIGRATION_PREVIEW_RECEIPT_SCOPE_MISMATCH", result.ErrorCode);
    }

    [DataTestMethod]
    [DataRow("partial", HolonReplicationMigrationFinding.IncompleteOriginalGraph)]
    [DataRow("conflict", HolonReplicationMigrationFinding.TargetDoesNotMatchOriginalReceipt)]
    public async Task IncompleteOrConflictingEvidenceRemainsAnExplicitReviewFinding(string fault, HolonReplicationMigrationFinding finding)
    {
        var f = new Fixture(); f.Receipt = row => new(f.Report(row.OperationId, row.ActorId, fault));
        var result = await f.Read(); Assert.IsFalse(result.IsError, result.Message + result.Exception);
        Assert.IsTrue(result.Result.Findings.Contains(finding));
    }

    [DataTestMethod]
    [DataRow("limit")] [DataRow("inventory-error")] [DataRow("receipt-error")]
    [DataRow("duplicate")] [DataRow("changed-actor")] [DataRow("absent")]
    public async Task IncompleteInventoryOrUnavailableEvidenceNeverProducesPartialPreview(string fault)
    {
        var f = new Fixture(); var cause = new InvalidOperationException("native-failure");
        if (fault == "inventory-error") f.Probe.Read = _ => new() { IsError = true, ErrorCode = "NATIVE_FAILURE", Exception = cause };
        if (fault == "receipt-error") f.Receipt = _ => new() { IsError = true, ErrorCode = "NATIVE_FAILURE", Exception = cause };
        if (fault is "duplicate" or "changed-actor") f.Add(4, "selected", HolonDeliveryState.Pending,
            f.Rows[0].OperationId, fault == "duplicate" ? f.Rows[0].ActorId : Guid.NewGuid());
        if (fault == "absent") f.Rows.RemoveAll(r => r.TargetId == "selected");
        var result = await f.Read(fault == "limit" ? 2 : 10);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.IsFalse(result.IsSaved);
        if (fault.EndsWith("error", StringComparison.Ordinal))
        { Assert.AreEqual("NATIVE_FAILURE", result.ErrorCode); Assert.AreSame(cause, result.Exception); }
        if (fault != "receipt-error") Assert.AreEqual(0, f.ReceiptReads.Count);
    }

    [TestMethod]
    public async Task HistoricalReceiverProofIsBoundToReviewWithoutClosingOtherExecutionGates()
    {
        var f = new Fixture(); var unverified = await f.Read();
        f.ReplicaReceipt = row => new(f.Delivered(row));
        var verified = await f.Read(); Assert.IsFalse(verified.IsError, verified.Message);
        Assert.AreEqual(2, verified.Result.ReplicaReceipts.Count);
        Assert.IsFalse(verified.Result.Findings.Contains(HolonReplicationMigrationFinding.OriginalReplicaReceiptsUnverified));
        Assert.IsTrue(verified.Result.Findings.Contains(HolonReplicationMigrationFinding.OriginalPayloadReceiptBindingUnverified));
        Assert.IsTrue(verified.Result.Findings.Contains(HolonReplicationMigrationFinding.NativeWriteAndDeliveryFencesRequired));
        Assert.AreNotEqual(unverified.Result.PreviewDigest, verified.Result.PreviewDigest);
        f.ReplicaReceipt = row => new(f.Delivered(row, "changed-digest"));
        Assert.AreNotEqual(verified.Result.PreviewDigest, (await f.Read()).Result.PreviewDigest);
    }

    [TestMethod]
    public async Task ExplicitAbsenceRemainsAFindingEvenWhenObligationClaimsCompleted()
    {
        var f = new Fixture(); f.ReplicaReceipt = row => new(f.Delivered(row, "absent"));
        var result = await f.Read(); Assert.IsFalse(result.IsError);
        Assert.IsTrue(result.Result.Findings.Contains(HolonReplicationMigrationFinding.OriginalReplicaReceiptAbsent));
        Assert.AreEqual(2, result.Result.ReplicaReceipts.Count);
    }

    [DataTestMethod]
    [DataRow("operation")] [DataRow("actor")] [DataRow("target")] [DataRow("authority")]
    [DataRow("lineage")] [DataRow("kind")] [DataRow("graph")]
    public async Task HistoricalReceiverEvidenceCannotSubstituteAnotherScope(string fault)
    {
        var f = new Fixture(); f.ReplicaReceipt = row => new(f.Delivered(row, fault));
        var result = await f.Read(); Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_MIGRATION_PREVIEW_REPLICA_RECEIPT_SCOPE_MISMATCH", result.ErrorCode);
    }

    [TestMethod]
    public async Task HistoricalReceiverWeakConsistencyRequiresExplicitReviewOptIn()
    {
        var f = new Fixture(); f.ReplicaReceipt = row => new(f.Delivered(row, "weak"));
        var rejected = await f.Read(); Assert.IsNull(rejected.Result);
        Assert.AreEqual("COSMIC_MIGRATION_PREVIEW_REPLICA_RECEIPT_CONSISTENCY_UNSUPPORTED", rejected.ErrorCode);
        f.ReplicaConsistency = HolonReplicaHeadReadConsistency.ReadCommittedValidated;
        var admitted = await f.Read(); Assert.IsFalse(admitted.IsError);
        Assert.AreEqual(f.ReplicaConsistency, admitted.Result.RequiredReplicaReceiptConsistency);
    }

    [TestMethod]
    public async Task ReceiverReadFailurePreservesCauseAndPublishesNoPartialReview()
    {
        var f = new Fixture(); var cause = new InvalidOperationException("native-receipt-failure");
        f.ReplicaReceipt = _ => new() { IsError = true, ErrorCode = "NATIVE_RECEIPT_UNVERIFIED", Exception = cause };
        var rejected = await f.Read(); Assert.IsNull(rejected.Result); Assert.AreSame(cause, rejected.Exception);
        Assert.AreEqual("NATIVE_RECEIPT_UNVERIFIED", rejected.ErrorCode);
    }

    [TestMethod]
    public async Task ActiveDeliveryChangesPreviewAndRemainsAReviewFinding()
    {
        var f = new Fixture(); var first = await f.Read(); var prior = f.Rows[2];
        f.Rows[2] = HolonReplicationOperationObservation.Capture(new HolonDeliveryInspection(prior.Sequence,
            prior.OperationId, prior.TargetId, HolonDeliveryState.InFlight, 1, null, null, null), prior.SourceId,
            prior.SourceAuthority, prior.ActorId, prior.PayloadDigest, "new-hidden-claim", "worker", 20, 0, 0, null);
        var active = await f.Read(); Assert.IsFalse(active.IsError);
        Assert.AreNotEqual(first.Result.PreviewDigest, active.Result.PreviewDigest);
        Assert.IsTrue(active.Result.Findings.Contains(HolonReplicationMigrationFinding.DeliveryInFlight));
    }

    [DataTestMethod]
    [DataRow("window")] [DataRow("store")] [DataRow("lineage")] [DataRow("consistency")]
    [DataRow("backtrack")]
    public async Task CrossPageSubstitutionCannotProduceAReview(string fault)
    {
        var f = new Fixture(); var native = f.Probe.Read;
        f.Probe.Read = request => {
            if (request.AfterSequence == 0) return native(request);
            var source = fault == "store" ? new CosmicStorageIdentity(f.Source.Authority, f.Source.EndpointFingerprint, Guid.NewGuid()) : f.Source;
            var changed = new HolonReplicationLineageReadRequest(source, fault == "lineage" ? "other" : request.SourceId,
                request.PageSize, fault == "backtrack" ? 0 : request.AfterSequence,
                fault == "window" ? request.ThroughSequence + 1 : request.ThroughSequence,
                fault == "consistency" ? HolonReplicaHeadReadConsistency.ReadCommittedValidated : request.RequiredConsistency);
            if (fault == "lineage") return new(new HolonReplicationLineagePage(changed, changed.ThroughSequence.Value,
                Array.Empty<HolonReplicationOperationObligation>(), false, changed.RequiredConsistency));
            return native(changed);
        };
        var result = await f.Read(); Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_MIGRATION_PREVIEW_INVENTORY_SCOPE_MISMATCH", result.ErrorCode);
        Assert.AreEqual(0, f.ReceiptReads.Count);
    }

    [TestMethod]
    public async Task CancellationAfterReceiptReadCannotPublishPartialReview()
    {
        var f = new Fixture(); using var cancel = new CancellationTokenSource();
        f.Receipt = row => { cancel.Cancel(); return new(f.Receipts[row.OperationId]); };
        var result = await f.Read(token: cancel.Token);
        Assert.AreEqual("COSMIC_MIGRATION_PREVIEW_CANCELLED", result.ErrorCode);
        Assert.IsNull(result.Result); Assert.AreEqual(1, f.ReceiptReads.Count);
    }

    [TestMethod]
    public async Task CancellationStopsBeforeAnyNativeObservation()
    {
        var f = new Fixture(); using var cancel = new CancellationTokenSource(); cancel.Cancel();
        var result = await f.Read(token: cancel.Token);
        Assert.AreEqual("COSMIC_MIGRATION_PREVIEW_CANCELLED", result.ErrorCode);
        Assert.AreEqual(0, f.Probe.Requests.Count); Assert.AreEqual(0, f.ReceiptReads.Count);
    }
}
