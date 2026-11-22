using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class AccessLogOverrideTests
{
    private sealed class Store : IAccessLogStore
    {
        public readonly List<AccessLogEvent> Events = new();
        public Task InsertAsync(IReadOnlyList<AccessLogEvent> b, CancellationToken c) { Events.AddRange(b); return Task.CompletedTask; }
        public Task<IReadOnlyList<AccessLogEvent>> QueryByHolonAsync(Guid id, int l, int o, CancellationToken c) => Task.FromResult<IReadOnlyList<AccessLogEvent>>(new List<AccessLogEvent>());
        public Task<IReadOnlyList<AccessLogEvent>> QueryByAvatarAsync(Guid id, int l, int o, CancellationToken c) => Task.FromResult<IReadOnlyList<AccessLogEvent>>(new List<AccessLogEvent>());
    }
    private static List<(Guid, string)> One() => new() { (Guid.NewGuid(), "Holon") };

    [Fact]
    public async Task AnAppCanOptInEvenWhenTheDnaIsOff()
    {
        var store = new Store(); var dna = new AccessLogSettings { Enabled = false };
        var service = new AccessLogService(() => dna, store);
        service.Overrides.Replace(new[] { ("piper", (bool?)true) });
        Assert.True(service.IsEnabled);
        service.Record(Guid.NewGuid(), "piper", "LoadHolon", One());
        service.Record(Guid.NewGuid(), "other", "LoadHolon", One());
        service.Record(Guid.NewGuid(), null, "LoadHolon", One());
        await service.FlushAsync();
        Assert.Equal("piper", Assert.Single(store.Events).AppId);
    }

    [Fact]
    public async Task AnAppCanOptOutWhenTheDnaIsOn()
    {
        var store = new Store(); var dna = new AccessLogSettings { Enabled = true };
        var service = new AccessLogService(() => dna, store);
        service.Overrides.Replace(new[] { ("piper", (bool?)false) });
        service.Record(Guid.NewGuid(), "piper", "LoadHolon", One());
        service.Record(Guid.NewGuid(), "other", "LoadHolon", One());
        await service.FlushAsync();
        Assert.Equal("other", Assert.Single(store.Events).AppId);
    }

    [Fact]
    public async Task NoOverridesAndDnaOffStillLogsNothing()
    {
        var store = new Store(); var service = new AccessLogService(() => new AccessLogSettings(), store);
        Assert.False(service.IsEnabled);
        service.Record(Guid.NewGuid(), "piper", "LoadHolon", One());
        Assert.Equal(0, await service.FlushAsync());
    }
}
