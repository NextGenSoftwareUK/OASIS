using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cosmic;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public partial class HostedCosmicRecoveryTests
{
    private sealed class PolicyLifetime
    {
        public int Constructed, Disposed, Authorized;
    }

    private sealed class LifetimePolicy : ICosmicOperationRecoveryPolicy, IDisposable
    {
        private readonly PolicyLifetime lifetime;
        public LifetimePolicy(PolicyLifetime lifetime) { this.lifetime = lifetime; lifetime.Constructed++; }
        public OASISResult<CosmicOperationRecoveryGrant> Authorize(Guid actor, ProviderType authority)
        {
            lifetime.Authorized++;
            throw new InvalidOperationException("Readiness must not authorize or resolve record keys.");
        }
        public void Dispose() => lifetime.Disposed++;
    }

    private static IServiceCollection EnrolledRecoveryServices() => new ServiceCollection()
        .AddSingleton(new StorageAdmissionRoutes(new[]
            { new StorageAdmissionRoute("POST", CosmicOperationRecoveryRoutes.Recover, Budget()) }));

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RecoveryReadinessOwnsAShortScopeUnderEitherScopeValidationMode(bool validateScopes)
    {
        var lifetime = new PolicyLifetime();
        using var services = EnrolledRecoveryServices()
            .AddScoped<ICosmicOperationRecoveryPolicy>(_ => new LifetimePolicy(lifetime))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = validateScopes });
        await new CosmicRecoveryReadiness(services).StartAsync(CancellationToken.None);
        Assert.Equal(1, lifetime.Constructed);
        Assert.Equal(1, lifetime.Disposed);
        Assert.Equal(0, lifetime.Authorized);
    }

    [Fact]
    public async Task RecoveryReadinessDoesNotDisposeAnApplicationSingleton()
    {
        var lifetime = new PolicyLifetime();
        var services = EnrolledRecoveryServices()
            .AddSingleton<ICosmicOperationRecoveryPolicy>(_ => new LifetimePolicy(lifetime))
            .BuildServiceProvider();
        try
        {
            await new CosmicRecoveryReadiness(services).StartAsync(CancellationToken.None);
            Assert.Equal(1, lifetime.Constructed);
            Assert.Equal(0, lifetime.Disposed);
            Assert.Equal(0, lifetime.Authorized);
        }
        finally { services.Dispose(); }
        Assert.Equal(1, lifetime.Disposed);
    }

    [Fact]
    public async Task UnenrolledOrCancelledRecoveryReadinessNeverConstructsAPolicy()
    {
        var lifetime = new PolicyLifetime();
        using var services = new ServiceCollection()
            .AddScoped<ICosmicOperationRecoveryPolicy>(_ => new LifetimePolicy(lifetime)).BuildServiceProvider();
        var readiness = new CosmicRecoveryReadiness(services);
        await readiness.StartAsync(CancellationToken.None);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => readiness.StartAsync(new CancellationToken(true)));
        Assert.Equal(0, lifetime.Constructed);
        Assert.Equal(0, lifetime.Disposed);
        Assert.Equal(0, lifetime.Authorized);
    }
}
