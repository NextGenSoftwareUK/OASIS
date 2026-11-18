using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Completes the single shared OASIS/provider initialization before worker services start.</summary>
    public sealed class OASISInitializationHostedService : IHostedService
    {
        private readonly HyperDriveHostedProviderAccessor _providerAccessor;
        private readonly ILogger<OASISInitializationHostedService> _logger;
        private readonly IHolonReplicationAuthorizationPolicy _authorization;

        public OASISInitializationHostedService(
            HyperDriveHostedProviderAccessor providerAccessor,
            ILogger<OASISInitializationHostedService> logger,
            IHolonReplicationAuthorizationPolicy authorization)
        {
            _providerAccessor = providerAccessor;
            _logger = logger;
            _authorization = authorization;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var result = await _providerAccessor.GetAsync().ConfigureAwait(false);
            if (result == null || result.IsError || result.Result == null)
            {
                _logger.LogError(
                    "OASIS/default-provider initialization failed; the API host will remain online and provider-backed operations will report degraded availability: {Message}",
                    result?.Message ?? "No provider result was returned.");
            }
            else
            {
                // Foreground enrollment must retain its policy even when delivery is paused.
                HolonReplicationRoutes.UseAuthorizationBinding(ProviderManager.Instance,
                    () => _authorization.BindingIdentity);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
