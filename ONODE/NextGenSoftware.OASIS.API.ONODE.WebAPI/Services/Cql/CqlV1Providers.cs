using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Read the existing host registry. Missing/inactive providers do not trigger activation or failover.</summary>
public sealed class CqlV1Providers : ICqlV1Providers
{
    private readonly ICqlHostedSourceRegistry sources;
    public CqlV1Providers(ICqlHostedSourceRegistry sources = null) => this.sources = sources;

    public IOASISStorageProvider Resolve(ProviderType? requested)
    {
        var registry = ProviderManager.Instance;
        var provider = requested.HasValue
            ? sources == null ? registry.GetStorageProvider(requested.Value) : sources.Find(requested.Value)
            : registry.CurrentStorageProvider;
        if (provider == null || !provider.IsProviderActivated) throw new InvalidOperationException("Provider unavailable.");
        return provider;
    }

    public HolonManager CreateManager(IOASISStorageProvider provider)
        => CreateQueryManager(OASISBootLoader.OASISBootLoader.OASISDNA, ProviderManager.Instance);

    internal static HolonManager CreateQueryManager(NextGenSoftware.OASIS.API.DNA.OASISDNA dna, ProviderManager registry)
    {
        if (dna?.OASIS?.HyperDriveMode != NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2)
            throw new InvalidOperationException("CQL requires Hyperdrive V2.");
        // CQL dispatch owns an explicit, already-admitted adapter snapshot.
        // Supplying it to the storage-manager constructor would register it,
        // activate it or subscribe persistent events as a constructor side effect.
        // In particular a read-only representation must never replace or conflict
        // with a wallet-backed canonical provider of the same enum type.
        return new HolonManager(null, dna, registry);
    }
}

// Concrete host composition stays with the production provider resolver.
// Loopback demo hosts supply their own registry and do not reference BootLoader.
/// <summary>Registers explicitly enrolled CQL-only sources without switching the default
/// provider or editing replication/failover lists. Registry owns provider lifetime.</summary>
public sealed class CqlHostedSourceRegistry : ICqlHostedSourceRegistry
{
    private readonly IReadOnlyDictionary<ProviderType, CqlReadSourceProfile> profiles;

    public CqlHostedSourceRegistry(IEnumerable<CqlReadSourceProfile> profiles = null)
        => this.profiles = (profiles ?? Array.Empty<CqlReadSourceProfile>())
            .ToDictionary(profile => profile.Storage.ProviderType.Value);

    public IOASISStorageProvider Find(ProviderType provider)
        => profiles.TryGetValue(provider, out var profile) ? profile.Storage
            : ProviderManager.Instance.GetStorageProvider(provider);

    public async Task ActivateReadSourceAsync(ProviderType provider, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IOASISStorageProvider storage;
        if (profiles.TryGetValue(provider, out var profile)) storage = profile.Storage;
        else
        {
            var registration = await OASISBootLoader.OASISBootLoader.RegisterProviderAsync(provider,
                activateProviderIfOASISProviderBootTypeIsHot: false);
            if (registration == null || registration.IsError || registration.Result == null)
                throw new InvalidOperationException("CQL_SOURCE_REGISTRATION_FAILED");
            storage = registration.Result;
        }
        if (storage.ProviderType?.Value != provider || storage is not ICosmicQueryStorageProvider adapter
            || adapter.QueryProviderType != provider || adapter.QueryCapabilities == null
            || storage is not ICosmicReadOnlyQueryActivation activationProvider)
            throw new InvalidOperationException("CQL_SOURCE_ADAPTER_INVALID");
        cancellationToken.ThrowIfCancellationRequested();
        // Activation is an instance/schema health check, not a user query or business save.
        // Await the owning provider operation; do not detach it on an observation timeout.
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var activation = await activationProvider.ActivateQuerySourceAsync(deadline.Token);
        cancellationToken.ThrowIfCancellationRequested();
        if (activation == null || activation.IsError || !activation.Result || !storage.IsProviderActivated)
            throw new InvalidOperationException("CQL_SOURCE_ACTIVATION_FAILED");
    }
}
