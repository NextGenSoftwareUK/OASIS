using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlRequiredPolicyHostingTests
{
    private sealed class Counts
    {
        public int Constructed, Disposed, Authorized, LaterStarts;
    }

    private sealed class ScopedPolicy : ICqlReadAccessPolicy, IDisposable
    {
        private readonly Counts counts;
        public ScopedPolicy(Counts counts) { this.counts = counts; counts.Constructed++; }
        public OASISResult<CosmicQueryReadAccess> Authorize(Guid actor, ProviderType authority)
        {
            counts.Authorized++;
            throw new InvalidOperationException("Readiness must not authorize an actor.");
        }
        public void Dispose() => counts.Disposed++;
    }

    private sealed class LaterInitialization(Counts counts) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) { counts.LaterStarts++; return Task.CompletedTask; }
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static IHost Host(Counts counts, int registration)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { [CqlReadPolicyEnrollment.ConfigurationKey] = "true" }).Build();
        return new HostBuilder().UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .ConfigureServices((_, services) =>
            {
                services.AddConfiguredCqlReadAccessPolicy(configuration);
                if (registration == 1) services.AddScoped<ICqlReadAccessPolicy>(_ => null!);
                if (registration == 2) services.AddScoped<ICqlReadAccessPolicy>(_ => new ScopedPolicy(counts));
                services.AddSingleton<IHostedService>(new LaterInitialization(counts));
            }).Build();
    }

    [Theory]
    [InlineData(0)] [InlineData(1)]
    public async Task ShippingRequiredPolicyStopsHostBeforeLaterInitialization(int registration)
    {
        var counts = new Counts();
        using var host = Host(counts, registration);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.StartAsync());
        Assert.Contains(CqlReadPolicyEnrollment.ConfigurationKey, error.Message);
        Assert.Equal(0, counts.LaterStarts);
        Assert.Equal(0, counts.Authorized);
    }

    [Fact]
    public async Task ShippingReadinessDisposesItsScopedPolicyWithoutAuthorizing()
    {
        var counts = new Counts();
        using var host = Host(counts, 2);
        await host.StartAsync();
        Assert.Equal(1, counts.Constructed);
        Assert.Equal(1, counts.Disposed);
        Assert.Equal(0, counts.Authorized);
        Assert.Equal(1, counts.LaterStarts);
        await host.StopAsync();
    }
}
