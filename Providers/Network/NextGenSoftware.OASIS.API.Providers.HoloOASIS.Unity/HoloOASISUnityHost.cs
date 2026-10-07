using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity
{
    public sealed class HolochainConductorEndpoints
    {
        public string AdminWebSocketUri { get; set; }
        public string AppWebSocketUri { get; set; }
        public byte[] AppAuthenticationToken { get; set; }
        public string AgentPublicKey { get; set; }
        public string InstalledAppId { get; set; } = global::NextGenSoftware.OASIS.API.Providers.HoloOASIS.HoloOASIS.OASIS_HAPP_ID;
    }

    /// <summary>
    /// Platform boundary for a device-owned conductor. Android binds this to the official
    /// Holochain foreground-service runtime; desktop hosts may own a child process.
    /// </summary>
    public interface IHolochainConductorLifecycle : IAsyncDisposable
    {
        Task<OASISResult<HolochainConductorEndpoints>> StartAsync(CancellationToken cancellationToken);
        Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken);
        Task<OASISResult<HolochainConductorEndpoints>> ResumeAsync(CancellationToken cancellationToken);
        Task<OASISResult<bool>> StopAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Serializes Unity lifecycle transitions with provider activation. A suspended or
    /// stopped conductor can therefore never remain advertised as an active provider.
    /// </summary>
    public sealed class HoloOASISUnityHost : IAsyncDisposable
    {
        private readonly IHolochainConductorLifecycle _lifecycle;
        private readonly OASISDNA _oasisDNA;
        private readonly SemaphoreSlim _transitionLock = new SemaphoreSlim(1, 1);
        private bool _conductorStarted;
        private bool _disposed;

        public HoloOASIS Provider { get; private set; }

        public HoloOASISUnityHost(IHolochainConductorLifecycle lifecycle, OASISDNA oasisDNA = null)
        {
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _oasisDNA = oasisDNA;
        }

        public async Task<OASISResult<HoloOASIS>> StartAsync(CancellationToken cancellationToken = default)
        {
            await _transitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                if (Provider != null && Provider.IsProviderActivated) return Success(Provider);
                var conductor = await _lifecycle.StartAsync(cancellationToken).ConfigureAwait(false);
                if (conductor.IsError || conductor.Result == null)
                    return Error<HoloOASIS>("HOLO_CONDUCTOR_START_FAILED", conductor.Message);
                _conductorStarted = true;
                var endpointValidation = ValidateEndpoints(conductor.Result);
                if (endpointValidation.IsError)
                {
                    await StopConductorAsync().ConfigureAwait(false);
                    return Error<HoloOASIS>("HOLO_CONDUCTOR_ENDPOINT_INVALID", endpointValidation.Message);
                }
                Provider = CreateProvider(conductor.Result);
                var activation = await Provider.ActivateProviderAsync().ConfigureAwait(false);
                if (activation.IsError || !activation.Result)
                {
                    Provider = null;
                    await StopConductorAsync().ConfigureAwait(false);
                    return Error<HoloOASIS>("HOLO_PROVIDER_ACTIVATION_FAILED", activation.Message);
                }
                return Success(Provider);
            }
            finally { _transitionLock.Release(); }
        }

        public async Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken = default)
        {
            await _transitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                if (Provider != null && Provider.IsProviderActivated)
                {
                    var deactivation = await Provider.DeActivateProviderAsync().ConfigureAwait(false);
                    if (deactivation.IsError || !deactivation.Result)
                        return Error<bool>("HOLO_PROVIDER_DEACTIVATION_FAILED", deactivation.Message);
                }
                var suspended = await _lifecycle.SuspendAsync(cancellationToken).ConfigureAwait(false);
                return suspended.IsError || !suspended.Result
                    ? Error<bool>("HOLO_CONDUCTOR_SUSPEND_FAILED", suspended.Message) : Success(true);
            }
            finally { _transitionLock.Release(); }
        }

        public async Task<OASISResult<HoloOASIS>> ResumeAsync(CancellationToken cancellationToken = default)
        {
            await _transitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                var conductor = await _lifecycle.ResumeAsync(cancellationToken).ConfigureAwait(false);
                if (conductor.IsError || conductor.Result == null)
                    return Error<HoloOASIS>("HOLO_CONDUCTOR_RESUME_FAILED", conductor.Message);
                _conductorStarted = true;
                var endpointValidation = ValidateEndpoints(conductor.Result);
                if (endpointValidation.IsError)
                {
                    await StopConductorAsync().ConfigureAwait(false);
                    return Error<HoloOASIS>("HOLO_CONDUCTOR_ENDPOINT_INVALID", endpointValidation.Message);
                }
                Provider = CreateProvider(conductor.Result);
                var activation = await Provider.ActivateProviderAsync().ConfigureAwait(false);
                if (activation.IsError || !activation.Result)
                {
                    Provider = null;
                    await StopConductorAsync().ConfigureAwait(false);
                    return Error<HoloOASIS>("HOLO_PROVIDER_REACTIVATION_FAILED", activation.Message);
                }
                return Success(Provider);
            }
            finally { _transitionLock.Release(); }
        }

        public async ValueTask DisposeAsync()
        {
            await _transitionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed) return;
                if (Provider != null && Provider.IsProviderActivated)
                    await Provider.DeActivateProviderAsync().ConfigureAwait(false);
                await StopConductorAsync().ConfigureAwait(false);
                await _lifecycle.DisposeAsync().ConfigureAwait(false);
                Provider = null;
                _disposed = true;
            }
            finally
            {
                _transitionLock.Release();
                _transitionLock.Dispose();
            }
        }

        private HoloOASIS CreateProvider(HolochainConductorEndpoints endpoints)
        {
            if (!string.IsNullOrWhiteSpace(endpoints.AdminWebSocketUri))
                return new HoloOASIS(endpoints.AdminWebSocketUri, endpoints.AppWebSocketUri, _oasisDNA);

            var dna = new NextGenSoftware.Holochain.HoloNET.Client.HoloNETDNA
            {
                HolochainConductorAppAgentURI = endpoints.AppWebSocketUri,
                AppAuthenticationToken = endpoints.AppAuthenticationToken,
                AgentPubKey = endpoints.AgentPublicKey,
                InstalledAppId = endpoints.InstalledAppId,
                AutoStartHolochainConductor = false,
                AutoShutdownHolochainConductor = false
            };
            var appClient = new NextGenSoftware.Holochain.HoloNET.Client.HoloNETClientAppAgent(
                endpoints.InstalledAppId, endpoints.AgentPublicKey, dna);
            return new HoloOASIS(null, appClient, _oasisDNA);
        }

        private async Task StopConductorAsync()
        {
            if (!_conductorStarted) return;
            await _lifecycle.StopAsync(CancellationToken.None).ConfigureAwait(false);
            _conductorStarted = false;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HoloOASISUnityHost));
        }

        private static OASISResult<bool> ValidateEndpoints(HolochainConductorEndpoints endpoints)
        {
            if (endpoints == null)
                return Error<bool>("HOLO_CONDUCTOR_ENDPOINTS_REQUIRED", "The conductor returned no endpoints.");
            if (!IsWebSocketEndpoint(endpoints.AppWebSocketUri))
                return Error<bool>("HOLO_CONDUCTOR_APP_ENDPOINT_INVALID",
                    "The conductor app endpoint must be an absolute ws/wss URI.");
            if (!string.IsNullOrWhiteSpace(endpoints.AdminWebSocketUri))
            {
                if (!IsWebSocketEndpoint(endpoints.AdminWebSocketUri))
                    return Error<bool>("HOLO_CONDUCTOR_ADMIN_ENDPOINT_INVALID",
                        "The conductor admin endpoint must be an absolute ws/wss URI.");
            }
            else
            {
                if (endpoints.AppAuthenticationToken == null || endpoints.AppAuthenticationToken.Length == 0)
                    return Error<bool>("HOLO_CONDUCTOR_APP_TOKEN_REQUIRED",
                        "A service-provisioned app endpoint requires an authentication token.");
                if (string.IsNullOrWhiteSpace(endpoints.InstalledAppId))
                    return Error<bool>("HOLO_CONDUCTOR_APP_ID_REQUIRED",
                        "A service-provisioned app endpoint requires its installed app id.");
            }
            return Success(true);
        }

        private static bool IsWebSocketEndpoint(string value) =>
            Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == "ws" || uri.Scheme == "wss");

        private static OASISResult<T> Success<T>(T value) => new OASISResult<T>(value);
        private static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        {
            IsError = true,
            Message = string.IsNullOrWhiteSpace(message) ? code : $"{code}: {message}"
        };
    }
}
