using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Serializes OASIS boot/provider activation across all hosted HyperDrive workers.</summary>
    public sealed class HyperDriveHostedProviderAccessor
    {
        private readonly object _gate = new object();
        private Task<OASISResult<IOASISStorageProvider>> _initialization;

        public Task<OASISResult<IOASISStorageProvider>> GetAsync()
        {
            lock (_gate)
                return _initialization ??= InitializeAsync();
        }

        private static async Task<OASISResult<IOASISStorageProvider>> InitializeAsync()
        {
            return await OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync()
                .ConfigureAwait(false);
        }
    }
}
