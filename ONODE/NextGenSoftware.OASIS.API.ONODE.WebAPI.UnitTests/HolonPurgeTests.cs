using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Purge;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HolonPurgeTests
{
    // ---- fakes: no database, no providers --------------------------------------------------------------------------------

    private sealed class FakeStore : IHolonPurgeStore
    {
        public readonly Dictionary<Guid, HolonPurgeEntry> Entries = new Dictionary<Guid, HolonPurgeEntry>();
        public readonly List<HolonPurgeAuditRecord> Audit = new List<HolonPurgeAuditRecord>();
        public readonly Dictionary<Guid, PurgeChoice> Avatars = new Dictionary<Guid, PurgeChoice>();
        public readonly List<int> DueLimits = new List<int>();
        public bool FailAudit;
        public Task<HolonPurgeEntry> GetAsync(Guid id, CancellationToken c) => Task.FromResult(Entries.TryGetValue(id, out var e) ? e : null);
        public Task SchedulePendingAsync(HolonPurgeEntry e, CancellationToken c)
        {
            if (Entries.TryGetValue(e.HolonId, out var old)) { e.Hold = old.Hold; e.HoldReason = old.HoldReason; }
            Entries[e.HolonId] = e; return Task.CompletedTask;
        }
        public Task CancelAsync(Guid id, string reason, CancellationToken c)
        { if (Entries.TryGetValue(id, out var e) && e.Status == "pending") { e.Status = "cancelled"; e.CancelReason = reason; } return Task.CompletedTask; }
        public Task SetHoldAsync(Guid id, Guid owner, bool hold, string reason, Guid by, DateTime now, CancellationToken c)
        {
            if (!Entries.TryGetValue(id, out var e)) Entries[id] = e = new HolonPurgeEntry { HolonId = id, OwnerAvatarId = owner };
            e.Hold = hold; e.HoldReason = reason; e.HoldByAvatarId = by; return Task.CompletedTask;
        }
        public Task<IReadOnlyList<HolonPurgeEntry>> ListDueAsync(DateTime now, int limit, CancellationToken c)
        {
            DueLimits.Add(limit);
            return Task.FromResult<IReadOnlyList<HolonPurgeEntry>>(Entries.Values.Where(e => e.Status == "pending" && e.PurgeAfterUtc <= now).OrderBy(e => e.PurgeAfterUtc).Take(limit).ToList());
        }
        public Task MarkPurgedAsync(Guid id, Guid owner, DateTime at, int removed, CancellationToken c)
        {
            if (!Entries.TryGetValue(id, out var e)) Entries[id] = e = new HolonPurgeEntry { HolonId = id };
            e.Status = "purged"; e.OwnerAvatarId = owner; e.PurgedAtUtc = at; e.VersionsRemoved = removed; return Task.CompletedTask;
        }
        public Task WriteAuditAsync(HolonPurgeAuditRecord r, CancellationToken c)
        { if (FailAudit) throw new InvalidOperationException("audit store down"); Audit.Add(r); return Task.CompletedTask; }
        public Task<IReadOnlyDictionary<Guid, PurgeChoice>> ListAvatarChoicesAsync(CancellationToken c) => Task.FromResult<IReadOnlyDictionary<Guid, PurgeChoice>>(Avatars);
        public Task<PurgeChoice> GetAvatarChoiceAsync(Guid id, CancellationToken c) => Task.FromResult(Avatars.TryGetValue(id, out var v) ? v : new PurgeChoice());
        public Task SetAvatarChoiceAsync(Guid id, PurgeChoice v, CancellationToken c) { if (v != null && !v.IsEmpty) Avatars[id] = v; else Avatars.Remove(id); return Task.CompletedTask; }
    }

    private sealed class FakeBackend : IHolonPurgeBackend
    {
        // holon id -> (current record, number of saved versions). Removing the key = purged.
        public readonly Dictionary<Guid, (Holon Holon, int Versions)> Primary = new Dictionary<Guid, (Holon, int)>();
        public readonly HashSet<(Guid Holon, Guid Avatar)> DeleteGrants = new HashSet<(Guid, Guid)>();
        public bool ReplicaFails;
        public int PurgeCalls;
        public Task<HolonPurgeSubject> GetSubjectAsync(Guid id, CancellationToken c)
        {
            if (!Primary.TryGetValue(id, out var row)) return Task.FromResult(new HolonPurgeSubject { Id = id, Exists = false });
            return Task.FromResult(new HolonPurgeSubject { Id = id, Exists = true, Holon = row.Holon, CreatedByAvatarId = row.Holon.CreatedByAvatarId,
                IsDeleted = row.Holon.DeletedDate != DateTime.MinValue, DeletedDate = row.Holon.DeletedDate, VersionCount = row.Versions });
        }
        public Task<bool> CanDeleteAsync(HolonPurgeSubject s, Guid avatar, CancellationToken c)
            => Task.FromResult(s.CreatedByAvatarId == avatar || DeleteGrants.Contains((s.Id, avatar)));
        public Task<IReadOnlyList<HolonPurgeStoreOutcome>> PurgeAsync(Guid id, CancellationToken c)
        {
            PurgeCalls++;
            var list = new List<HolonPurgeStoreOutcome>();
            if (Primary.TryGetValue(id, out var row)) { Primary.Remove(id); list.Add(new HolonPurgeStoreOutcome { Store = "MongoDBOASIS", State = HolonPurgeStoreState.Purged, VersionsRemoved = row.Versions }); }
            else list.Add(new HolonPurgeStoreOutcome { Store = "MongoDBOASIS", State = HolonPurgeStoreState.NothingToPurge });
            if (ReplicaFails) list.Add(new HolonPurgeStoreOutcome { Store = "SQLLiteDBOASIS", State = HolonPurgeStoreState.NotPurged, Message = "cannot purge" });
            return Task.FromResult<IReadOnlyList<HolonPurgeStoreOutcome>>(list);
        }
    }

    private sealed class Rig
    {
        public DateTime Now = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        public VersionPurgeSettings Settings = new VersionPurgeSettings { Enabled = true, ApplyToAll = true, PurgeAfterDays = 30, BatchSize = 25 };
        public FakeStore Store = new FakeStore();
        public FakeBackend Backend = new FakeBackend();
        public HolonPurgePolicy Policy;
        public HolonPurgeService Service;
        public readonly Guid Owner = Guid.NewGuid();
        public Rig() { Policy = new HolonPurgePolicy(() => Settings); Service = new HolonPurgeService(Policy, Store, Backend, () => Now); }
        public Holon Add(int versions, bool deleted, Guid? owner = null)
        {
            var h = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = owner ?? Owner, DeletedDate = deleted ? Now.AddDays(-1) : DateTime.MinValue };
            Backend.Primary[h.Id] = (h, versions); return h;
        }
        public HolonPurgeCaller Caller(Guid? avatar = null) => new HolonPurgeCaller { AvatarId = avatar ?? Owner, AppId = "piper" };
        public Task<HolonPurgeResult> Purge(Guid id, Guid? avatar = null, bool live = false) => Service.PurgeAsync(id, Caller(avatar), live, CancellationToken.None);
    }

    // ---- defaults and switches ---------------------------------------------------------------------------------------------

    [Fact]
    public void DefaultsAreOff()
    {
        var d = new VersionPurgeSettings();
        Assert.False(d.Enabled); Assert.False(d.ApplyToAll); Assert.Equal(30, d.PurgeAfterDays); Assert.Equal(25, d.BatchSize);
        Assert.Equal(300, d.CycleSeconds); Assert.Equal(0, d.BackupRetentionDays);
        Assert.False(new OASISDNA().OASIS.VersionPurge.Enabled);
        Assert.Null(new AppUsageOverrides().VersionPurgeEnabled);
    }

    [Fact]
    public async Task PolicyOffMeansNothingIsPurgedOrScheduled()
    {
        var r = new Rig(); r.Settings.Enabled = false;
        var h = r.Add(3, deleted: true);
        r.Service.QueueDeleted(h, "piper");
        Assert.Equal(0, r.Service.QueuedCount);
        var result = await r.Purge(h.Id);
        Assert.Equal(HolonPurgeStatus.Disabled, result.Status);
        var cycle = await r.Service.RunCycleAsync(CancellationToken.None);
        Assert.False(cycle.MasterEnabled);
        Assert.Equal(0, r.Backend.PurgeCalls);
        Assert.True(r.Backend.Primary.ContainsKey(h.Id));
        Assert.Empty(r.Store.DueLimits);
    }

    [Theory]
    // master, applyToAll, avatar, app, expected
    [InlineData(false, true, true, true, HolonPurgeDecision.MasterOff)]   // master forbids even when avatar and app opted in
    [InlineData(true, false, null, null, HolonPurgeDecision.Off)]         // nobody opted in
    [InlineData(true, false, true, null, HolonPurgeDecision.On)]          // avatar opted in
    [InlineData(true, false, null, true, HolonPurgeDecision.On)]          // app opted in
    [InlineData(true, true, null, null, HolonPurgeDecision.On)]           // node default on
    [InlineData(true, true, false, true, HolonPurgeDecision.Off)]         // avatar (most specific) beats app
    [InlineData(true, false, true, false, HolonPurgeDecision.On)]         // avatar beats app the other way
    [InlineData(true, true, null, false, HolonPurgeDecision.Off)]         // app beats the node default
    [InlineData(true, false, false, null, HolonPurgeDecision.Off)]
    public void OverrideCombinations(bool master, bool all, bool? avatar, bool? app, HolonPurgeDecision expected)
    {
        var settings = new VersionPurgeSettings { Enabled = master, ApplyToAll = all };
        var policy = new HolonPurgePolicy(() => settings);
        var owner = Guid.NewGuid();
        policy.SetOwner(owner, avatar);
        if (app.HasValue) policy.ReplaceApps(new[] { new KeyValuePair<string, bool>("piper", app.Value) });
        Assert.Equal(expected, policy.Decide(owner, "piper"));
    }

    [Fact]
    public void ClearingAnAvatarChoiceReturnsToInheriting()
    {
        var policy = new HolonPurgePolicy(() => new VersionPurgeSettings { Enabled = true, ApplyToAll = true });
        var a = Guid.NewGuid();
        policy.SetOwner(a, false); Assert.Equal(HolonPurgeDecision.Off, policy.Decide(a, null));
        policy.SetOwner(a, null); Assert.Equal(HolonPurgeDecision.On, policy.Decide(a, null));
    }

    // ---- explicit purge -----------------------------------------------------------------------------------------------------

    [Fact]
    public async Task PurgeRemovesEveryVersionAndNothingElse()
    {
        var r = new Rig();
        var target = r.Add(5, deleted: true);
        var other = r.Add(4, deleted: true);
        var live = r.Add(2, deleted: false);
        var result = await r.Purge(target.Id);
        Assert.Equal(HolonPurgeStatus.Purged, result.Status);
        Assert.Equal(5, result.VersionsRemoved);
        Assert.False(r.Backend.Primary.ContainsKey(target.Id));
        Assert.Equal(4, r.Backend.Primary[other.Id].Versions);
        Assert.Equal(2, r.Backend.Primary[live.Id].Versions);
        Assert.Contains("backups", result.BackupNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PurgeWritesOneContentFreeAuditRecord()
    {
        var r = new Rig();
        var h = r.Add(3, deleted: true);
        await r.Purge(h.Id);
        var record = Assert.Single(r.Store.Audit);
        Assert.Equal(h.Id, record.HolonId); Assert.Equal(r.Owner, record.ByAvatarId); Assert.Equal("explicit", record.Trigger);
        Assert.Equal(3, record.VersionsRemoved); Assert.Equal(r.Now, record.PurgedAtUtc);
        // the record type carries ids, counts and store names only
        Assert.All(typeof(HolonPurgeAuditRecord).GetProperties(), p => Assert.DoesNotContain(p.PropertyType, new[] { typeof(Holon), typeof(IDictionary<string, object>) }));
        Assert.Equal("purged", r.Store.Entries[h.Id].Status);
    }

    [Fact]
    public async Task AnUnauthorisedCallerIsRefusedAndNothingIsRemoved()
    {
        var r = new Rig();
        var h = r.Add(3, deleted: true);
        var stranger = Guid.NewGuid();
        var result = await r.Purge(h.Id, stranger);
        Assert.Equal(HolonPurgeStatus.Forbidden, result.Status);
        Assert.Equal(0, r.Backend.PurgeCalls);
        Assert.Empty(r.Store.Audit);
        Assert.True(r.Backend.Primary.ContainsKey(h.Id));
    }

    [Fact]
    public async Task AnAvatarWithADeleteGrantMayPurge()
    {
        var r = new Rig();
        var h = r.Add(2, deleted: true);
        var grantee = Guid.NewGuid(); r.Backend.DeleteGrants.Add((h.Id, grantee));
        Assert.Equal(HolonPurgeStatus.Purged, (await r.Purge(h.Id, grantee)).Status);
    }

    [Fact]
    public async Task ALiveHolonIsRefusedUnlessAllowLiveIsPassed()
    {
        var r = new Rig();
        var h = r.Add(2, deleted: false);
        Assert.Equal(HolonPurgeStatus.NotDeleted, (await r.Purge(h.Id)).Status);
        Assert.True(r.Backend.Primary.ContainsKey(h.Id));
        Assert.Equal(HolonPurgeStatus.Purged, (await r.Purge(h.Id, live: true)).Status);
        Assert.False(r.Backend.Primary.ContainsKey(h.Id));
    }

    [Fact]
    public async Task ALegalHoldBlocksTheExplicitPurgeUntilReleased()
    {
        var r = new Rig();
        var h = r.Add(3, deleted: true);
        Assert.Equal(HolonPurgeStatus.HoldSet, (await r.Service.SetHoldAsync(h.Id, r.Caller(), true, "parent access request", CancellationToken.None)).Status);
        Assert.Equal(HolonPurgeStatus.OnHold, (await r.Purge(h.Id)).Status);
        Assert.True(r.Backend.Primary.ContainsKey(h.Id));
        Assert.Equal(HolonPurgeStatus.HoldReleased, (await r.Service.SetHoldAsync(h.Id, r.Caller(), false, null, CancellationToken.None)).Status);
        Assert.Equal(HolonPurgeStatus.Purged, (await r.Purge(h.Id)).Status);
    }

    [Fact]
    public async Task OnlyAnAuthorisedCallerMayChangeAHold()
    {
        var r = new Rig();
        var h = r.Add(1, deleted: true);
        Assert.Equal(HolonPurgeStatus.Forbidden, (await r.Service.SetHoldAsync(h.Id, r.Caller(Guid.NewGuid()), true, "x", CancellationToken.None)).Status);
        Assert.False(r.Store.Entries.ContainsKey(h.Id));
    }

    [Fact]
    public async Task BatchPurgeReportsEachIdAndRespectsTheLimit()
    {
        var r = new Rig();
        var a = r.Add(2, true); var b = r.Add(3, true); var held = r.Add(1, true); var strangers = r.Add(1, true, Guid.NewGuid());
        await r.Service.SetHoldAsync(held.Id, r.Caller(), true, "hold", CancellationToken.None);
        var results = await r.Service.PurgeBatchAsync(new[] { a.Id, b.Id, held.Id, strangers.Id, Guid.NewGuid(), a.Id }, r.Caller(), false, CancellationToken.None);
        Assert.Equal(5, results.Count); // duplicate dropped
        Assert.Equal(HolonPurgeStatus.Purged, results[0].Status);
        Assert.Equal(HolonPurgeStatus.Purged, results[1].Status);
        Assert.Equal(HolonPurgeStatus.OnHold, results[2].Status);
        Assert.Equal(HolonPurgeStatus.Forbidden, results[3].Status);
        Assert.Equal(HolonPurgeStatus.NotFound, results[4].Status);
        var many = Enumerable.Range(0, 150).Select(_ => Guid.NewGuid()).ToList();
        Assert.Equal(HolonPurgeService.MaxBatch, (await r.Service.PurgeBatchAsync(many, r.Caller(), false, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task AStoreThatCannotBePurgedIsReportedNotHidden()
    {
        var r = new Rig(); r.Settings.BackupRetentionDays = 14; r.Backend.ReplicaFails = true;
        var h = r.Add(3, deleted: true);
        var result = await r.Purge(h.Id);
        Assert.Equal(HolonPurgeStatus.PurgedPartially, result.Status);
        Assert.Equal(new[] { "SQLLiteDBOASIS" }, result.UnpurgedStores);
        Assert.Contains("within 14 days", result.BackupNote);
        Assert.Equal(new[] { "SQLLiteDBOASIS" }, r.Store.Audit.Single().StoresNotPurged);
    }

    [Fact]
    public async Task APurgedHolonIsReportedAsPurgedToItsOwnerOnly()
    {
        var r = new Rig();
        var h = r.Add(2, true);
        await r.Purge(h.Id);
        Assert.Equal(HolonPurgeStatus.AlreadyPurged, (await r.Purge(h.Id)).Status);
        Assert.Equal(HolonPurgeStatus.NotFound, (await r.Purge(h.Id, Guid.NewGuid())).Status);
    }

    [Fact]
    public async Task AFailedAuditWriteNeverTurnsAPurgeIntoAFailure()
    {
        var r = new Rig(); r.Store.FailAudit = true;
        var h = r.Add(2, true);
        var result = await r.Purge(h.Id);
        Assert.Equal(HolonPurgeStatus.Purged, result.Status);
        Assert.Contains("Warning", result.Message);
    }

    // ---- scheduling and the worker ------------------------------------------------------------------------------------------

    private static async Task Schedule(Rig r, Holon h) { r.Service.QueueDeleted(h, "piper"); await r.Service.DrainQueueAsync(CancellationToken.None); }

    [Fact]
    public async Task ADeletionIsScheduledForDeletedDatePlusDaysAndNotPurgedEarly()
    {
        var r = new Rig();
        var h = r.Add(3, deleted: true);
        await Schedule(r, h);
        var entry = r.Store.Entries[h.Id];
        Assert.Equal("pending", entry.Status);
        Assert.Equal(h.DeletedDate.AddDays(30), entry.PurgeAfterUtc);
        var early = await r.Service.RunCycleAsync(CancellationToken.None);
        Assert.Equal(0, early.Due); Assert.True(r.Backend.Primary.ContainsKey(h.Id));
        r.Now = h.DeletedDate.AddDays(30).AddMinutes(1);
        var due = await r.Service.RunCycleAsync(CancellationToken.None);
        Assert.Equal(1, due.Purged); Assert.False(r.Backend.Primary.ContainsKey(h.Id));
        Assert.Equal("scheduled", r.Store.Audit.Single().Trigger); Assert.Equal(Guid.Empty, r.Store.Audit.Single().ByAvatarId);
    }

    [Fact]
    public async Task ZeroDaysSchedulesForTheMomentOfDeletion()
    {
        var r = new Rig(); r.Settings.PurgeAfterDays = 0;
        var h = r.Add(2, true);
        await Schedule(r, h);
        Assert.Equal(h.DeletedDate, r.Store.Entries[h.Id].PurgeAfterUtc);
        Assert.Equal(1, (await r.Service.RunCycleAsync(CancellationToken.None)).Purged);
    }

    [Fact]
    public async Task RestoringBeforeTheDateCancelsThePurge()
    {
        var r = new Rig();
        var h = r.Add(3, deleted: true);
        await Schedule(r, h);
        h.DeletedDate = DateTime.MinValue; // restored
        r.Now = r.Now.AddDays(60);
        var cycle = await r.Service.RunCycleAsync(CancellationToken.None);
        Assert.Equal(1, cycle.CancelledRestored); Assert.Equal(0, cycle.Purged);
        Assert.Equal("cancelled", r.Store.Entries[h.Id].Status);
        Assert.Equal(3, r.Backend.Primary[h.Id].Versions);
        Assert.Equal(0, r.Backend.PurgeCalls);
    }

    [Fact]
    public async Task ALegalHoldBlocksTheWorker()
    {
        var r = new Rig();
        var h = r.Add(3, deleted: true);
        await Schedule(r, h);
        await r.Service.SetHoldAsync(h.Id, r.Caller(), true, "access request open", CancellationToken.None);
        r.Now = r.Now.AddDays(60);
        var cycle = await r.Service.RunCycleAsync(CancellationToken.None);
        Assert.Equal(1, cycle.HeldBack); Assert.Equal(0, cycle.Purged); Assert.Equal(0, r.Backend.PurgeCalls);
        await r.Service.SetHoldAsync(h.Id, r.Caller(), false, null, CancellationToken.None);
        Assert.Equal(1, (await r.Service.RunCycleAsync(CancellationToken.None)).Purged);
    }

    [Fact]
    public async Task AHoldSetBeforeTheDeletionIsKeptWhenItIsScheduled()
    {
        var r = new Rig();
        var h = r.Add(3, deleted: false);
        await r.Service.SetHoldAsync(h.Id, r.Caller(), true, "early", CancellationToken.None);
        h.DeletedDate = r.Now.AddDays(-40);
        await Schedule(r, h);
        Assert.True(r.Store.Entries[h.Id].Hold);
        Assert.Equal(1, (await r.Service.RunCycleAsync(CancellationToken.None)).HeldBack);
    }

    [Fact]
    public async Task APolicyWithdrawnAfterDeletionCancelsThePurge()
    {
        var r = new Rig(); r.Settings.ApplyToAll = false;
        r.Policy.SetOwner(r.Owner, true);
        var h = r.Add(3, true);
        await Schedule(r, h);
        r.Policy.SetOwner(r.Owner, null); // owner withdraws; nobody else opted in
        r.Now = r.Now.AddDays(60);
        var cycle = await r.Service.RunCycleAsync(CancellationToken.None);
        Assert.Equal(1, cycle.CancelledPolicy); Assert.True(r.Backend.Primary.ContainsKey(h.Id));
    }

    [Fact]
    public async Task OnlyHolonsWhoseOwnerOrAppOptedInAreScheduled()
    {
        var r = new Rig(); r.Settings.ApplyToAll = false;
        var optedIn = Guid.NewGuid(); r.Policy.SetOwner(optedIn, true);
        var a = r.Add(1, true, optedIn); var b = r.Add(1, true);
        r.Service.QueueDeleted(a, null); r.Service.QueueDeleted(b, null);
        Assert.Equal(1, await r.Service.DrainQueueAsync(CancellationToken.None));
        Assert.True(r.Store.Entries.ContainsKey(a.Id)); Assert.False(r.Store.Entries.ContainsKey(b.Id));
    }

    [Fact]
    public async Task AWorkerCycleIsBoundedAndMakesOneDueQuery()
    {
        var r = new Rig(); r.Settings.BatchSize = 3; r.Settings.PurgeAfterDays = 0;
        var holons = Enumerable.Range(0, 10).Select(_ => r.Add(2, true)).ToList();
        foreach (var h in holons) r.Service.QueueDeleted(h, "piper");
        var cycle = await r.Service.RunCycleAsync(CancellationToken.None);
        Assert.Equal(3, cycle.Purged);
        Assert.Equal(3, r.Backend.PurgeCalls);
        Assert.Equal(new[] { 3 }, r.Store.DueLimits);   // exactly one query, limited to the batch size
        Assert.Equal(7, r.Backend.Primary.Count);
        Assert.Equal(3, (await r.Service.RunCycleAsync(CancellationToken.None)).Purged); // the rest follow in later cycles
    }

    [Fact]
    public async Task AFailedScheduleWriteIsRetriedNotLost()
    {
        var r = new Rig();
        var h = r.Add(1, true);
        r.Service.QueueDeleted(h, "piper");
        var failing = new Rig { Store = new FakeStore() };
        // a store that throws once
        var flaky = new ThrowOnceStore(); var service = new HolonPurgeService(r.Policy, flaky, r.Backend, () => r.Now);
        service.QueueDeleted(h, "piper");
        Assert.Equal(0, await service.DrainQueueAsync(CancellationToken.None));
        Assert.Equal(1, service.QueuedCount);
        Assert.Equal(1, await service.DrainQueueAsync(CancellationToken.None));
        Assert.Equal(0, service.QueuedCount);
    }

    private sealed class ThrowOnceStore : IHolonPurgeStore
    {
        private bool _thrown;
        public Task SchedulePendingAsync(HolonPurgeEntry e, CancellationToken c) { if (!_thrown) { _thrown = true; throw new InvalidOperationException("down"); } return Task.CompletedTask; }
        public Task<HolonPurgeEntry> GetAsync(Guid id, CancellationToken c) => Task.FromResult<HolonPurgeEntry>(null);
        public Task CancelAsync(Guid id, string r, CancellationToken c) => Task.CompletedTask;
        public Task SetHoldAsync(Guid id, Guid o, bool h, string r, Guid b, DateTime n, CancellationToken c) => Task.CompletedTask;
        public Task<IReadOnlyList<HolonPurgeEntry>> ListDueAsync(DateTime n, int l, CancellationToken c) => Task.FromResult<IReadOnlyList<HolonPurgeEntry>>(new List<HolonPurgeEntry>());
        public Task MarkPurgedAsync(Guid id, Guid o, DateTime a, int v, CancellationToken c) => Task.CompletedTask;
        public Task WriteAuditAsync(HolonPurgeAuditRecord r, CancellationToken c) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<Guid, PurgeChoice>> ListAvatarChoicesAsync(CancellationToken c) => Task.FromResult<IReadOnlyDictionary<Guid, PurgeChoice>>(new Dictionary<Guid, PurgeChoice>());
        public Task<PurgeChoice> GetAvatarChoiceAsync(Guid a, CancellationToken c) => Task.FromResult(new PurgeChoice());
        public Task SetAvatarChoiceAsync(Guid a, PurgeChoice e, CancellationToken c) => Task.CompletedTask;
    }

    // ---- days: configurable in the DNA, per app and per avatar; default 30 ------------------------------------------------

    [Fact]
    public void DaysDefaultToThirtyAndTheMostSpecificExplicitValueWins()
    {
        var settings = new VersionPurgeSettings { Enabled = true };
        var policy = new HolonPurgePolicy(() => settings);
        var owner = Guid.NewGuid();
        Assert.Equal((30, "global"), policy.EffectiveDays(owner, "piper"));
        settings.PurgeAfterDays = 45;
        Assert.Equal((45, "global"), policy.EffectiveDays(owner, "piper"));
        policy.ReplaceApps(new[] { new KeyValuePair<string, PurgeChoice>("piper", new PurgeChoice { Days = 10 }) });
        Assert.Equal((10, "app"), policy.EffectiveDays(owner, "piper"));
        Assert.Equal((45, "global"), policy.EffectiveDays(owner, "other"));
        policy.SetOwner(owner, null, 0);
        Assert.Equal((0, "avatar"), policy.EffectiveDays(owner, "piper")); // 0 = at deletion, and it beats the app's 10
        policy.SetOwner(owner, null, null);                                // cleared: inherit again
        Assert.Equal((10, "app"), policy.EffectiveDays(owner, "piper"));
    }

    [Fact]
    public void DaysAreIndependentOfTheSwitchAndTheMasterStillForbids()
    {
        var settings = new VersionPurgeSettings { Enabled = false };
        var policy = new HolonPurgePolicy(() => settings);
        var owner = Guid.NewGuid(); policy.SetOwner(owner, true, 7);
        Assert.Equal(HolonPurgeDecision.MasterOff, policy.Decide(owner, null));
        Assert.Equal(7, policy.EffectiveDays(owner, null).Days);
        settings.Enabled = true; settings.ApplyToAll = true;
        policy.SetOwner(owner, null, 7); // days only: the switch is inherited
        Assert.Equal(HolonPurgeDecision.On, policy.Decide(owner, null));
    }

    [Theory]
    [InlineData(null, true)] [InlineData(0, true)] [InlineData(30, true)] [InlineData(3650, true)]
    [InlineData(-1, false)] [InlineData(3651, false)]
    public void DaysAreValidated(int? days, bool ok)
    {
        var message = PurgeChoice.Validate(days);
        Assert.Equal(ok, message == null);
        if (!ok) Assert.Contains("0 to 3650", message);
    }

    [Fact]
    public async Task TheDeletePathSchedulesWithTheEffectiveDaysFixedAtDeletion()
    {
        var r = new Rig();
        r.Policy.ReplaceApps(new[] { new KeyValuePair<string, PurgeChoice>("piper", new PurgeChoice { Days = 5 }) });
        var h = r.Add(2, true);
        r.Service.QueueDeleted(h, "piper");
        r.Settings.PurgeAfterDays = 999; // changed later: must not move an already-scheduled purge
        await r.Service.DrainQueueAsync(CancellationToken.None);
        Assert.Equal(h.DeletedDate.AddDays(5), r.Store.Entries[h.Id].PurgeAfterUtc);
        // an avatar's own days beat the app's
        var own = Guid.NewGuid(); r.Policy.SetOwner(own, null, 2);
        var g = r.Add(1, true, own); r.Service.QueueDeleted(g, "piper"); await r.Service.DrainQueueAsync(CancellationToken.None);
        Assert.Equal(g.DeletedDate.AddDays(2), r.Store.Entries[g.Id].PurgeAfterUtc);
    }

    [Fact]
    public void ThePurgedStatusIsADistinctSealStatus()
    {
        Assert.NotEqual(HolonSealStatus.Broken, HolonSealStatus.Purged);
        Assert.NotEqual(HolonSealStatus.Unavailable, HolonSealStatus.Purged);
    }
}
