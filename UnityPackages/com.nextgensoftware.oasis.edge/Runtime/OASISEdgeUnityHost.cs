using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime;
using NextGenSoftware.OASIS.STARAPI.Client.Edge;
using UnityEngine;

namespace NextGenSoftware.OASIS.Edge.Unity
{
    /// <summary>Unity lifecycle binding for the platform-neutral OGEngineClient Edge host.</summary>
    public sealed class OASISEdgeUnityHost : MonoBehaviour
    {
        private OGEngineEdgeClient _client;
        private UnityEdgeConnectivityMonitor _connectivity;
        private bool _disposing;
        private readonly SemaphoreSlim _lifecycleGate = new SemaphoreSlim(1, 1);

        public OGEngineEdgeClient Client => _client;
        public EdgeRuntimeStatus Status => _client == null ? null : _client.Status;
        public bool IsInitialized => _client != null && _client.IsInitialized;
        public event EventHandler<EdgeRuntimeStatus> StatusChanged;

        public async Task InitializeAsync(Uri hostedOnodeBaseAddress, string bearerToken,
            Guid avatarId, Guid stableDeviceId, string databasePath,
            IEdgeSecureSessionStore secureSessionStore = null,
            IEdgeOfflineGrantValidator offlineGrantValidator = null)
        {
            EnsureNotInitialized();
            _client = new OGEngineEdgeClient();
            _client.StatusChanged += ClientOnStatusChanged;
            _connectivity = new UnityEdgeConnectivityMonitor();
            var started = await _client.InitializeOnlineAsync(hostedOnodeBaseAddress, bearerToken,
                avatarId, stableDeviceId, databasePath, _connectivity, secureSessionStore,
                offlineGrantValidator);
            if (started == null || (started.IsError && !IsExpectedNetworkAbsence(started.ErrorCode)))
            {
                string message = started == null
                    ? "OGEngineClient connectivity initialization returned no result."
                    : started.Message;
                await DisposeEndpointAsync();
                throw new InvalidOperationException(message);
            }
        }

        public async Task<OASISResult<HyperDriveOfflineSessionGrant>> InitializeOfflineAsync(
            Uri hostedOnodeBaseAddress, Guid avatarId, Guid stableDeviceId, string databasePath,
            IEdgeSecureSessionStore secureSessionStore, IEdgeOfflineGrantValidator offlineGrantValidator,
            IReadOnlyList<string> requiredScopes)
        {
            EnsureNotInitialized();
            _client = new OGEngineEdgeClient();
            _client.StatusChanged += ClientOnStatusChanged;
            _connectivity = new UnityEdgeConnectivityMonitor();
            var resumed = await _client.InitializeOfflineAsync(hostedOnodeBaseAddress, avatarId,
                stableDeviceId, databasePath, _connectivity, secureSessionStore, offlineGrantValidator,
                requiredScopes ?? Array.Empty<string>());
            if (resumed == null || resumed.IsError || resumed.Result == null)
            {
                await DisposeEndpointAsync();
                return resumed ?? Error<HyperDriveOfflineSessionGrant>("OGENGINE_OFFLINE_SESSION_RESUME_FAILED",
                    "OGEngineClient returned no offline session result.");
            }
            return resumed;
        }

        public Task<OASISResult<HyperDriveOfflineSessionGrant>> AuthenticateHostedSessionAsync(
            string bearerToken, IReadOnlyList<string> offlineScopes, int offlineLifetimeMinutes) =>
            _client == null
                ? Task.FromResult(Error<HyperDriveOfflineSessionGrant>("OGENGINE_EDGE_NOT_INITIALIZED",
                    "Initialize OGEngineClient before attaching a hosted session."))
                : _client.AttachHostedSessionAsync(bearerToken, offlineScopes, offlineLifetimeMinutes);

        private void Update() => _connectivity?.Poll();

        private async void OnApplicationPause(bool paused)
        {
            if (_client == null || _disposing) return;
            await _lifecycleGate.WaitAsync();
            try
            {
                if (_client == null || _disposing) return;
                var result = paused ? await _client.SuspendAsync() : await _client.ResumeAsync();
                if (result != null && result.IsError)
                    Debug.LogError($"[OGEngineClient Edge] {result.ErrorCode}: {result.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OGEngineClient Edge] Lifecycle transition failed: {ex.Message}");
            }
            finally { _lifecycleGate.Release(); }
        }

        private void ClientOnStatusChanged(object sender, EdgeRuntimeStatus status) =>
            StatusChanged?.Invoke(this, status);

        private async void OnDestroy()
        {
            try { await DisposeEndpointAsync(); }
            catch (Exception ex) { Debug.LogError($"[OGEngineClient Edge] Shutdown failed: {ex.Message}"); }
        }

        public async Task DisposeEndpointAsync()
        {
            if (_disposing) return;
            _disposing = true;
            await _lifecycleGate.WaitAsync();
            try
            {
                if (_client != null)
                {
                    _client.StatusChanged -= ClientOnStatusChanged;
                    await _client.DisposeAsync();
                    _client = null;
                }
                _connectivity = null;
            }
            finally
            {
                _lifecycleGate.Release();
                _disposing = false;
            }
        }

        private void EnsureNotInitialized()
        {
            if (_client != null)
                throw new InvalidOperationException("The Unity OGEngineClient host is already initialized.");
        }

        private static bool IsExpectedNetworkAbsence(string code) =>
            code == "HYPERDRIVE_NETWORK_UNAVAILABLE" || code == "HYPERDRIVE_NETWORK_TIMEOUT" ||
            code == "HYPERDRIVE_REMOTE_UNAVAILABLE";

        private static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    }
}
