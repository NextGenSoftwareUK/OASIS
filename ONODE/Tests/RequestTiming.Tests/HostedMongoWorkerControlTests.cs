using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Operations;
using Xunit;

public sealed class HostedMongoWorkerControlTests
{
    private static IConfiguration Settings(params (string Key, string? Value)[] values)
        => new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(x => x.Key, x => x.Value)).Build();

    [Fact]
    public void PausedWorkersAreNotConstructedOrStarted()
    {
        var services = new ServiceCollection();
        HostedMongoWorkerControl.AddHyperDriveWorker<MustNotConstruct>(services,
            Settings((HostedMongoWorkerControl.PauseHyperDriveKey, "true")));
        using var provider = services.BuildServiceProvider();
        Assert.Empty(provider.GetServices<IHostedService>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public void NormalModeRetainsTheHostedWorkerRegistration(string? value)
    {
        var services = new ServiceCollection();
        HostedMongoWorkerControl.AddHyperDriveWorker<MustNotConstruct>(services,
            Settings((HostedMongoWorkerControl.PauseHyperDriveKey, value)));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType == typeof(MustNotConstruct));
    }

    [Fact]
    public void ReconciliationCanPauseIndependentlyWithoutChangingForegroundRegistrations()
    {
        var configuration = Settings((HostedMongoWorkerControl.PauseReconciliationKey, "true"));
        Assert.False(HostedMongoWorkerControl.ReconciliationMayStart(configuration));
        var services = new ServiceCollection();
        services.AddSingleton<ForegroundService>();
        HostedMongoWorkerControl.AddHyperDriveWorker<MustNotConstruct>(services, configuration);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(ForegroundService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void HyperDrivePauseDoesNotImplicitlyDisableReconciliation()
        => Assert.True(HostedMongoWorkerControl.ReconciliationMayStart(
            Settings((HostedMongoWorkerControl.PauseHyperDriveKey, "true"))));

    [Fact]
    public void InvalidPauseValuesFailBeforeRegisteringAWorker()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => HostedMongoWorkerControl.AddHyperDriveWorker<MustNotConstruct>(
            services, Settings((HostedMongoWorkerControl.PauseHyperDriveKey, "invalid"))));
        Assert.Empty(services);
    }

    private sealed class ForegroundService { }
    private sealed class MustNotConstruct : IHostedService
    {
        public MustNotConstruct() => throw new InvalidOperationException("Paused worker must never resolve its dependencies.");
        public Task StartAsync(CancellationToken token) => throw new InvalidOperationException();
        public Task StopAsync(CancellationToken token) => Task.CompletedTask;
    }
}
