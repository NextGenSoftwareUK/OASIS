using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime;
using UnityEngine;

namespace NextGenSoftware.OASIS.Edge.Unity
{
    /// <summary>Stores signed offline grants only in the operating system's protected credential facility.</summary>
    public sealed class UnityPlatformSecureSessionStore : IEdgeSecureSessionStore
    {
        private readonly string _key;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private readonly WindowsCredentialSecureSessionStore _windowsStore;
#endif
        public UnityPlatformSecureSessionStore(Guid avatarId, Guid deviceId)
        {
            if (avatarId == Guid.Empty || deviceId == Guid.Empty)
                throw new ArgumentException("Avatar and device ids are required for secure-session isolation.");
            _key = $"OASIS.Edge.OfflineSession.{avatarId:N}.{deviceId:N}";
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _windowsStore = new WindowsCredentialSecureSessionStore(avatarId, deviceId);
#endif
        }

        public Task<OASISResult<bool>> SaveAsync(HyperDriveOfflineSessionGrant grant, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (grant == null) return Task.FromResult(Error<bool>("EDGE_SECURE_SESSION_GRANT_REQUIRED", "A signed offline grant is required."));
            return Task.FromResult(SaveProtected(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(grant))));
        }

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var loaded = LoadProtected();
            if (loaded.IsError) return Task.FromResult(Error<HyperDriveOfflineSessionGrant>(loaded.ErrorCode, loaded.Message));
            if (loaded.Result == null) return Task.FromResult(new OASISResult<HyperDriveOfflineSessionGrant> { IsLoaded = true });
            try
            {
                var grant = JsonConvert.DeserializeObject<HyperDriveOfflineSessionGrant>(Encoding.UTF8.GetString(loaded.Result));
                return Task.FromResult(new OASISResult<HyperDriveOfflineSessionGrant>(grant) { IsLoaded = true });
            }
            catch (JsonException ex)
            {
                return Task.FromResult(Error<HyperDriveOfflineSessionGrant>("EDGE_SECURE_SESSION_INVALID", $"The protected offline session is invalid: {ex.Message}"));
            }
        }

        public Task<OASISResult<bool>> DeleteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(DeleteProtected());
        }

        private OASISResult<bool> SaveProtected(byte[] value)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var bridge = new AndroidJavaClass("one.oasisomniverse.edge.SecureSessionStore");
                bridge.CallStatic("save", _key, Convert.ToBase64String(value));
                return Success();
            }
            catch (Exception ex) { return Error<bool>("EDGE_ANDROID_KEYSTORE_SAVE_FAILED", ex.Message); }
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
            int code = OasisEdgeKeychainSave(_key, Convert.ToBase64String(value));
            return code == 0 ? Success() : Error<bool>("EDGE_IOS_KEYCHAIN_SAVE_FAILED", $"Keychain returned status {code}.");
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return _windowsStore.SaveAsync(
                JsonConvert.DeserializeObject<HyperDriveOfflineSessionGrant>(Encoding.UTF8.GetString(value)),
                CancellationToken.None).GetAwaiter().GetResult();
#else
            return Error<bool>("EDGE_SECURE_STORE_UNSUPPORTED", "This Unity platform has no configured protected credential store.");
#endif
        }

        private OASISResult<byte[]> LoadProtected()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var bridge = new AndroidJavaClass("one.oasisomniverse.edge.SecureSessionStore");
                string value = bridge.CallStatic<string>("load", _key);
                return new OASISResult<byte[]>(string.IsNullOrEmpty(value) ? null : Convert.FromBase64String(value)) { IsLoaded = true };
            }
            catch (Exception ex) { return Error<byte[]>("EDGE_ANDROID_KEYSTORE_LOAD_FAILED", ex.Message); }
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
            IntPtr pointer = OasisEdgeKeychainLoad(_key);
            if (pointer == IntPtr.Zero) return new OASISResult<byte[]> { IsLoaded = true };
            try { return new OASISResult<byte[]>(Convert.FromBase64String(Marshal.PtrToStringAnsi(pointer))) { IsLoaded = true }; }
            catch (Exception ex) { return Error<byte[]>("EDGE_IOS_KEYCHAIN_LOAD_FAILED", ex.Message); }
            finally { OasisEdgeKeychainFree(pointer); }
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var loaded = _windowsStore.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            if (loaded.IsError) return Error<byte[]>(loaded.ErrorCode, loaded.Message);
            return new OASISResult<byte[]>(loaded.Result == null
                ? null
                : Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(loaded.Result))) { IsLoaded = true };
