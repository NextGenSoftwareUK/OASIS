using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class AccessLogTests
{
    private sealed class FakeStore : IAccessLogStore
    {
        public int Calls; public readonly List<AccessLogEvent> Events = new(); public bool Throw;
        public Task InsertAsync(IReadOnlyList<AccessLogEvent> batch, CancellationToken c)
        {
            Calls++; if (Throw) throw new InvalidOperationException("store down");
            Events.AddRange(batch); return Task.CompletedTask;
        }
        public Task<IReadOnlyList<AccessLogEvent>> QueryByHolonAsync(Guid id, int l, int o, CancellationToken c) => Task.FromResult<IReadOnlyList<AccessLogEvent>>(Events.Where(e => e.HolonIds.Contains(id)).ToList());
        public Task<IReadOnlyList<AccessLogEvent>> QueryByAvatarAsync(Guid id, int l, int o, CancellationToken c) => Task.FromResult<IReadOnlyList<AccessLogEvent>>(Events.Where(e => e.AvatarId == id).ToList());
    }

    private static readonly Guid Reader = Guid.NewGuid();
    private static List<(Guid, string)> Holons(int n, string type = "Holon") => Enumerable.Range(0, n).Select(_ => (Guid.NewGuid(), type)).ToList();

    private static (AccessLogService Service, FakeStore Store, AccessLogSettings Settings) Make(Action<AccessLogSettings> configure = null)
    {
        var settings = new AccessLogSettings { Enabled = true };
        configure?.Invoke(settings);
        var store = new FakeStore();
        return (new AccessLogService(() => settings, store), store, settings);
    }

    [Fact]
    public async Task DisabledByDefaultLogsNothingAndNeverTouchesTheStore()
    {
        var store = new FakeStore();
        var defaults = new AccessLogSettings();
        Assert.False(defaults.Enabled);
        var service = new AccessLogService(() => defaults, store);
        service.Record(Reader, "app", "LoadHolon", Holons(1));
        service.Record(Reader, "app", "LoadAllHolons", Holons(50));
        Assert.Equal(0, await service.FlushAsync());
        Assert.Equal(0, store.Calls);
        Assert.Empty(store.Events);
        Assert.Equal(0, service.DroppedEvents);
    }

    [Fact]
    public async Task NullSettingsAlsoLogNothing()
    {
        var store = new FakeStore();
        var service = new AccessLogService(() => null, store);
        service.Record(Reader, "a", "LoadHolon", Holons(1));
        Assert.Equal(0, await service.FlushAsync());
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task OneReadIsOneEventWithIdsOnly()
    {
        var (service, store, _) = Make();
        var h = Holons(1);
        service.Record(Reader, "piper", "LoadHolon", h);
        Assert.Equal(1, await service.FlushAsync());
        var e = Assert.Single(store.Events);
        Assert.Equal(Reader, e.AvatarId); Assert.Equal("piper", e.AppId); Assert.Equal("LoadHolon", e.Operation);
        Assert.Equal(h[0].Item1, Assert.Single(e.HolonIds)); Assert.Equal(1, e.TotalCount);
    }

    [Fact]
    public async Task BulkReadIsOneEventWithCappedIdsAndTotalCount()
    {
        var (service, store, _) = Make(s => s.MaxHolonIdsPerEvent = 10);
        service.Record(Reader, null, "LoadAllHolons", Holons(75));
        await service.FlushAsync();
        var e = Assert.Single(store.Events);
        Assert.Equal(10, e.HolonIds.Count); Assert.Equal(75, e.TotalCount);
    }

    [Fact]
    public async Task AppOperationAndTypeFiltersApply()
    {
        var (service, store, _) = Make(s => { s.Apps = new() { "piper" }; s.Operations = new() { "LoadHolon" }; s.HolonTypes = new() { "Student" }; });
        service.Record(Reader, "other", "LoadHolon", Holons(1, "Student"));
        service.Record(Reader, "piper", "LoadAllHolons", Holons(1, "Student"));
        service.Record(Reader, "piper", "LoadHolon", Holons(1, "Note"));
        service.Record(Reader, "piper", "LoadHolon", Holons(1, "Student"));
        await service.FlushAsync();
        Assert.Single(store.Events);
    }

    [Fact]
    public async Task IpIsOnlyStoredWhenCaptureIpIsOn()
    {
        var (a, sa, _) = Make(); a.Record(Reader, null, "LoadHolon", Holons(1), "1.2.3.4"); await a.FlushAsync();
        Assert.Null(sa.Events[0].IpAddress);
        var (b, sb, _) = Make(s => s.CaptureIp = true); b.Record(Reader, null, "LoadHolon", Holons(1), "1.2.3.4"); await b.FlushAsync();
        Assert.Equal("1.2.3.4", sb.Events[0].IpAddress);
    }

    [Fact]
    public async Task FullQueueDropsAndCountsInsteadOfBlockingOrThrowing()
    {
        var (service, store, _) = Make(s => s.QueueCapacity = 3);
        for (var i = 0; i < 10; i++) service.Record(Reader, null, "LoadHolon", Holons(1));
        Assert.Equal(7, service.DroppedEvents);
        Assert.Equal(3, await service.FlushAsync());
    }

    [Fact]
    public async Task StoreFailureDoesNotBreakRecordingOrThrow()
    {
        var (service, store, _) = Make();
        store.Throw = true;
        service.Record(Reader, null, "LoadHolon", Holons(1));
        var written = await service.FlushAsync();
        Assert.Equal(0, written); Assert.Equal(1, service.FailedBatches);
        store.Throw = false;
        service.Record(Reader, null, "LoadHolon", Holons(1));
        Assert.Equal(1, await service.FlushAsync());
    }

    [Fact]
    public async Task UnknownReaderOrEmptyReadIsIgnored()
    {
        var (service, store, _) = Make();
        service.Record(Guid.Empty, null, "LoadHolon", Holons(1));
        service.Record(Reader, null, "LoadHolon", new List<(Guid, string)>());
        Assert.Equal(0, await service.FlushAsync());
    }

    [Fact]
    public void OnlyOwnerOrAdministratorMaySeeWhoReadAHolon()
    {
        var owner = Guid.NewGuid(); var other = Guid.NewGuid();
        Assert.True(AccessLogService.CanViewHolonLog(false, owner, owner));
        Assert.True(AccessLogService.CanViewHolonLog(true, other, owner));
        Assert.False(AccessLogService.CanViewHolonLog(false, other, owner));
        Assert.False(AccessLogService.CanViewHolonLog(true, Guid.Empty, owner));
    }

    [Fact]
    public void AnAvatarSeesOnlyItsOwnReadsUnlessAdministrator()
    {
        var me = Guid.NewGuid(); var you = Guid.NewGuid();
        Assert.True(AccessLogService.CanViewAvatarLog(false, me, me));
        Assert.False(AccessLogService.CanViewAvatarLog(false, me, you));
        Assert.True(AccessLogService.CanViewAvatarLog(true, me, you));
    }

    [Fact]
    public void DnaDefaultsAreOffAndSafe()
    {
        var s = new AccessLogSettings();
        Assert.False(s.Enabled); Assert.False(s.CaptureIp); Assert.Empty(s.Apps); Assert.Equal(365, s.RetentionDays);
        Assert.Equal(200, s.MaxHolonIdsPerEvent); Assert.Equal(10000, s.QueueCapacity);
    }
}
