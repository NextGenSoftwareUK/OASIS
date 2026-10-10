using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>
    /// Desktop Windows secure-session adapter backed by Credential Manager. This implementation is
    /// shared by NativeAOT games and Unity so signed offline grants never enter ordinary files.
    /// </summary>
    public sealed class WindowsCredentialSecureSessionStore : IEdgeSecureSessionStore
    {
        private const uint GenericCredential = 1;
        private const uint PersistLocalMachine = 2;
        private const int ErrorNotFound = 1168;
        private const int MaximumCredentialBlobBytes = 2560;
        private readonly string _key;

        public WindowsCredentialSecureSessionStore(Guid avatarId, Guid deviceId)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Windows Credential Manager is available only on Windows.");
            if (avatarId == Guid.Empty) throw new ArgumentException("An avatar id is required.", nameof(avatarId));
            if (deviceId == Guid.Empty) throw new ArgumentException("A device id is required.", nameof(deviceId));
            _key = $"OASIS.Edge.OfflineSession.{avatarId:N}.{deviceId:N}";
        }

        public Task<OASISResult<bool>> SaveAsync(HyperDriveOfflineSessionGrant grant,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (grant == null)
                return Task.FromResult(Error<bool>("EDGE_SECURE_SESSION_GRANT_REQUIRED",
                    "A signed offline-session grant is required."));
            byte[] value = SecureSessionGrantJson.Serialize(grant);
            if (value.Length > MaximumCredentialBlobBytes)
                return Task.FromResult(Error<bool>("EDGE_WINDOWS_CREDENTIAL_TOO_LARGE",
                    $"The signed offline-session grant is {value.Length} bytes; Windows Credential Manager permits {MaximumCredentialBlobBytes}."));

            IntPtr blob = Marshal.AllocHGlobal(value.Length);
            try
            {
                if (value.Length != 0) Marshal.Copy(value, 0, blob, value.Length);
                var credential = new NativeCredential
                {
                    Type = GenericCredential,
                    TargetName = _key,
                    UserName = "OASIS Edge",
                    Persist = PersistLocalMachine,
                    CredentialBlob = blob,
                    CredentialBlobSize = (uint)value.Length
                };
                return Task.FromResult(CredWrite(ref credential, 0)
                    ? Success(true, saved: true)
                    : WinError<bool>("SAVE", Marshal.GetLastWin32Error()));
            }
            finally { Marshal.FreeHGlobal(blob); }
        }

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CredRead(_key, GenericCredential, 0, out IntPtr pointer))
            {
                int error = Marshal.GetLastWin32Error();
                return Task.FromResult(error == ErrorNotFound
                    ? new OASISResult<HyperDriveOfflineSessionGrant> { IsLoaded = true }
                    : WinError<HyperDriveOfflineSessionGrant>("LOAD", error));
            }

            try
            {
                var credential = Marshal.PtrToStructure<NativeCredential>(pointer);
                var bytes = new byte[credential.CredentialBlobSize];
                if (bytes.Length != 0) Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
                return Task.FromResult(SecureSessionGrantJson.Deserialize(bytes));
            }
            finally { CredFree(pointer); }
        }

        public Task<OASISResult<bool>> DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (CredDelete(_key, GenericCredential, 0)) return Task.FromResult(Success(true));
            int error = Marshal.GetLastWin32Error();
            return Task.FromResult(error == ErrorNotFound ? Success(true) : WinError<bool>("DELETE", error));
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NativeCredential
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(ref NativeCredential credential, uint flags);
        [DllImport("advapi32", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);
        [DllImport("advapi32", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, uint type, uint flags);
        [DllImport("advapi32")] private static extern void CredFree(IntPtr credential);

        private static OASISResult<T> WinError<T>(string operation, int code) =>
            Error<T>($"EDGE_WINDOWS_CREDENTIAL_{operation}_FAILED",
                $"Windows Credential Manager returned error {code}.");
        private static OASISResult<bool> Success(bool value, bool saved = false) =>
            new OASISResult<bool>(value) { IsSaved = saved };
        private static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
