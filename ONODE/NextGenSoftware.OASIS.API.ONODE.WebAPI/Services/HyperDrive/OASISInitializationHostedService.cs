using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Completes the single shared OASIS/provider initialization before worker services start.</summary>
    public sealed class OASISInitializationHostedService : IHostedService
    {
        private readonly HyperDriveHostedProviderAccessor _providerAccessor;

        public OASISInitializationHostedService(HyperDriveHostedProviderAccessor providerAccessor) =>
            _providerAccessor = providerAccessor;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var result = await _providerAccessor.GetAsync().ConfigureAwait(false);
            if (result == null || result.IsError || result.Result == null)
                throw new InvalidOperationException(result?.Message ?? "OASIS and its default provider could not be initialized.");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
