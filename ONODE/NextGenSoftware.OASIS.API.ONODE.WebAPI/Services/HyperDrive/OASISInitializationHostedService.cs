using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Completes the single shared OASIS/provider initialization before worker services start.</summary>
    public sealed class OASISInitializationHostedService : IHostedService
    {
        private readonly HyperDriveHostedProviderAccessor _providerAccessor;
        private readonly ILogger<OASISInitializationHostedService> _logger;

        public OASISInitializationHostedService(
            HyperDriveHostedProviderAccessor providerAccessor,
            ILogger<OASISInitializationHostedService> logger)
        {
            _providerAccessor = providerAccessor;
            _logger = logger;
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
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
