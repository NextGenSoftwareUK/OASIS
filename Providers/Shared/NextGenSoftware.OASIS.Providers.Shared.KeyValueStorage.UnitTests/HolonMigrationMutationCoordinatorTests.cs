using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
[DoNotParallelize]
public class HolonMigrationMutationCoordinatorTests
{
    private sealed class Native : IHolonMigrationMutationStorageProvider
    {
        internal HolonMigrationFenceObservation Stored;
        internal int Reads, Starts;
        internal bool LoseAcknowledgement;
        internal Action AfterStart;
        public Task<OASISResult<HolonMigrationFenceObservation>> ReadHolonMigrationFenceAsync(HolonMigrationFenceRequest r, CancellationToken c = default)
        { Reads++; return Task.FromResult(new OASISResult<HolonMigrationFenceObservation>(Stored)); }
        public Task<OASISResult<HolonMigrationFenceObservation>> BeginHolonMigrationMutationAsync(HolonMigrationMutationStartRequest r, CancellationToken c = default)
        {
            Starts++; Assert.AreEqual(Stored.OwnerToken, r.OwnerToken);
            Stored = new(r.Fence, r.OwnerToken, HolonMigrationFenceState.MutationStarted, r.MutationDigest);
            AfterStart?.Invoke();
            return Task.FromResult(LoseAcknowledgement
                ? new OASISResult<HolonMigrationFenceObservation> { IsError = true, ErrorCode = "ACK_LOST", Exception = new IOException("ack lost") }
                : new OASISResult<HolonMigrationFenceObservation>(Stored) { IsSaved = true });
        }
        public Task<OASISResult<HolonMigrationFenceObservation>> AcquireHolonMigrationFenceAsync(HolonMigrationFenceRequest r, CancellationToken c = default)
            => throw new AssertFailedException("Mutation start cannot acquire missing barriers.");
        public Task<OASISResult<HolonMigrationFenceObservation>> CancelHolonMigrationFenceAsync(HolonMigrationFenceRequest r, Guid owner, CancellationToken c = default)
            => throw new AssertFailedException("Mutation start cannot compensate.");
    }
    private sealed class Permission : IHolonMigrationMutationAuthorizationPolicy
    {
        public string BindingIdentity => "mutation-admin";
        internal bool Deny;
        public Task<OASISResult<bool>> AuthorizeAsync(HolonMigrationMutationPreparation p, CancellationToken c = default)
            => Task.FromResult(new OASISResult<bool>(!Deny));
    }
    private sealed class Setup
    {
        internal string Folder;
        internal HolonReplicationMigrationBarrierPlan Plan;
        internal HolonMigrationMutationPreparation Preparation;
        internal FileHolonMigrationMutationPreparationStore Store;
        internal Native[] Natives;
        internal HolonMigrationBarrierParticipant[] Participants;
        internal Permission Permission = new();
        internal async Task Initialize()
        {
            (Plan, Preparation) = await HolonMigrationMutationAuthorizationTests.PrepareWithPlan();
            Folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../..", "TestResults/MutationPreparationCurrent/coordinator-fixture"));
            if (Directory.Exists(Folder)) Assert.AreEqual(0, Directory.GetFileSystemEntries(Folder).Length, "Preserve failed evidence.");
            Store = new(Folder);
            var saved = await Store.SaveAsync(Preparation, Plan, Preparation.Fingerprint);
            Assert.IsFalse(saved.IsError, saved.Exception?.ToString());
            Natives = Plan.Scopes.Select(s => new Native { Stored = new(s, Guid.NewGuid(), HolonMigrationFenceState.Held) }).ToArray();
            Participants = Plan.Scopes.Select((s,i) => new HolonMigrationBarrierParticipant(s.ExpectedStorage, Natives[i])).ToArray();
        }
        internal Task<OASISResult<HolonMigrationMutationProgress>> Run()
            => HolonMigrationMutationCoordinator.StartPersistedAsync(Plan, Preparation.Fingerprint, Store,
                new HolonReplicationMigrationBarrierCoordinatorTests.Policy(), Permission, Participants);
        internal void Clean()
        { foreach (var path in Directory.GetFiles(Folder)) File.Delete(path); Assert.AreEqual(0, Directory.GetFileSystemEntries(Folder).Length); }
    }

