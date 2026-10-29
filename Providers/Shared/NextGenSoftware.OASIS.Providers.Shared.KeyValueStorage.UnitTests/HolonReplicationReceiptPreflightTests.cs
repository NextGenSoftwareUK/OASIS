using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationReceiptPreflightTests
{
    private sealed class Fixture
    {
        public Holon Row = new() { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(), Name = "private-preflight-value" };
        public ConditionalHolonReceiptEvidenceRequest Request;
        public ConditionalHolonWriteOutcome Outcome;
        public HolonReplicaHeadEvidence Head;
        public Fixture()
        {
            CosmicConcurrency.SetProviderRevisionId(Row, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            var source = new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.NewGuid());
            var target = new CosmicStorageIdentity(ProviderType.Neo4jOASIS, new string('B', 64), Guid.NewGuid());
            var enrollment = new HolonReplicationEnrollment("original-source", new[] { "target" });
            var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(), Guid.NewGuid(),
                source.Authority, new[] { new HolonWriteCondition(Row.Id, null) }, enrollment), ConditionalHolonOperation.Save,
                new[] { Row }, saveChildren: false);
            Request = new(original, source, target, new[] { Row.Id });
            var evidence = ConditionalHolonReceiptEvidenceProjection.Create(Request, new[] { Row });
            Outcome = new(original.Write.OperationId, original.Write.ActorId, original.Write.Authority, original.Operation,
                ConditionalHolonReceiptState.Committed, null, evidence);
            Head = new(enrollment.SourceId, source.Authority, Row.VersionId, Row.PreviousVersionId, Row.Version,
                Guid.NewGuid(), false, new string('C', 64), evidence.Records[0].PortableContentDigest);
        }
        public HolonReplicaHeadReadResult Target(HolonReplicaHeadObservation observation = null,
            HolonReplicaHeadReadConsistency consistency = HolonReplicaHeadReadConsistency.NativeSnapshot)
            => new(Request.ComparisonTarget, new[] { observation ?? new HolonReplicaHeadObservation(Row.Id, true, Head) }, consistency);
    }

    [DataTestMethod]
    [DataRow(HolonReplicationReceiptComparisonKind.MatchesOriginalReceipt)]
    [DataRow(HolonReplicationReceiptComparisonKind.MissingReplicaHead)]
    [DataRow(HolonReplicationReceiptComparisonKind.UntrackedCurrentRecord)]
    [DataRow(HolonReplicationReceiptComparisonKind.SourceLineageConflict)]
    [DataRow(HolonReplicationReceiptComparisonKind.LogicalVersionConflict)]
    [DataRow(HolonReplicationReceiptComparisonKind.DeletionConflict)]
    [DataRow(HolonReplicationReceiptComparisonKind.ContentConflict)]
    [DataRow(HolonReplicationReceiptComparisonKind.PortableContentEvidenceMissing)]
    public void ExactObservationsHaveExplicitNonAuthorizingClassifications(HolonReplicationReceiptComparisonKind kind)
    {
        var f = new Fixture(); var head = f.Head;
        var missing = kind is HolonReplicationReceiptComparisonKind.MissingReplicaHead or HolonReplicationReceiptComparisonKind.UntrackedCurrentRecord;
        var deleted = kind == HolonReplicationReceiptComparisonKind.DeletionConflict;
        var changed = new HolonReplicaHeadEvidence(kind == HolonReplicationReceiptComparisonKind.SourceLineageConflict ? "other-source" : head.SourceId,
            head.SourceAuthority, kind == HolonReplicationReceiptComparisonKind.LogicalVersionConflict ? Guid.NewGuid() : head.VersionId,
            head.PreviousVersionId, head.Version, head.ProviderRevisionId, deleted, head.SnapshotDigest,
            kind == HolonReplicationReceiptComparisonKind.PortableContentEvidenceMissing ? null
                : kind == HolonReplicationReceiptComparisonKind.ContentConflict ? new string('D', 64) : head.PortableContentDigest);
        var observation = new HolonReplicaHeadObservation(f.Row.Id, !deleted && kind != HolonReplicationReceiptComparisonKind.MissingReplicaHead, missing ? null : changed);
        var result = HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, f.Target(observation));
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        Assert.AreEqual(kind, result.Result.Records[0].Kind);
        Assert.IsTrue(result.Result.IsCompleteOriginalGraph);
        Assert.IsFalse(result.IsSaved); Assert.IsFalse(result.IsDeleted);
        Assert.IsFalse(System.Text.Json.JsonSerializer.Serialize(result.Result).Contains(f.Row.Name, StringComparison.Ordinal));
    }

    [TestMethod]
    public void DigestBindsActualTargetRevisionAndRequiredConsistency()
    {
        var f = new Fixture();
        var first = HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, f.Target()).Result;
        Assert.AreEqual(first.ObservationDigest, HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, f.Target()).Result.ObservationDigest);
        var head = f.Head;
        f.Head = new(head.SourceId, head.SourceAuthority, head.VersionId, head.PreviousVersionId, head.Version,
            Guid.NewGuid(), head.HardDeleted, head.SnapshotDigest, head.PortableContentDigest);
        Assert.AreNotEqual(first.ObservationDigest, HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, f.Target()).Result.ObservationDigest);
        var weakRequirement = HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, f.Target(), HolonReplicaHeadReadConsistency.ReadCommittedValidated);
        Assert.AreNotEqual(weakRequirement.Result.ObservationDigest, HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, f.Target()).Result.ObservationDigest);
    }

    [TestMethod]
    public void WeakerTargetObservationRequiresExplicitOptIn()
    {
        var f = new Fixture(); var target = f.Target(consistency: HolonReplicaHeadReadConsistency.ReadCommittedValidated);
        Assert.AreEqual("COSMIC_MIGRATION_PREFLIGHT_CONSISTENCY_UNSUPPORTED", HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, target).ErrorCode);
        var accepted = HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, target, HolonReplicaHeadReadConsistency.ReadCommittedValidated);
        Assert.IsFalse(accepted.IsError); Assert.AreEqual(target.Consistency, accepted.Result.TargetConsistency);
    }

    [TestMethod]
    public void ChangedInputOrReplicationSourceCannotReuseEarlierEvidence()
    {
        var f = new Fixture(); f.Row.Name = "changed-after-observation";
        Assert.AreEqual("COSMIC_MIGRATION_PREFLIGHT_SOURCE_SCOPE_MISMATCH", HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, f.Target()).ErrorCode);
        f = new Fixture(); var original = f.Request.Original;
        var changed = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(original.Write.OperationId, original.Write.ActorId,
            original.Write.Authority, original.Write.Conditions, new HolonReplicationEnrollment("other-lineage", new[] { "target" })),
            original.Operation, original.Roots, saveChildren: false);
        var request = new ConditionalHolonReceiptEvidenceRequest(changed, f.Request.ExpectedSource, f.Request.ComparisonTarget, f.Request.HolonIds);
        Assert.AreEqual("COSMIC_MIGRATION_PREFLIGHT_SOURCE_SCOPE_MISMATCH", HolonReplicationReceiptPreflight.Compare(request, f.Outcome, f.Target()).ErrorCode);
    }

    [DataTestMethod]
    [DataRow(true)] [DataRow(false)]
    public void WrongTargetStoreOrIdentitySetCannotProducePartialReport(bool wrongStore)
    {
        var f = new Fixture();
        var target = new HolonReplicaHeadReadResult(wrongStore ? new CosmicStorageIdentity(f.Request.ComparisonTarget.Authority,
            f.Request.ComparisonTarget.EndpointFingerprint, Guid.NewGuid()) : f.Request.ComparisonTarget,
            new[] { new HolonReplicaHeadObservation(wrongStore ? f.Row.Id : Guid.NewGuid(), true, f.Head) });
        var result = HolonReplicationReceiptPreflight.Compare(f.Request, f.Outcome, target);
        Assert.AreEqual("COSMIC_MIGRATION_PREFLIGHT_TARGET_SCOPE_MISMATCH", result.ErrorCode); Assert.IsNull(result.Result);
    }

    [TestMethod]
    public void OldEvidenceWithoutOriginalInputFingerprintIsInsufficient()
    {
        var f = new Fixture();
        var old = new ConditionalHolonReceiptEvidence(f.Request, 1, f.Outcome.ReceiptEvidence.Records);
        var outcome = new ConditionalHolonWriteOutcome(f.Outcome.OperationId, f.Outcome.ActorId, f.Outcome.Authority,
            f.Outcome.Operation, ConditionalHolonReceiptState.Committed, null, old);
        Assert.AreEqual("COSMIC_MIGRATION_PREFLIGHT_SOURCE_SCOPE_MISMATCH",
            HolonReplicationReceiptPreflight.Compare(f.Request, outcome, f.Target()).ErrorCode);
    }

    [TestMethod]
    public void SelectedSubsetReportKeepsWholeOperationScopeVisible()
    {
        var f = new Fixture(); var second = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid() };
        CosmicConcurrency.SetProviderRevisionId(second, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        var write = f.Request.Original.Write;
        var original = new ConditionalHolonWriteRequest(new ConditionalHolonWrite(write.OperationId, write.ActorId, write.Authority,
            new[] { write.Conditions[0], new HolonWriteCondition(second.Id, null) }, write.ReplicationEnrollment),
            ConditionalHolonOperation.Save, new[] { f.Row, second }, saveChildren: false);
        var request = new ConditionalHolonReceiptEvidenceRequest(original, f.Request.ExpectedSource,
            f.Request.ComparisonTarget, new[] { f.Row.Id });
        var evidence = ConditionalHolonReceiptEvidenceProjection.Create(request, new[] { f.Row, second });
        var outcome = new ConditionalHolonWriteOutcome(write.OperationId, write.ActorId, write.Authority,
            original.Operation, ConditionalHolonReceiptState.Committed, null, evidence);
        var result = HolonReplicationReceiptPreflight.Compare(request, outcome, f.Target());
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        Assert.IsFalse(result.Result.IsCompleteOriginalGraph);
        Assert.AreEqual(2, result.Result.OriginalSelectedRecordCount);
        Assert.AreEqual(1, result.Result.Records.Count);
    }

    public class SourceProbe : DispatchProxy
    {
        public int Reads;
        public Func<Task<OASISResult<ConditionalHolonWriteOutcome>>> Read;
        protected override object Invoke(MethodInfo method, object[] args)
        {
            Assert.AreEqual("ReadConditionalHolonReceiptEvidenceAsync", method.Name, "Preflight may not claim, capture, prepare or write.");
            Reads++; return Read();
        }
    }
    private sealed class TargetProbe : IHolonReplicaHeadAuditStorageProvider
    {
        public int Reads;
        public OASISResult<HolonReplicaHeadReadResult> Outcome;
        public Task<OASISResult<HolonReplicaHeadReadResult>> ReadHolonReplicaHeadsAsync(HolonReplicaHeadReadRequest request, CancellationToken token = default)
        { Reads++; return Task.FromResult(Outcome); }
    }

    [DataTestMethod]
    [DataRow("absent")] [DataRow("source-error")] [DataRow("target-error")] [DataRow("cancelled")] [DataRow("matched")]
    public async Task ReadOrchestrationNeverWritesAndStopsWhenRequiredEvidenceIsUnavailable(string mode)
    {
        var f = new Fixture(); var cause = new InvalidOperationException("native-test-cause");
        var source = DispatchProxy.Create<IConditionalHolonReceiptEvidenceStorageProvider, SourceProbe>();
        var probe = (SourceProbe)source;
        probe.Read = () => Task.FromResult(mode == "source-error"
            ? new OASISResult<ConditionalHolonWriteOutcome> { IsError = true, ErrorCode = "NATIVE_SOURCE_REJECTED", Exception = cause }
            : new OASISResult<ConditionalHolonWriteOutcome>(mode == "absent"
                ? new(f.Outcome.OperationId, f.Outcome.ActorId, f.Outcome.Authority, f.Outcome.Operation, ConditionalHolonReceiptState.Absent)
                : f.Outcome) { IsWarning = true, WarningCount = 1 });
        var target = new TargetProbe { Outcome = mode == "target-error"
            ? new() { IsError = true, ErrorCode = "NATIVE_TARGET_REJECTED", Exception = cause }
            : new(f.Target()) };
        using var cancelled = new CancellationTokenSource(); if (mode == "cancelled") cancelled.Cancel();
        var result = await HolonReplicationReceiptPreflight.ReadAsync(source, target, f.Request, cancellationToken: cancelled.Token);
        Assert.AreEqual(mode != "matched", result.IsError);
        Assert.AreEqual(mode == "cancelled" ? 0 : 1, probe.Reads);
        Assert.AreEqual(mode is "target-error" or "matched" ? 1 : 0, target.Reads);
        Assert.IsFalse(result.IsSaved);
        if (mode.EndsWith("-error", StringComparison.Ordinal)) Assert.AreSame(cause, result.Exception);
        if (mode is "matched" or "target-error") Assert.AreEqual(1, result.WarningCount);
        if (mode != "matched") Assert.IsNull(result.Result);
    }
}
