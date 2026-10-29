using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonMigrationMutationAuthorizationTests
{
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = DateTimeOffset.Parse("2026-10-29T00:00:00Z");
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static async Task<HolonMigrationMutationPreparation> Prepare()
    {
        var f = new HolonReplicationMigrationBarrierCoordinatorTests.Setup(); await f.Initialize();
        var plan = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        var delivery = new HolonMigrationDeliveryReconciliationReport(plan, 3,
            HolonReplicaHeadReadConsistency.NativeSnapshot, Array.Empty<HolonMigrationDeliveryObservation>());
        var replacement = new HolonMigrationReplacementReport(plan, HolonReplicaHeadReadConsistency.NativeSnapshot,
            new[] { HolonReplicaHeadReadConsistency.NativeSnapshot },
            f.Preview.Receipts.SelectMany(r => r.Records).Select(r => r.Original).DistinctBy(r => r.HolonId)
                .OrderBy(r => r.HolonId.ToString("D"), StringComparer.Ordinal)
                .Select(r => new HolonMigrationReplacementRecord(r, false, null, HolonMigrationReplacementKind.EmptyAtObservation)));
        return new(plan, delivery, replacement, "mutation-admin");
    }

    [DataTestMethod]
    [DataRow(-1, false)] [DataRow(0, true)] [DataRow(59, true)] [DataRow(60, false)]
    public async Task ApprovalUsesInclusiveStartAndExclusiveExpiry(int seconds, bool allowed)
    {
        var preparation = await Prepare(); var clock = new Clock(); var start = clock.Now;
        var policy = new ScopedHolonMigrationMutationPolicy("mutation-admin",
            new[] { new HolonMigrationMutationGrant(preparation, start, start.AddMinutes(1)) }, clock);
        clock.Now = start.AddSeconds(seconds);
        var result = await policy.AuthorizeAsync(preparation);
        Assert.AreEqual(allowed, result.Result); Assert.AreEqual(!allowed, result.IsError);
    }

    [TestMethod]
    public async Task DifferentPreparationAndRevokedPolicyDenyWithoutExtendingApproval()
    {
        var preparation = await Prepare(); var other = await Prepare(); var clock = new Clock();
        var policy = new ScopedHolonMigrationMutationPolicy("mutation-admin",
            new[] { new HolonMigrationMutationGrant(preparation, clock.Now, clock.Now.AddMinutes(1)) }, clock);
        Assert.IsTrue((await policy.AuthorizeAsync(preparation)).Result);
        Assert.IsTrue((await policy.AuthorizeAsync(other)).IsError);
        var revoked = new ScopedHolonMigrationMutationPolicy("mutation-admin", Array.Empty<HolonMigrationMutationGrant>(), clock);
        Assert.AreEqual("COSMIC_MIGRATION_MUTATION_AUTHORIZATION_DENIED", (await revoked.AuthorizeAsync(preparation)).ErrorCode);
        Assert.IsTrue((await revoked.AuthorizeAsync(null)).IsError);
    }

    [TestMethod]
    public async Task WrongBindingDuplicatesAndUnboundedAllowlistAreRejected()
    {
        var preparation = await Prepare(); var clock = new Clock();
        var grant = new HolonMigrationMutationGrant(preparation, clock.Now, clock.Now.AddMinutes(1));
        Assert.ThrowsException<ArgumentException>(() => new ScopedHolonMigrationMutationPolicy("other-admin", new[] { grant }));
        Assert.ThrowsException<ArgumentException>(() => new ScopedHolonMigrationMutationPolicy("mutation-admin", new[] { grant, grant }));
        Assert.ThrowsException<ArgumentException>(() => new ScopedHolonMigrationMutationPolicy("mutation-admin", Enumerable.Repeat(grant, 257)));
        Assert.ThrowsException<ArgumentException>(() => new HolonMigrationMutationGrant(preparation, clock.Now, clock.Now));
    }

    [TestMethod]
    public async Task CancellationIsNotConvertedIntoDenialOrSuccess()
    {
        var preparation = await Prepare();
        var policy = new ScopedHolonMigrationMutationPolicy("mutation-admin", Array.Empty<HolonMigrationMutationGrant>());
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => policy.AuthorizeAsync(preparation, cancellation.Token));
    }
}
