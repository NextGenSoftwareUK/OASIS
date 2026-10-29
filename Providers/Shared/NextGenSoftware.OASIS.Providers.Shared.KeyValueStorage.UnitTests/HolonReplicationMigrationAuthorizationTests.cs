using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonReplicationMigrationAuthorizationTests
{
    private readonly Guid migration = Guid.NewGuid(), actor = Guid.NewGuid();
    private readonly CosmicStorageIdentity source = new(ProviderType.MongoDBOASIS, new string('A', 64), Guid.NewGuid());
    private readonly CosmicStorageIdentity target = new(ProviderType.SQLLiteDBOASIS, new string('B', 64), Guid.NewGuid());
    private readonly DateTimeOffset start = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private HolonReplicationMigrationAuthorizationContext Context(string change = null)
    {
        var s = change == "source" ? new CosmicStorageIdentity(source.Authority, source.EndpointFingerprint, Guid.NewGuid()) : source;
        var oldTarget = change == "old-store" ? new CosmicStorageIdentity(target.Authority, target.EndpointFingerprint, Guid.NewGuid()) : target;
        var newTarget = change == "new-store" ? new CosmicStorageIdentity(target.Authority, target.EndpointFingerprint, Guid.NewGuid()) : target;
        return new(change == "migration" ? Guid.NewGuid() : migration, change == "operator" ? Guid.NewGuid() : actor,
            s, oldTarget, change == "old-source" ? "other-source" : "legacy-source",
            change == "old-target" ? "other-target" : "legacy-target",
            new(s, newTarget, change == "route-policy" ? "other-policy" : "replication-policy"),
            new string(change == "preview" ? 'D' : 'C', 64), change == "admin-policy" ? "other-admin" : "migration-admin-v1");
    }

    [DataTestMethod]
    [DataRow("migration")] [DataRow("operator")] [DataRow("source")] [DataRow("old-store")]
    [DataRow("new-store")] [DataRow("old-source")] [DataRow("old-target")]
    [DataRow("route-policy")] [DataRow("preview")] [DataRow("admin-policy")]
    public async Task ExactGrantCannotAuthorizeAnotherContext(string change)
    {
        var original = Context(); var other = Context(change);
        var policy = new ScopedHolonReplicationMigrationPolicy("migration-admin-v1",
            new[] { new HolonReplicationMigrationGrant(original, start, start.AddMinutes(5)) }, new Clock { Now = start });
        Assert.IsTrue((await policy.AuthorizeAsync(Context())).Result, "Equivalent immutable context must match.");
        var rejected = await policy.AuthorizeAsync(other);
        Assert.IsTrue(rejected.IsError); Assert.IsFalse(rejected.Result);
        Assert.AreEqual("COSMIC_MIGRATION_AUTHORIZATION_DENIED", rejected.ErrorCode);
        Assert.AreNotEqual(original.ContextDigest, other.ContextDigest);
    }

    [TestMethod]
    public async Task EmptyHostPolicyDeniesEvenAValidAuthenticatedContext()
    {
        var policy = new ScopedHolonReplicationMigrationPolicy("migration-admin-v1", Array.Empty<HolonReplicationMigrationGrant>());
        Assert.IsTrue((await policy.AuthorizeAsync(Context())).IsError);
        Assert.IsTrue((await policy.AuthorizeAsync(null)).IsError);
    }

    [TestMethod]
    public async Task GrantIntervalIsInclusiveAtStartAndExclusiveAtExpiryAndHostListIsCopied()
    {
        var clock = new Clock { Now = start.AddTicks(-1) }; var context = Context();
        var grants = new List<HolonReplicationMigrationGrant> { new(context, start, start.AddMinutes(5)) };
        var policy = new ScopedHolonReplicationMigrationPolicy("migration-admin-v1", grants, clock); grants.Clear();
        Assert.IsTrue((await policy.AuthorizeAsync(context)).IsError);
        clock.Now = start; Assert.IsTrue((await policy.AuthorizeAsync(context)).Result);
        clock.Now = start.AddMinutes(5); Assert.IsTrue((await policy.AuthorizeAsync(context)).IsError);
        clock.Now = start.AddMinutes(1);
        var revoked = new ScopedHolonReplicationMigrationPolicy("migration-admin-v1", grants, clock);
        Assert.IsTrue((await revoked.AuthorizeAsync(context)).IsError);
    }

    [TestMethod]
    public async Task CancellationCannotAdmitAnOtherwiseValidGrant()
    {
        var c = Context(); var policy = new ScopedHolonReplicationMigrationPolicy("migration-admin-v1",
            new[] { new HolonReplicationMigrationGrant(c, start, start.AddMinutes(1)) }, new Clock { Now = start });
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () => await policy.AuthorizeAsync(c, cancel.Token));
    }

    [TestMethod]
    public void HostConfigurationRejectsWrongPolicyDuplicatesAndEmptyIntervals()
    {
        var c = Context(); var g = new HolonReplicationMigrationGrant(c, start, start.AddMinutes(1));
        Assert.ThrowsException<ArgumentException>(() => new ScopedHolonReplicationMigrationPolicy("different", new[] { g }));
        Assert.ThrowsException<ArgumentException>(() => new ScopedHolonReplicationMigrationPolicy("migration-admin-v1", new[] { g, g }));
        Assert.ThrowsException<ArgumentException>(() => new HolonReplicationMigrationGrant(c, start, start));
        Assert.ThrowsException<ArgumentException>(() => new HolonReplicationMigrationAuthorizationContext(migration, actor,
            source, target, "old", "target", new(source, target, "route"), "not-a-digest", "admin"));
        Assert.ThrowsException<ArgumentException>(() => new HolonReplicationMigrationAuthorizationContext(migration, actor,
            source, target, "old", "target", new(new(source.Authority, source.EndpointFingerprint, Guid.NewGuid()), target, "route"), new string('A',64), "admin"));
    }
}
