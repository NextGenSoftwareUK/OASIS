using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonMigrationReplacementReviewTests
{
    private sealed class Setup
    {
        internal HolonReplicationMigrationBarrierCoordinatorTests.Setup Fixture;
        internal HolonReplicationMigrationBarrierPlan Plan;
        internal ConditionalHolonReceiptRecordEvidence[] Expected;
        internal HolonMigrationReplacementKind Kind = HolonMigrationReplacementKind.MatchingOriginalLineage;
        internal Func<HolonReplicaHeadReadRequest, OASISResult<HolonReplicaHeadReadResult>> ReadHeads;
        internal async Task Initialize()
        {
            Fixture = new(); await Fixture.Initialize(); Assert.IsFalse((await Fixture.Run()).IsError);
            Plan = HolonReplicationMigrationBarrierPlan.Create(Fixture.Preview, Fixture.Context);
            Expected = Fixture.Preview.Receipts.SelectMany(r => r.Records).Select(r => r.Original).DistinctBy(r => r.HolonId).ToArray();
            ReadHeads = request => new(new(request.ExpectedStorage, request.HolonIds.Select(Observe)));
            foreach (var native in Fixture.Providers) { native.Acquires = 0; native.Heads = request => ReadHeads(request); }
        }
        internal HolonReplicaHeadObservation Observe(Guid id)
        {
            var expected = Expected.Single(r => r.HolonId == id);
            if (Kind == HolonMigrationReplacementKind.EmptyAtObservation) return new(id, false, null);
            if (Kind == HolonMigrationReplacementKind.UntrackedLocalRecord) return new(id, true, null);
            var foreign = Kind == HolonMigrationReplacementKind.ForeignLineage;
            var versionConflict = Kind == HolonMigrationReplacementKind.LogicalVersionConflict;
            var deleted = Kind == HolonMigrationReplacementKind.DeletionConflict;
            var lineage = foreign ? "foreign-source" : Kind == HolonMigrationReplacementKind.MatchingReplacementLineage
                ? Fixture.Context.ReplacementRoute.SourceId : Fixture.Context.OriginalSourceId;
            var head = new HolonReplicaHeadEvidence(lineage, Fixture.Context.SourceStorage.Authority,
                versionConflict ? Guid.NewGuid() : expected.VersionId, versionConflict ? expected.VersionId : expected.PreviousVersionId,
                versionConflict ? expected.Version + 1 : expected.Version, Guid.NewGuid(), deleted, new string('D', 64),
                Kind == HolonMigrationReplacementKind.PortableContentUnverified ? null : Kind == HolonMigrationReplacementKind.ContentConflict
                    ? new string('E', 64) : expected.PortableContentDigest);
            return new(id, !deleted, head);
        }
        internal Task<OASISResult<HolonMigrationReplacementReport>> Read(int pageSize = 1,
            HolonReplicaHeadReadConsistency consistency = HolonReplicaHeadReadConsistency.NativeSnapshot)
            => HolonMigrationReplacementReview.ReadAsync(Plan, Fixture.Permission, Fixture.Participants, consistency, pageSize);
    }

    [DataTestMethod]
    [DataRow(HolonMigrationReplacementKind.EmptyAtObservation)]
    [DataRow(HolonMigrationReplacementKind.MatchingOriginalLineage)]
    [DataRow(HolonMigrationReplacementKind.MatchingReplacementLineage)]
    [DataRow(HolonMigrationReplacementKind.UntrackedLocalRecord)]
    [DataRow(HolonMigrationReplacementKind.ForeignLineage)]
    [DataRow(HolonMigrationReplacementKind.LogicalVersionConflict)]
    [DataRow(HolonMigrationReplacementKind.DeletionConflict)]
    [DataRow(HolonMigrationReplacementKind.PortableContentUnverified)]
    [DataRow(HolonMigrationReplacementKind.ContentConflict)]
    public async Task EveryReplacementConditionIsReportedWithoutAdoptionOrWrite(HolonMigrationReplacementKind kind)
    {
        var f = new Setup(); await f.Initialize(); f.Kind = kind;
        var result = await f.Read(); Assert.IsFalse(result.IsError, result.Message + result.Exception);
        Assert.AreEqual(f.Expected.Length, result.Result.Records.Count); Assert.IsTrue(result.Result.Records.All(r => r.Kind == kind));
        Assert.IsFalse(result.IsSaved); Assert.AreEqual(f.Expected.Length, result.Result.PageConsistencies.Count);
        Assert.IsTrue(f.Fixture.Providers.All(p => p.Acquires == 0));
        var json = JsonSerializer.Serialize(result.Result);
        Assert.IsFalse(json.Contains("private-business-value"));
        foreach (var native in f.Fixture.Providers) Assert.IsFalse(json.Contains(native.Stored.OwnerToken.ToString()));
    }

    [DataTestMethod]
    [DataRow("store")] [DataRow("order")] [DataRow("missing")] [DataRow("extra")] [DataRow("consistency")]
    public async Task WrongNativeScopeOrIncompletePageCannotProduceAPartialReport(string fault)
    {
        var f = new Setup(); await f.Initialize();
        f.ReadHeads = request => {
            var rows = request.HolonIds.Select(f.Observe).ToArray();
            if (fault == "order") rows = rows.Reverse().ToArray();
            if (fault == "missing") rows = rows.Take(1).ToArray();
            if (fault == "extra") rows = rows.Append(new HolonReplicaHeadObservation(Guid.NewGuid(), false, null)).ToArray();
            var store = fault == "store" ? new CosmicStorageIdentity(request.ExpectedStorage.Authority, request.ExpectedStorage.EndpointFingerprint, Guid.NewGuid()) : request.ExpectedStorage;
            return new(new(store, rows, fault == "consistency" ? HolonReplicaHeadReadConsistency.ReadCommittedValidated : request.RequiredConsistency));
        };
        var result = await f.Read(64); Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.IsTrue(f.Fixture.Providers.All(p => p.Acquires == 0));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task BarrierLossOrApprovalRevocationDuringReadPreventsAReport(bool revoke)
    {
        var f = new Setup(); await f.Initialize();
        f.ReadHeads = request => {
            if (revoke) f.Fixture.Permission.Deny = true;
            else { var old = f.Fixture.Providers[0].Stored; f.Fixture.Providers[0].Stored = new(old.Request, old.OwnerToken, HolonMigrationFenceState.Cancelled); }
            return new(new(request.ExpectedStorage, request.HolonIds.Select(f.Observe)));
        };
        var result = await f.Read(); Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.IsTrue(f.Fixture.Providers.All(p => p.Acquires == 0));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeFailureOrKnownCleanupWarningPreservesItsCause(bool warning)
    {
        var f = new Setup(); await f.Initialize(); var cause = new IOException("owned replacement evidence failure"); var first = true;
        f.ReadHeads = request => {
            if (!warning) return new() { IsError = true, ErrorCode = "OWNED_REPLACEMENT_READ_FAILED", Exception = cause };
            var warn = first; first = false;
            return new(new(request.ExpectedStorage, request.HolonIds.Select(f.Observe))) { IsWarning = warn, WarningCount = warn ? 1 : 0, Exception = warn ? cause : null };
        };
        var result = await f.Read(); Assert.AreEqual(!warning, result.IsError); Assert.AreSame(cause, result.Exception);
        if (warning) { Assert.IsTrue(result.IsWarning); Assert.AreEqual(1, result.WarningCount); Assert.IsNotNull(result.Result); }
        else { Assert.AreEqual("OWNED_REPLACEMENT_READ_FAILED", result.ErrorCode); Assert.IsNull(result.Result); }
    }

    [TestMethod]
    public async Task ExplicitWeakerNativeReadRemainsVisibleInTheReview()
    {
        var f = new Setup(); await f.Initialize();
        f.ReadHeads = request => new(new(request.ExpectedStorage, request.HolonIds.Select(f.Observe), request.RequiredConsistency));
        var result = await f.Read(consistency: HolonReplicaHeadReadConsistency.ReadCommittedValidated);
        Assert.IsFalse(result.IsError); Assert.AreEqual(HolonReplicaHeadReadConsistency.ReadCommittedValidated, result.Result.RequiredConsistency);
        Assert.IsTrue(result.Result.PageConsistencies.All(c => c == HolonReplicaHeadReadConsistency.ReadCommittedValidated));
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task EvidenceCanReadStableStartedPhaseButNotAnIntentChangingDuringTheRead(bool change)
    {
        var f = new Setup(); await f.Initialize(); var prior = f.Fixture.Providers[0].Stored;
        f.Fixture.Providers[0].Stored = new(prior.Request, prior.OwnerToken, HolonMigrationFenceState.MutationStarted, new string('D',64));
        if (change) f.ReadHeads = request => {
            f.Fixture.Providers[0].Stored = new(prior.Request, prior.OwnerToken, HolonMigrationFenceState.MutationStarted, new string('E',64));
            return new(new(request.ExpectedStorage, request.HolonIds.Select(f.Observe)));
        };
        var read = await f.Read(); Assert.AreEqual(change, read.IsError);
        if (change) Assert.IsNull(read.Result); else Assert.IsNotNull(read.Result);
        Assert.IsFalse(read.IsSaved); Assert.IsTrue(f.Fixture.Providers.All(p => p.Acquires == 0));
    }

}