#else
            return Error<byte[]>("EDGE_SECURE_STORE_UNSUPPORTED", "This Unity platform has no configured protected credential store.");
#endif
        }

        private OASISResult<bool> DeleteProtected()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var bridge = new AndroidJavaClass("one.oasisomniverse.edge.SecureSessionStore");
                bridge.CallStatic("delete", _key);
                return Success();
            }
            catch (Exception ex) { return Error<bool>("EDGE_ANDROID_KEYSTORE_DELETE_FAILED", ex.Message); }
#elif (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
            int code = OasisEdgeKeychainDelete(_key);
            return code == 0 ? Success() : Error<bool>("EDGE_IOS_KEYCHAIN_DELETE_FAILED", $"Keychain returned status {code}.");
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return _windowsStore.DeleteAsync(CancellationToken.None).GetAwaiter().GetResult();
#else
            return Error<bool>("EDGE_SECURE_STORE_UNSUPPORTED", "This Unity platform has no configured protected credential store.");
#endif
        }

#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int OasisEdgeKeychainSave(string key, string value);
        [DllImport("__Internal")] private static extern IntPtr OasisEdgeKeychainLoad(string key);
        [DllImport("__Internal")] private static extern int OasisEdgeKeychainDelete(string key);
        [DllImport("__Internal")] private static extern void OasisEdgeKeychainFree(IntPtr value);
#endif

        private static OASISResult<bool> Success() => new OASISResult<bool>(true) { IsSaved = true };
        private static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };

#if false // Retired: Windows credential persistence now lives in Edge Runtime so Unity and NativeAOT games share one security implementation.
        private static class WindowsCredentialStore
        {
            private const uint Generic = 1, LocalMachine = 2;
            private const int NotFound = 1168;
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct Credential
            {
                public uint Flags, Type;
                public string TargetName, Comment;
                public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
                public uint CredentialBlobSize;
                public IntPtr CredentialBlob;
                public uint Persist, AttributeCount;
                public IntPtr Attributes;
                public string TargetAlias, UserName;
            }
            [DllImport("advapi32", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern bool CredWrite(ref Credential credential, uint flags);
            [DllImport("advapi32", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);
            [DllImport("advapi32", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern bool CredDelete(string target, uint type, uint flags);
            [DllImport("advapi32")] private static extern void CredFree(IntPtr credential);

            internal static OASISResult<bool> Save(string key, byte[] value)
            {
                IntPtr blob = Marshal.AllocHGlobal(value.Length);
                try
                {
                    Marshal.Copy(value, 0, blob, value.Length);
                    var credential = new Credential { Type = Generic, TargetName = key, UserName = "OASIS Edge", Persist = LocalMachine,
                        CredentialBlob = blob, CredentialBlobSize = (uint)value.Length };
                    return CredWrite(ref credential, 0) ? Success() : WinError<bool>("SAVE", Marshal.GetLastWin32Error());
                }
                finally { Marshal.FreeHGlobal(blob); }
            }

            internal static OASISResult<byte[]> Load(string key)
            {
                if (!CredRead(key, Generic, 0, out IntPtr pointer))
                {
                    int error = Marshal.GetLastWin32Error();
                    return error == NotFound ? new OASISResult<byte[]> { IsLoaded = true } : WinError<byte[]>("LOAD", error);
                }
                try
                {
                    var credential = Marshal.PtrToStructure<Credential>(pointer);
                    var bytes = new byte[credential.CredentialBlobSize];
                    if (bytes.Length != 0) Marshal.Copy(credential.CredentialBlob, bytes, 0, bytes.Length);
                    return new OASISResult<byte[]>(bytes) { IsLoaded = true };
                }
                finally { CredFree(pointer); }
            }

            internal static OASISResult<bool> Delete(string key)
            {
                if (CredDelete(key, Generic, 0) || Marshal.GetLastWin32Error() == NotFound) return Success();
                return WinError<bool>("DELETE", Marshal.GetLastWin32Error());
            }
            private static OASISResult<T> WinError<T>(string operation, int code) =>
                Error<T>($"EDGE_WINDOWS_CREDENTIAL_{operation}_FAILED", $"Windows Credential Manager returned error {code}.");
        }
#endif
    }
}
