using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class HolonReplicationMigrationAuditTests
{
    private sealed class Source : IBoundHolonReplicationOutboxSource
    {
        internal int Opens;
        internal CosmicStorageIdentity Expected;
        internal readonly Store Store = new();
        internal OASISResult<IHolonReplicationRouteInventoryOutbox> Failure;
        public OASISResult<IHolonReplicationRouteInventoryOutbox> GetBoundHolonReplicationOutbox(CosmicStorageIdentity expected)
        { Opens++; Expected = expected; return Failure ?? new(Store); }
        public OASISResult<IHolonReplicationReadinessOutbox> GetHolonReplicationOutbox()
            => throw new InvalidOperationException("An audit cannot downgrade to an unbound source.");
    }
    private sealed class Store : IHolonReplicationRouteInventoryOutbox
    {
        internal int Reads;
        internal int Size;
        internal string After;
        internal OASISResult<HolonReplicationRoutePage> Page = new(new(Array.Empty<HolonDeliveryInspection>(), false));
        public Task<OASISResult<HolonReplicationRoutePage>> ReadRouteHeadsAsync(int pageSize = 64, string afterTargetId = null, CancellationToken cancellationToken = default)
        { cancellationToken.ThrowIfCancellationRequested(); Reads++; Size = pageSize; After = afterTargetId; return Task.FromResult(Page); }
        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId, string workerId, TimeSpan leaseDuration, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No claim during audit.");
        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim, TimeSpan leaseDuration, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No renewal during audit.");
        public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome, string errorCode = null, string message = null, DateTime? nextAttemptUtc = null, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No finish during audit.");
        public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome, HolonDeliveryRecoveryEvidence recoveryEvidence, string errorCode = null, string message = null, DateTime? nextAttemptUtc = null, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No recovery mutation during audit.");
        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Use one bounded native observation.");
        public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string targetId, CancellationToken cancellationToken = default) => throw new InvalidOperationException("No repeated per-target reads.");
    }
    private static CosmicStorageIdentity Identity() => new(ProviderType.SQLLiteDBOASIS, new string('A', 64), Guid.NewGuid());
    private static HolonDeliveryInspection Head(string target = "legacy-target", HolonDeliveryState state = HolonDeliveryState.Pending,
        int attempts = 0, HolonReplicationRouteBinding route = null)
        => new(1, Guid.NewGuid(), target, state, attempts, "original-code", "original-message", null, route);

    [DataTestMethod]
    [DataRow(HolonDeliveryState.Pending, 0)]
    [DataRow(HolonDeliveryState.InFlight, 2)]
    [DataRow(HolonDeliveryState.ReconciliationRequired, 10)]
    public async Task LegacyHeadsRemainUnboundAndUnchangedWithoutAnyMutation(HolonDeliveryState state, int attempts)
    {
        var source = new Source(); var identity = Identity(); var head = Head(state: state, attempts: attempts);
        source.Store.Page = new(new(new[] { head }, false));
        var result = await HolonReplicationMigrationAudit.ReadPageAsync(source, identity);
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreSame(identity, source.Expected);
        Assert.AreEqual(1, source.Opens); Assert.AreEqual(1, source.Store.Reads);
        var observed = result.Result.Heads[0];
        Assert.AreEqual(state, observed.State); Assert.AreEqual(attempts, observed.AttemptCount);
        Assert.AreEqual(head.OperationId, observed.OperationId);
        Assert.IsTrue(observed.RequiresNativeBindingProof);
        Assert.IsNull(observed.TargetStorageFingerprint);
        Assert.AreEqual("original-code", observed.ErrorCode);
        Assert.AreEqual(64, result.Result.ObservationDigest.Length);
        Assert.AreEqual(state, head.State); Assert.AreEqual(attempts, head.AttemptCount);
    }
    [TestMethod]
    public async Task BoundDescriptorMustMatchTheExplicitPhysicalSource()
    {
        var source = new Source(); var identity = Identity();
        var route = new HolonReplicationRouteBinding(Identity(), new(ProviderType.MongoDBOASIS, new string('B', 64), Guid.NewGuid()), "policy");
        source.Store.Page = new(new(new[] { Head(route.TargetId, route: route) }, false));
        var result = await HolonReplicationMigrationAudit.ReadPageAsync(source, identity);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_MIGRATION_AUDIT_SOURCE_CHANGED", result.ErrorCode);
    }
    [TestMethod]
    public async Task CursorAndObservationDigestBindOnePageWithoutHiddenEnumeration()
    {
        var source = new Source(); var identity = Identity(); var head = Head("target-b");
        source.Store.Page = new(new(new[] { head }, true));
        var first = await HolonReplicationMigrationAudit.ReadPageAsync(source, identity, 1, "target-a");
        var same = await HolonReplicationMigrationAudit.ReadPageAsync(source, identity, 1, "target-a");
        Assert.IsFalse(first.IsError, first.Message);
        Assert.AreEqual(1, source.Store.Size); Assert.AreEqual("target-a", source.Store.After);
        Assert.AreEqual("target-b", first.Result.NextAfterTargetId);
        Assert.AreEqual(first.Result.ObservationDigest, same.Result.ObservationDigest);
        Assert.AreEqual(2, source.Store.Reads);
        source.Store.Page = new(new(new[] { new HolonDeliveryInspection(head.Sequence, head.OperationId, head.TargetId,
            HolonDeliveryState.InFlight, 1, head.ErrorCode, head.Message, null) }, true));
        var changed = await HolonReplicationMigrationAudit.ReadPageAsync(source, identity, 1, "target-a");
        Assert.AreNotEqual(first.Result.ObservationDigest, changed.Result.ObservationDigest);
    }
    [DataTestMethod]
    [DataRow(true)] [DataRow(false)]
    public async Task NativeFailuresRetainOriginalCodeAndCauseWithoutPartialSuccess(bool atOpen)
    {
        var source = new Source(); var cause = new IOException("native source unavailable");
        if (atOpen) source.Failure = new() { IsError = true, ErrorCode = "NATIVE_FAILURE", Exception = cause, Message = "original" };
        else source.Store.Page = new() { IsError = true, ErrorCode = "NATIVE_FAILURE", Exception = cause, Message = "original" };
        var result = await HolonReplicationMigrationAudit.ReadPageAsync(source, Identity());
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual("NATIVE_FAILURE", result.ErrorCode); Assert.AreSame(cause, result.Exception);
        Assert.AreEqual("original", result.Message); Assert.AreEqual(atOpen ? 0 : 1, source.Store.Reads);
    }
    [DataTestMethod]
    [DataRow(0)] [DataRow(257)]
    public async Task InvalidBoundsRejectBeforeAnySourceAccess(int size)
    {
        var source = new Source();
        var result = await HolonReplicationMigrationAudit.ReadPageAsync(source, Identity(), size);
        Assert.IsTrue(result.IsError); Assert.AreEqual(0, source.Opens);
    }
    [TestMethod]
    public async Task ProviderCannotReturnAnOutOfOrderOrOversizedPage()
    {
        var source = new Source();
        source.Store.Page = new(new(new[] { Head("target-c"), Head("target-b") }, false));
        var result = await HolonReplicationMigrationAudit.ReadPageAsync(source, Identity());
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_MIGRATION_AUDIT_INVALID_PAGE", result.ErrorCode);
        result = await HolonReplicationMigrationAudit.ReadPageAsync(source, Identity(), 1);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
    }
    [TestMethod]
    public async Task CancellationDoesNotOpenOrInitializeTheSource()
    {
        var source = new Source(); using var canceled = new CancellationTokenSource(); canceled.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => HolonReplicationMigrationAudit.ReadPageAsync(source, Identity(), cancellationToken: canceled.Token));
        Assert.AreEqual(0, source.Opens);
    }
}
