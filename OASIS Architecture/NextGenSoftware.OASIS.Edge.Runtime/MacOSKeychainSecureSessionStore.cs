using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>macOS Keychain adapter using Security.framework directly; secrets never appear in process arguments.</summary>
    public sealed class MacOSKeychainSecureSessionStore : IEdgeSecureSessionStore
    {
        private const int ItemNotFound = -25300;
        private readonly byte[] _service = Encoding.UTF8.GetBytes("one.oasisomniverse.edge");
        private readonly byte[] _account;

        public MacOSKeychainSecureSessionStore(Guid avatarId, Guid deviceId)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                throw new PlatformNotSupportedException("macOS Keychain is available only on macOS.");
            if (avatarId == Guid.Empty || deviceId == Guid.Empty)
                throw new ArgumentException("Avatar and device ids are required for secure-session isolation.");
            _account = Encoding.UTF8.GetBytes($"{avatarId:N}.{deviceId:N}");
        }

        public Task<OASISResult<bool>> SaveAsync(HyperDriveOfflineSessionGrant grant, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (grant == null) return Task.FromResult(SecureSessionGrantJson.Error<bool>(
                "EDGE_SECURE_SESSION_GRANT_REQUIRED", "A signed offline-session grant is required."));
            byte[] value = SecureSessionGrantJson.Serialize(grant);
            int found = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)_service.Length, _service,
                (uint)_account.Length, _account, out _, out IntPtr password, out IntPtr item);
            if (password != IntPtr.Zero) SecKeychainItemFreeContent(IntPtr.Zero, password);
            try
            {
                int status = found == 0
                    ? SecKeychainItemModifyAttributesAndData(item, IntPtr.Zero, (uint)value.Length, value)
                    : found == ItemNotFound
                        ? SecKeychainAddGenericPassword(IntPtr.Zero, (uint)_service.Length, _service,
                            (uint)_account.Length, _account, (uint)value.Length, value, out item)
                        : found;
                return Task.FromResult(status == 0 ? Success(true, true) : Error<bool>("SAVE", status));
            }
            finally { if (item != IntPtr.Zero) CFRelease(item); }
        }

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)_service.Length, _service,
                (uint)_account.Length, _account, out uint length, out IntPtr password, out IntPtr item);
            try
            {
                if (status == ItemNotFound) return Task.FromResult(new OASISResult<HyperDriveOfflineSessionGrant> { IsLoaded = true });
                if (status != 0) return Task.FromResult(Error<HyperDriveOfflineSessionGrant>("LOAD", status));
                var bytes = new byte[length];
                if (length != 0) Marshal.Copy(password, bytes, 0, bytes.Length);
                return Task.FromResult(SecureSessionGrantJson.Deserialize(bytes));
            }
            finally
            {
                if (password != IntPtr.Zero) SecKeychainItemFreeContent(IntPtr.Zero, password);
                if (item != IntPtr.Zero) CFRelease(item);
            }
        }

        public Task<OASISResult<bool>> DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)_service.Length, _service,
                (uint)_account.Length, _account, out _, out IntPtr password, out IntPtr item);
            if (password != IntPtr.Zero) SecKeychainItemFreeContent(IntPtr.Zero, password);
            try
            {
                if (status == ItemNotFound) return Task.FromResult(Success(true));
                if (status != 0) return Task.FromResult(Error<bool>("DELETE", status));
                status = SecKeychainItemDelete(item);
                return Task.FromResult(status == 0 ? Success(true) : Error<bool>("DELETE", status));
            }
            finally { if (item != IntPtr.Zero) CFRelease(item); }
        }

        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        private static extern int SecKeychainAddGenericPassword(IntPtr keychain, uint serviceLength, byte[] service,
            uint accountLength, byte[] account, uint passwordLength, byte[] password, out IntPtr item);
        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        private static extern int SecKeychainFindGenericPassword(IntPtr keychain, uint serviceLength, byte[] service,
            uint accountLength, byte[] account, out uint passwordLength, out IntPtr password, out IntPtr item);
        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        private static extern int SecKeychainItemModifyAttributesAndData(IntPtr item, IntPtr attributes,
            uint passwordLength, byte[] password);
        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        private static extern int SecKeychainItemDelete(IntPtr item);
        [DllImport("/System/Library/Frameworks/Security.framework/Security")]
        private static extern int SecKeychainItemFreeContent(IntPtr attributes, IntPtr data);
        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr value);

        private static OASISResult<bool> Success(bool value, bool saved = false) => new OASISResult<bool>(value) { IsSaved = saved };
        private static OASISResult<T> Error<T>(string operation, int status) => SecureSessionGrantJson.Error<T>(
            $"EDGE_MACOS_KEYCHAIN_{operation}_FAILED", $"macOS Keychain returned status {status}.");
    }
}
