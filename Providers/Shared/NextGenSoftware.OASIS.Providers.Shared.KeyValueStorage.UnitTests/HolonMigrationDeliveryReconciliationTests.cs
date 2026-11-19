using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonMigrationDeliveryReconciliationTests
{
    public interface ReadOnlyNativeSource : IHolonReplicationLineageInventoryOutbox, IHolonReplicationPayloadAuditOutbox { }
    public class Inventory : DispatchProxy
    {
        internal HolonReplicationMigrationPreviewTests.Fixture Evidence;
        internal Action AfterPayload;
        internal bool FailPayload;
        internal int LineageReads;
        internal Exception Cause = new IOException("owned payload read failure");
        protected override object Invoke(MethodInfo method, object[] args)
        {
            if (method.Name == "ReadFrozenPayloadAsync") return ReadFrozenPayloadAsync((CosmicStorageIdentity)args[0],
                (HolonReplicationOperationObligation)args[1], (HolonReplicaHeadReadConsistency)args[2], (CancellationToken)args[3]);
            Assert.AreEqual("ReadLineageObligationsAsync", method.Name, "Reconciliation may not claim, finish, renew or clear obligations.");
            LineageReads++;
            return Task.FromResult(Evidence.Probe.Read((HolonReplicationLineageReadRequest)args[0]));
        }
        public Task<OASISResult<string>> ReadFrozenPayloadAsync(CosmicStorageIdentity expectedSource,
            HolonReplicationOperationObligation row, HolonReplicaHeadReadConsistency consistency = HolonReplicaHeadReadConsistency.NativeSnapshot,
            CancellationToken cancellationToken = default)
        {
            AfterPayload?.Invoke();
            return Task.FromResult(FailPayload ? new OASISResult<string> { IsError = true, ErrorCode = "OWNED_FROZEN_READ_FAILED", Exception = Cause }
                : new OASISResult<string>(Evidence.Payloads[row.OperationId]));
        }
    }
    private sealed class Setup
    {
        internal HolonReplicationMigrationBarrierCoordinatorTests.Setup Fixture;
        internal IHolonReplicationLineageInventoryOutbox Source;
        internal Inventory Probe => (Inventory)Source;
        internal HolonReplicationMigrationBarrierPlan Plan;
        internal async Task Initialize(HolonDeliveryState? state = null)
        {
            Fixture = new(); await Fixture.Initialize();
            if (state.HasValue)
            {
                var old = Fixture.Evidence.Rows[0];
                Fixture.Evidence.Rows[0] = State(old, state.Value);
                Fixture.Preview = (await Fixture.Evidence.Read()).Result;
                Fixture.Context = Fixture.Preview.AuthorizationContext(Fixture.Context.MigrationId, Fixture.Context.OperatorId, Fixture.Permission.BindingIdentity);
            }
            Plan = HolonReplicationMigrationBarrierPlan.Create(Fixture.Preview, Fixture.Context);
            Assert.IsFalse((await Fixture.Run()).IsError);
            foreach (var native in Fixture.Providers)
            {
                native.Acquires = 0;
                native.Receipt = r => new(Fixture.Evidence.Delivered(Fixture.Evidence.Rows.First(row => row.OperationId == r.Original.OperationId)));
            }
            Source = DispatchProxy.Create<ReadOnlyNativeSource, Inventory>(); Probe.Evidence = Fixture.Evidence;
        }
        internal Task<OASISResult<HolonMigrationDeliveryReconciliationReport>> Read(int max = 4096)
            => HolonMigrationDeliveryReconciliation.ReadAsync(Plan, Fixture.Permission, Fixture.Participants, Source, max, 1);
    }
    private static HolonReplicationOperationObligation State(HolonReplicationOperationObligation row, HolonDeliveryState state)
        => HolonReplicationOperationObservation.Capture(new(row.Sequence, row.OperationId, row.TargetId, state, 1, null, null, null),
            row.SourceId, row.SourceAuthority, row.ActorId, row.PayloadDigest, "private-lease", "private-worker", 10, 0,
            state == HolonDeliveryState.Completed ? 1 : 0, null);

    [DataTestMethod]
    [DataRow(HolonDeliveryState.InFlight)] [DataRow(HolonDeliveryState.ReconciliationRequired)]
    public async Task HistoricalAcknowledgementDoesNotFinishOrClearOutstandingSourceState(HolonDeliveryState state)
    {
        var f = new Setup(); await f.Initialize(state);
        var result = await f.Read(); Assert.IsFalse(result.IsError, result.Message + result.Exception);
        var first = result.Result.Obligations.Single(r => r.Sequence == 1);
        Assert.AreEqual(state, first.SourceState); Assert.AreEqual(HolonMigrationDeliveryObservationKind.OriginalReceiverCommitted, first.Kind);
        Assert.IsNotNull(first.ReplicaAcknowledgementDigest); Assert.IsFalse(result.IsSaved);
        Assert.AreEqual(state, f.Fixture.Evidence.Rows[0].State); Assert.IsTrue(f.Fixture.Providers.All(p => p.Acquires == 0));
        var json = JsonSerializer.Serialize(result.Result);
        Assert.IsFalse(json.Contains("private-lease")); Assert.IsFalse(json.Contains("private-worker")); Assert.IsFalse(json.Contains("private-business-value"));
        Assert.AreEqual(result.Result.ObservationDigest, (await f.Read()).Result.ObservationDigest);
        Assert.AreEqual(HolonMigrationDeliveryObservationKind.OtherAffectedRoute, result.Result.Obligations.Single(r => r.Sequence == 2).Kind);
    }

    [TestMethod]
    public async Task ChangedLeaseStateChangesReviewDigestWithoutBecomingMutationPermission()
    {
        var f = new Setup(); await f.Initialize(); var before = await f.Read(); Assert.IsFalse(before.IsError);
        f.Fixture.Evidence.Rows[0] = State(f.Fixture.Evidence.Rows[0], HolonDeliveryState.InFlight);
        var after = await f.Read(); Assert.IsFalse(after.IsError, after.Message + after.Exception);
        Assert.IsTrue(after.Result.Obligations.Single(r => r.Sequence == 1).ChangedSinceReview);
        Assert.AreNotEqual(before.Result.ObservationDigest, after.Result.ObservationDigest); Assert.IsFalse(after.IsSaved);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task LaterObligationsAreInspectedForActualProtectedGraphMembership(bool affected)
    {
        var f = new Setup(); await f.Initialize();
        var original = CommittedHolonReplicaPayload.Read(f.Fixture.Evidence.Payloads[f.Fixture.Evidence.Rows[0].OperationId]);
        var roots = original.Roots.ToArray();
        if (!affected) roots[0].Id = Guid.NewGuid();
        var operation = Guid.NewGuid();
        var transport = new CommittedHolonReplicaRequest(operation, original.SourceId, original.SourceAuthority,
            original.ActorId, original.Operation, roots, saveChildren: false);
        var payload = CommittedHolonReplicaPayload.Capture(transport);
        f.Fixture.Evidence.Payloads[operation] = payload;
        f.Fixture.Evidence.Rows.Add(HolonReplicationOperationObservation.Capture(new(4, operation, "selected", HolonDeliveryState.Pending, 0, null, null, null),
            original.SourceId, original.SourceAuthority, original.ActorId, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload))), "lease", "worker", 0, 0, 0, null));
        var reviewed = await f.Read(); Assert.IsFalse(reviewed.IsError, reviewed.Message + reviewed.Exception);
        Assert.AreEqual(4L, reviewed.Result.ThroughSequence);
        Assert.AreEqual(affected ? HolonMigrationDeliveryObservationKind.AdditionalAffectedObligation : HolonMigrationDeliveryObservationKind.UnrelatedGraph,
            reviewed.Result.Obligations.Single(r => r.Sequence == 4).Kind);
    }

    [DataTestMethod]
    [DataRow("barrier")] [DataRow("missing")] [DataRow("payload")] [DataRow("limit")]
    public async Task MissingOrChangingProofReturnsNoPartialReport(string failure)
    {
        var f = new Setup(); await f.Initialize();
        if (failure == "barrier") f.Probe.AfterPayload = () => { var prior = f.Fixture.Providers[0].Stored;
            f.Fixture.Providers[0].Stored = new(prior.Request, prior.OwnerToken, HolonMigrationFenceState.Cancelled); };
        if (failure == "missing") f.Fixture.Evidence.Rows.RemoveAt(0);
        if (failure == "payload") f.Probe.FailPayload = true;
        var failed = await f.Read(failure == "limit" ? 2 : 4096);
        Assert.IsTrue(failed.IsError); Assert.IsNull(failed.Result); Assert.IsTrue(f.Fixture.Providers.All(p => p.Acquires == 0));
        if (failure == "payload") { Assert.AreEqual("OWNED_FROZEN_READ_FAILED", failed.ErrorCode); Assert.AreSame(f.Probe.Cause, failed.Exception); }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task CurrentApprovalIsRequiredBeforeAndAfterNativeEvidence(bool revokeLater)
    {
        var f = new Setup(); await f.Initialize();
        if (revokeLater) f.Probe.AfterPayload = () => f.Fixture.Permission.Deny = true;
        else f.Fixture.Permission.Deny = true;
        var failed = await f.Read(); Assert.IsTrue(failed.IsError); Assert.IsNull(failed.Result);
        Assert.AreEqual("COSMIC_MIGRATION_AUTHORIZATION_DENIED", failed.ErrorCode);
        Assert.IsTrue(f.Fixture.Providers.All(p => p.Acquires == 0));
        if (!revokeLater) Assert.AreEqual(0, f.Probe.LineageReads);
    }

    [TestMethod]
    public async Task KnownNativeCleanupWarningPreservesValidEvidenceAndOriginalCause()
    {
        var f = new Setup(); await f.Initialize(); var cause = new IOException("owned receipt cleanup warning"); var first = true;
        foreach (var native in f.Fixture.Providers) native.Receipt = r => {
            var warn = first; first = false;
            return new(f.Fixture.Evidence.Delivered(f.Fixture.Evidence.Rows.First(row => row.OperationId == r.Original.OperationId)))
                { IsWarning = warn, WarningCount = warn ? 1 : 0, Exception = warn ? cause : null };
        };
        var observed = await f.Read(); Assert.IsFalse(observed.IsError); Assert.IsNotNull(observed.Result);
        Assert.IsTrue(observed.IsWarning); Assert.AreEqual(1, observed.WarningCount); Assert.AreSame(cause, observed.Exception);
    }

    [TestMethod]
    public async Task ReceiverFailurePreservesNativeCauseAndDoesNotIssueAReplay()
    {
        var f = new Setup(); await f.Initialize(); var cause = new IOException("owned native receipt failure");
        foreach (var native in f.Fixture.Providers) native.Receipt = _ => new() { IsError = true, ErrorCode = "OWNED_RECEIPT_FAILED", Exception = cause };
        var failed = await f.Read(); Assert.IsTrue(failed.IsError); Assert.IsNull(failed.Result);
        Assert.AreEqual("OWNED_RECEIPT_FAILED", failed.ErrorCode); Assert.AreSame(cause, failed.Exception);
    }
}
