using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Holochain.HoloNET.Client;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.UnitTests;

public sealed class HoloOASISUnityHostTests
{
    [Fact]
    public void ServiceProvisionedProviderBindsRepositoryToInjectedAppClient()
    {
        var appClient = new HoloNETClientAppAgent("oasis", "uhCAk-agent", new HoloNETDNA
        {
            HolochainConductorAppAgentURI = "ws://127.0.0.1:8888",
            AppAuthenticationToken = new byte[] { 1, 2, 3 },
            AutoStartHolochainConductor = false
        });
        var provider = new HoloOASIS(null, appClient);
        var field = typeof(global::NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS)
            .GetField("_genericRepository", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
        var repository = Assert.IsType<global::NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories.GenericRepository>(
            field.GetValue(provider));

        Assert.Same(appClient, repository.HoloNETClientAppAgent);
    }

    [Fact]
    public async Task StartSurfacesConductorFailureWithoutCreatingProvider()
    {
        var lifecycle = new FakeLifecycle
        {
            StartResult = Error<HolochainConductorEndpoints>("service unavailable")
        };
        var host = new HoloOASISUnityHost(lifecycle);

        var result = await host.StartAsync();

        Assert.True(result.IsError);
        Assert.Contains("HOLO_CONDUCTOR_START_FAILED", result.Message);
        Assert.Null(host.Provider);
        await host.DisposeAsync();
        Assert.Equal(0, lifecycle.StopCalls);
    }

    [Fact]
    public async Task StartRejectsNonWebSocketEndpointsAndStopsConductor()
    {
        var lifecycle = new FakeLifecycle
        {
            StartResult = Success(new HolochainConductorEndpoints
            {
                AdminWebSocketUri = "http://127.0.0.1:7777",
                AppWebSocketUri = "ws://127.0.0.1:8888"
            })
        };
        var host = new HoloOASISUnityHost(lifecycle);

        var result = await host.StartAsync();

        Assert.True(result.IsError);
        Assert.Contains("HOLO_CONDUCTOR_ENDPOINT_INVALID", result.Message);
        Assert.Null(host.Provider);
        await host.DisposeAsync();
        Assert.Equal(1, lifecycle.StopCalls);
    }

    [Fact]
    public async Task ServiceProvisionedSessionRequiresAuthenticationToken()
    {
        var lifecycle = new FakeLifecycle
        {
            StartResult = Success(new HolochainConductorEndpoints
            {
                AppWebSocketUri = "ws://127.0.0.1:8888",
                AgentPublicKey = "uhCAk-agent",
                InstalledAppId = "oasis"
            })
        };
        var host = new HoloOASISUnityHost(lifecycle);

        var result = await host.StartAsync();

        Assert.True(result.IsError);
        Assert.Contains("HOLO_CONDUCTOR_APP_TOKEN_REQUIRED", result.Message);
        Assert.Null(host.Provider);
        await host.DisposeAsync();
        Assert.Equal(1, lifecycle.StopCalls);
    }

    private sealed class FakeLifecycle : IHolochainConductorLifecycle
    {
        public OASISResult<HolochainConductorEndpoints> StartResult { get; set; } =
            Error<HolochainConductorEndpoints>("not configured");
        public int StopCalls { get; private set; }

        public Task<OASISResult<HolochainConductorEndpoints>> StartAsync(CancellationToken cancellationToken) =>
            Task.FromResult(StartResult);

        public Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Success(true));

        public Task<OASISResult<HolochainConductorEndpoints>> ResumeAsync(CancellationToken cancellationToken) =>
            Task.FromResult(StartResult);

        public Task<OASISResult<bool>> StopAsync(CancellationToken cancellationToken)
        {
            StopCalls++;
            return Task.FromResult(Success(true));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static OASISResult<T> Success<T>(T value) => new(value);
    private static OASISResult<T> Error<T>(string message) => new() { IsError = true, Message = message };
}
