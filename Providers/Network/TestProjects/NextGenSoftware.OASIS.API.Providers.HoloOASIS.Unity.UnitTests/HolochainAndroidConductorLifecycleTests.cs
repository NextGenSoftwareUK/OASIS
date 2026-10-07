using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.UnitTests;

public sealed class HolochainAndroidConductorLifecycleTests
{
    [Fact]
    public async Task Start_ConvertsAuthenticatedServiceSessionWithoutAdminEndpoint()
    {
        var bridge = new FakeBridge { Session = Session(45678, new byte[] { 1, 2, 3 }) };
        await using var lifecycle = new HolochainAndroidConductorLifecycle(bridge, Options());

        var result = await lifecycle.StartAsync(CancellationToken.None);

        Assert.False(result.IsError, result.Message);
        Assert.Equal("ws://127.0.0.1:45678", result.Result.AppWebSocketUri);
        Assert.Null(result.Result.AdminWebSocketUri);
        Assert.Equal(new byte[] { 1, 2, 3 }, result.Result.AppAuthenticationToken);
        Assert.Equal("oasis", result.Result.InstalledAppId);
    }

    [Fact]
    public async Task Start_RejectsMissingTokenAndDoesNotEnterStartedState()
    {
        var bridge = new FakeBridge { Session = Session(45678, Array.Empty<byte>()) };
        await using var lifecycle = new HolochainAndroidConductorLifecycle(bridge, Options());

        var failed = await lifecycle.StartAsync(CancellationToken.None);
        bridge.Session = Session(45678, new byte[] { 9 });
        var retried = await lifecycle.StartAsync(CancellationToken.None);

        Assert.True(failed.IsError);
        Assert.Equal("HOLO_ANDROID_APP_TOKEN_REQUIRED", failed.ErrorCode);
        Assert.False(retried.IsError, retried.Message);
        Assert.Equal(2, bridge.StartCount);
        Assert.Equal(1, bridge.StopCount);
    }

    [Fact]
    public async Task Resume_UsesEnsureAppWebsocketSession()
    {
        var bridge = new FakeBridge { Session = Session(40000, new byte[] { 1 }) };
        await using var lifecycle = new HolochainAndroidConductorLifecycle(bridge, Options());
        Assert.False((await lifecycle.StartAsync(CancellationToken.None)).IsError);
        bridge.Session = Session(40001, new byte[] { 2 });

        var resumed = await lifecycle.ResumeAsync(CancellationToken.None);

        Assert.False(resumed.IsError, resumed.Message);
        Assert.Equal("ws://127.0.0.1:40001", resumed.Result.AppWebSocketUri);
        Assert.Equal("oasis", bridge.ResumedAppId);
    }

    [Fact]
    public void Constructor_RejectsNonHttpsNetworkEndpoints()
    {
        var options = Options();
        options.BootstrapUrl = "http://bootstrap.invalid";
        Assert.Throws<ArgumentException>(() =>
            new HolochainAndroidConductorLifecycle(new FakeBridge(), options));
    }

    private static HolochainAndroidRuntimeOptions Options() => new()
    {
        AppBundle = new byte[] { 5 }, InstalledAppId = "oasis"
    };

    private static HolochainAndroidAppSession Session(int port, byte[] token) => new()
    {
        Port = port, AuthenticationToken = token
    };

    private sealed class FakeBridge : IHolochainAndroidServiceBridge
    {
        public HolochainAndroidAppSession Session { get; set; } = Session(40000, new byte[] { 1 });
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public string ResumedAppId { get; private set; }

        public Task<OASISResult<HolochainAndroidAppSession>> StartAndSetupAsync(
            HolochainAndroidRuntimeOptions options, CancellationToken cancellationToken)
        {
            StartCount++;
            return Task.FromResult(new OASISResult<HolochainAndroidAppSession>(Session));
        }

        public Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<bool>(true));

        public Task<OASISResult<HolochainAndroidAppSession>> ResumeAsync(
            string installedAppId, CancellationToken cancellationToken)
        {
            ResumedAppId = installedAppId;
            return Task.FromResult(new OASISResult<HolochainAndroidAppSession>(Session));
        }

        public Task<OASISResult<bool>> StopAsync(CancellationToken cancellationToken)
        {
            StopCount++;
            return Task.FromResult(new OASISResult<bool>(true));
        }

        public ValueTask DisposeAsync() => default;
    }
}
