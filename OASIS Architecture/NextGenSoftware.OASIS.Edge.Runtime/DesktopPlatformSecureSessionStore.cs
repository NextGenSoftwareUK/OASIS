using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>
    /// Selects the operating system's protected desktop credential facility. This is platform
    /// dispatch, not a fallback: an unavailable native store returns a structured error and never
    /// writes the grant to an ordinary file.
    /// </summary>
    public sealed class DesktopPlatformSecureSessionStore : IEdgeSecureSessionStore
    {
        private readonly IEdgeSecureSessionStore _platformStore;

        public DesktopPlatformSecureSessionStore(Guid avatarId, Guid deviceId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _platformStore = new WindowsCredentialSecureSessionStore(avatarId, deviceId);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                _platformStore = new MacOSKeychainSecureSessionStore(avatarId, deviceId);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _platformStore = new LinuxSecretServiceSecureSessionStore(avatarId, deviceId);
            else
                throw new PlatformNotSupportedException(
                    "This desktop operating system has no supported OASIS protected credential adapter.");
        }

        public Task<OASISResult<bool>> SaveAsync(HyperDriveOfflineSessionGrant grant,
            CancellationToken cancellationToken) => _platformStore.SaveAsync(grant, cancellationToken);
        public Task<OASISResult<HyperDriveOfflineSessionGrant>> LoadAsync(CancellationToken cancellationToken) =>
            _platformStore.LoadAsync(cancellationToken);
        public Task<OASISResult<bool>> DeleteAsync(CancellationToken cancellationToken) =>
            _platformStore.DeleteAsync(cancellationToken);
    }
}