    [TestMethod]
    public async Task StartsExactPersistedIntentAndRecoveryDoesNotRepeatStartedParticipants()
    {
        var f = new Setup(); await f.Initialize(); var first = await f.Run();
        Assert.IsFalse(first.IsError, first.Exception?.ToString()); Assert.IsTrue(first.Result.AllParticipantsStarted);
        Assert.AreEqual(f.Natives.Length, first.SavedCount);
        var resumed = await f.Run(); Assert.IsFalse(resumed.IsError); Assert.IsFalse(resumed.IsSaved);
        Assert.IsTrue(f.Natives.All(n => n.Starts == 1 && n.Reads == 2));
        var json = System.Text.Json.JsonSerializer.Serialize(resumed.Result);
        foreach (var n in f.Natives) Assert.IsFalse(json.Contains(n.Stored.OwnerToken.ToString()));
        f.Clean();
    }

    [TestMethod]
    public async Task LostAcknowledgementPreservesUnknownAndFreshInvocationReadsBeforeContinuing()
    {
        var f = new Setup(); await f.Initialize(); f.Natives[0].LoseAcknowledgement = true;
        var failed = await f.Run(); Assert.AreEqual("ACK_LOST", failed.ErrorCode);
        Assert.AreEqual(HolonMigrationBarrierObservationState.Unknown, failed.Result.Barriers.Stores[0].State);
        Assert.AreEqual(0, f.Natives[1].Starts);
        var resumed = await f.Run(); Assert.IsFalse(resumed.IsError); Assert.IsTrue(resumed.Result.AllParticipantsStarted);
        Assert.IsTrue(f.Natives.All(n => n.Starts == 1 && n.Reads == 2)); f.Clean();
    }

    [TestMethod]
    public async Task MissingLastCapabilityPreventsAllNativeCalls()
    {
        var f = new Setup(); await f.Initialize();
        f.Participants[^1] = f.Participants[^1] with { Provider = new HolonReplicationMigrationBarrierCoordinatorTests.Native() };
        var rejected = await f.Run(); Assert.AreEqual("COSMIC_MIGRATION_MUTATION_CAPABILITY_REQUIRED", rejected.ErrorCode);
        Assert.IsTrue(f.Natives.All(n => n.Reads == 0 && n.Starts == 0)); f.Clean();
    }

    [TestMethod]
    public async Task DifferentExistingIntentPreventsAnyNewStart()
    {
        var f = new Setup(); await f.Initialize(); var last = f.Natives[^1];
        last.Stored = new(last.Stored.Request, last.Stored.OwnerToken, HolonMigrationFenceState.MutationStarted, new string('A',64));
        var rejected = await f.Run(); Assert.AreEqual("COSMIC_MIGRATION_MUTATION_SCOPE_OR_INTENT_MISMATCH", rejected.ErrorCode);
        Assert.IsTrue(f.Natives.All(n => n.Reads == 1 && n.Starts == 0)); f.Clean();
    }

    [TestMethod]
    public async Task RevocationAfterFirstStartPreservesPartialProgressWithoutCompensation()
    {
        var f = new Setup(); await f.Initialize(); f.Natives[0].AfterStart = () => f.Permission.Deny = true;
        var rejected = await f.Run(); Assert.IsTrue(rejected.IsError); Assert.IsTrue(rejected.IsSaved);
        Assert.AreEqual(HolonMigrationBarrierObservationState.MutationStarted, rejected.Result.Barriers.Stores[0].State);
        Assert.AreEqual(HolonMigrationBarrierObservationState.Held, rejected.Result.Barriers.Stores[1].State);
        Assert.AreEqual(0, f.Natives[1].Starts); f.Clean();
    }
}
