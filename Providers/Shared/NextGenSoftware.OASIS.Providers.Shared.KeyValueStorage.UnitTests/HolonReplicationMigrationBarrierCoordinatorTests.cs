using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationMigrationBarrierCoordinatorTests
{
    internal sealed class Native : IHolonMigrationFenceStorageProvider
    {
        public HolonMigrationFenceObservation Stored;
        public int Reads, Acquires;
        public bool LoseAcknowledgement, ForeignScope, FailRead, Warn;
        public Action AfterAcquire;
        public Task<OASISResult<HolonMigrationFenceObservation>> ReadHolonMigrationFenceAsync(HolonMigrationFenceRequest r, CancellationToken c = default)
        {
            Reads++;
            if (FailRead) return Task.FromResult(new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCode = "OWNED_READ_FAILED" });
            var request = ForeignScope ? new HolonMigrationFenceRequest(r.MigrationId, Guid.NewGuid(), r.ExpectedStorage,
                r.PreviewDigest, r.AuthorizationContextDigest, r.HolonIds) : r;
            return Task.FromResult(new OASISResult<HolonMigrationFenceObservation>(Stored ?? new(request, Guid.Empty, HolonMigrationFenceState.Absent)));
        }
        public Task<OASISResult<HolonMigrationFenceObservation>> AcquireHolonMigrationFenceAsync(HolonMigrationFenceRequest r, CancellationToken c = default)
        {
            Acquires++; Stored ??= new(r, Guid.NewGuid(), HolonMigrationFenceState.Held); AfterAcquire?.Invoke();
            return Task.FromResult(LoseAcknowledgement ? new OASISResult<HolonMigrationFenceObservation> {
                IsError = true, ErrorCode = "OWNED_COMMIT_ACK_LOST", Exception = new System.IO.IOException("owned acknowledgement loss") }
                : new OASISResult<HolonMigrationFenceObservation>(Stored) { IsSaved = true, IsWarning = Warn, Exception = Warn ? new System.IO.IOException("owned cleanup warning") : null });
        }
        public Task<OASISResult<HolonMigrationFenceObservation>> CancelHolonMigrationFenceAsync(HolonMigrationFenceRequest r, Guid owner, CancellationToken c = default)
            => throw new AssertFailedException("Coordinator must never compensate or release uncertain scopes.");
    }
    internal sealed class Policy : IHolonReplicationMigrationAuthorizationPolicy
    {
        public string BindingIdentity => "owned-admin";
        public bool Deny;
        public Task<OASISResult<bool>> AuthorizeAsync(HolonReplicationMigrationAuthorizationContext context, CancellationToken token = default)
            => Task.FromResult(new OASISResult<bool>(!Deny));
    }
    internal sealed class Setup
    {
        public HolonReplicationMigrationPreview Preview;
        public HolonReplicationMigrationAuthorizationContext Context;
        public Policy Permission = new();
        public Native[] Providers = { new(), new() };
        public HolonMigrationBarrierParticipant[] Participants;
        public async Task Initialize(bool proofs = true, bool thirdStore = false)
        {
            var f = new HolonReplicationMigrationPreviewTests.Fixture();
            if (thirdStore) { f.ReplacementTarget = new(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.Neo4jOASIS, new string('E',64), Guid.NewGuid()); Providers = new[] { new Native(), new Native(), new Native() }; }
            if (proofs) { f.ReplicaReceipt = row => new(f.Delivered(row)); f.PayloadProof = (row, receipt) => new(f.Proof(row, receipt)); }
            var preview = await f.Read(); Assert.IsFalse(preview.IsError, preview.Message); Preview = preview.Result;
            Context = Preview.AuthorizationContext(Guid.NewGuid(), Guid.NewGuid(), Permission.BindingIdentity);
            Participants = new[] { Preview.SourceStorage, Preview.OriginalTargetStorage, Preview.ReplacementRoute.TargetStorage }.GroupBy(s => s.BindingFingerprint).Select(g => g.First()).OrderBy(s => s.BindingFingerprint, StringComparer.Ordinal)
                .Select((s, i) => new HolonMigrationBarrierParticipant(s, Providers[i])).ToArray();
        }
        public Task<OASISResult<HolonMigrationBarrierProgress>> Run(CancellationToken token = default)
            => HolonReplicationMigrationBarrierCoordinator.AcquireAsync(Preview, Context, Permission, Participants, token);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ResumeReadsNativeReceiptsWithoutAcquiringAgainAndRedactsOwners(bool thirdStore)
    {
        var f = new Setup(); await f.Initialize(thirdStore: thirdStore);
        var acquired = await f.Run(); Assert.IsFalse(acquired.IsError, acquired.Message + acquired.Exception);
        Assert.IsTrue(acquired.Result.AllBarriersHeld); Assert.AreEqual(thirdStore ? 3 : 2, acquired.Result.Stores.Count);
        Assert.IsTrue(f.Providers.All(p => p.Reads == 1 && p.Acquires == 1));
        foreach (var native in f.Providers)
        {
            CollectionAssert.AreEquivalent(f.Preview.Receipts.SelectMany(r => r.Records).Select(r => r.Original.HolonId).Distinct().ToArray(), native.Stored.Request.HolonIds.ToArray());
            Assert.AreEqual(f.Context.ContextDigest, native.Stored.Request.AuthorizationContextDigest);
            Assert.IsFalse(JsonSerializer.Serialize(acquired.Result).Contains(native.Stored.OwnerToken.ToString()));
        }
        var resumed = await f.Run(); Assert.IsFalse(resumed.IsError); Assert.IsTrue(resumed.Result.AllBarriersHeld);
        Assert.IsTrue(f.Providers.All(p => p.Reads == 2 && p.Acquires == 1));
        Assert.IsFalse(resumed.IsSaved);
    }

    [TestMethod]
    public async Task LostCommitAcknowledgementRemainsUnknownUntilExplicitNativeReadRecovery()
    {
        var f = new Setup(); await f.Initialize(); f.Providers[1].LoseAcknowledgement = true;
        var failed = await f.Run(); Assert.IsTrue(failed.IsError); Assert.AreEqual("OWNED_COMMIT_ACK_LOST", failed.ErrorCode);
        Assert.IsInstanceOfType<System.IO.IOException>(failed.Exception);
        Assert.AreEqual(HolonMigrationBarrierObservationState.Held, failed.Result.Stores[0].State);
        Assert.AreEqual(HolonMigrationBarrierObservationState.Unknown, failed.Result.Stores[1].State);
        Assert.IsFalse(failed.Result.AllBarriersHeld);
        var recovered = await f.Run(); Assert.IsFalse(recovered.IsError); Assert.IsTrue(recovered.Result.AllBarriersHeld);
        Assert.IsTrue(f.Providers.All(p => p.Acquires == 1));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task FailedOrForeignObservationPreventsEveryAcquire(bool foreign)
    {
        var f = new Setup(); await f.Initialize(); f.Providers[1].FailRead = !foreign; f.Providers[1].ForeignScope = foreign;
        var failed = await f.Run(); Assert.IsTrue(failed.IsError); Assert.IsFalse(failed.Result.AllBarriersHeld);
        Assert.IsTrue(f.Providers.All(p => p.Acquires == 0));
        Assert.AreEqual(HolonMigrationBarrierObservationState.Unknown, failed.Result.Stores[1].State);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task RevocationOrCancellationAfterFirstCommitPreservesPartialProgress(bool cancel)
    {
        var f = new Setup(); await f.Initialize(); using var cancellation = new CancellationTokenSource();
        f.Providers[0].AfterAcquire = () => { if (cancel) cancellation.Cancel(); else f.Permission.Deny = true; };
        var failed = await f.Run(cancellation.Token); Assert.IsTrue(failed.IsError);
        Assert.AreEqual(HolonMigrationBarrierObservationState.Held, failed.Result.Stores[0].State);
        Assert.AreEqual(HolonMigrationBarrierObservationState.Absent, failed.Result.Stores[1].State);
        Assert.AreEqual(1, f.Providers[0].Acquires); Assert.AreEqual(0, f.Providers[1].Acquires);
    }

    [TestMethod]
    public async Task UnprovedPreviewAndMissingParticipantAndWrongContextDoNotTouchNativeStores()
    {
        var f = new Setup(); await f.Initialize(false);
        Assert.IsTrue((await f.Run()).IsError); Assert.IsTrue(f.Providers.All(p => p.Reads == 0));
        await f.Initialize(); f.Participants = f.Participants.Take(1).ToArray();
        Assert.IsTrue((await f.Run()).IsError); Assert.IsTrue(f.Providers.All(p => p.Reads == 0));
        f.Context = new(f.Context.MigrationId, f.Context.OperatorId, f.Context.SourceStorage, f.Context.OriginalTargetStorage,
            f.Context.OriginalSourceId, "different-route", f.Context.ReplacementRoute, f.Context.PreviewDigest, f.Context.AuthorizationPolicyBinding);
        Assert.AreEqual("COSMIC_MIGRATION_BARRIER_SCOPE_MISMATCH", (await f.Run()).ErrorCode);
        Assert.IsTrue(f.Providers.All(p => p.Reads == 0));
    }

    [TestMethod]
    public async Task NativeCleanupWarningPreservesAcknowledgedStateAndOriginalCause()
    {
        var f = new Setup(); await f.Initialize(); f.Providers[0].Warn = true;
        var result = await f.Run(); Assert.IsFalse(result.IsError); Assert.IsTrue(result.IsWarning);
        Assert.IsTrue(result.Result.AllBarriersHeld); Assert.AreEqual(1, result.WarningCount);
        Assert.IsInstanceOfType<System.IO.IOException>(result.Exception);
    }

    [TestMethod]
    public async Task CancelledNativeIdentityCannotAcquireAdditionalParticipants()
    {
        var f = new Setup(); await f.Initialize(); var initial = await f.Run(); Assert.IsFalse(initial.IsError);
        var prior = f.Providers[0].Stored;
        f.Providers[0].Stored = new(prior.Request, prior.OwnerToken, HolonMigrationFenceState.Cancelled);
        f.Providers[1].Stored = null;
        var failed = await f.Run(); Assert.IsTrue(failed.IsError); Assert.AreEqual("COSMIC_MIGRATION_BARRIER_TERMINAL", failed.ErrorCode);
        Assert.AreEqual(1, f.Providers[1].Acquires);
    }
}
