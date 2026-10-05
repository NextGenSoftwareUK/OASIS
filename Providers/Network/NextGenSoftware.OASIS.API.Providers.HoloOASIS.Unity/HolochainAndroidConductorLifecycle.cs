using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity
{
    /// <summary>
    /// Android implementation of the platform-neutral conductor lifecycle. It deliberately has no
    /// admin-WebSocket path: administration belongs to the same-package Android IPC service.
    /// </summary>
    public sealed class HolochainAndroidConductorLifecycle : IHolochainConductorLifecycle
    {
        private readonly IHolochainAndroidServiceBridge _bridge;
        private readonly HolochainAndroidRuntimeOptions _options;
        private bool _started;
        private bool _disposed;

        public HolochainAndroidConductorLifecycle(IHolochainAndroidServiceBridge bridge,
            HolochainAndroidRuntimeOptions options)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            ValidateOptions(options);
        }

        public async Task<OASISResult<HolochainConductorEndpoints>> StartAsync(
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (_started)
                return Error<HolochainConductorEndpoints>("HOLO_ANDROID_ALREADY_STARTED",
                    "The Android Holochain service is already started; use ResumeAsync after suspension.");

            var started = await _bridge.StartAndSetupAsync(_options, cancellationToken).ConfigureAwait(false);
            var endpoints = ToEndpoints(started, "HOLO_ANDROID_START_FAILED");
            if (!endpoints.IsError)
            {
                _started = true;
                return endpoints;
            }
            // StartAndSetupAsync owns one atomic service-start operation. If it returned a session
            // that violates the contract, undo that start before exposing the error to the host.
            if (started != null && !started.IsError && started.Result != null)
            {
                var stopped = await _bridge.StopAsync(CancellationToken.None).ConfigureAwait(false);
                if (stopped == null || stopped.IsError || !stopped.Result)
                    return Error<HolochainConductorEndpoints>("HOLO_ANDROID_INVALID_SESSION_CLEANUP_FAILED",
                        stopped?.Message ?? "The invalid Android app session could not be stopped.");
            }
            return endpoints;
        }

        public async Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (!_started)
                return Error<bool>("HOLO_ANDROID_NOT_STARTED", "The Android Holochain service is not started.");
            return await _bridge.SuspendAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<OASISResult<HolochainConductorEndpoints>> ResumeAsync(
            CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (!_started)
                return Error<HolochainConductorEndpoints>("HOLO_ANDROID_NOT_STARTED",
                    "The Android Holochain service is not started.");
            var resumed = await _bridge.ResumeAsync(_options.InstalledAppId, cancellationToken)
                .ConfigureAwait(false);
            return ToEndpoints(resumed, "HOLO_ANDROID_RESUME_FAILED");
        }

        public async Task<OASISResult<bool>> StopAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (!_started) return new OASISResult<bool>(true);
            var stopped = await _bridge.StopAsync(cancellationToken).ConfigureAwait(false);
            if (stopped != null && !stopped.IsError && stopped.Result) _started = false;
            return stopped ?? Error<bool>("HOLO_ANDROID_STOP_FAILED", "The Android service bridge returned no result.");
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            if (_started)
            {
                var stopped = await _bridge.StopAsync(CancellationToken.None).ConfigureAwait(false);
                if (stopped == null || stopped.IsError || !stopped.Result)
                    throw new InvalidOperationException(stopped?.Message ?? "The Android Holochain service could not be stopped.");
                _started = false;
            }
            await _bridge.DisposeAsync().ConfigureAwait(false);
            _disposed = true;
        }

        private OASISResult<HolochainConductorEndpoints> ToEndpoints(
            OASISResult<HolochainAndroidAppSession> result, string errorCode)
        {
            if (result == null || result.IsError || result.Result == null)
                return Error<HolochainConductorEndpoints>(result?.ErrorCode ?? errorCode,
                    result?.Message ?? "The Android service returned no authenticated app session.");
            if (result.Result.Port < 1 || result.Result.Port > 65535)
                return Error<HolochainConductorEndpoints>("HOLO_ANDROID_APP_PORT_INVALID",
                    "The Android service returned an invalid app-interface port.");
            if (result.Result.AuthenticationToken == null || result.Result.AuthenticationToken.Length == 0)
                return Error<HolochainConductorEndpoints>("HOLO_ANDROID_APP_TOKEN_REQUIRED",
                    "The Android service returned no app-interface authentication token.");

            return new OASISResult<HolochainConductorEndpoints>(new HolochainConductorEndpoints
            {
                AppWebSocketUri = $"ws://127.0.0.1:{result.Result.Port}",
                AppAuthenticationToken = result.Result.AuthenticationToken,
                InstalledAppId = _options.InstalledAppId
            });
        }

        private static void ValidateOptions(HolochainAndroidRuntimeOptions options)
        {
            if (options.AppBundle == null || options.AppBundle.Length == 0)
                throw new ArgumentException("The packaged Holochain app bundle is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(options.InstalledAppId))
                throw new ArgumentException("The installed Holochain app id is required.", nameof(options));
            ValidateHttpsUrl(options.BootstrapUrl, nameof(options.BootstrapUrl));
            ValidateHttpsUrl(options.RelayUrl, nameof(options.RelayUrl));
        }

        private static void ValidateHttpsUrl(string value, string name)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException($"{name} must be an absolute HTTPS URL.", name);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HolochainAndroidConductorLifecycle));
        }

        private static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        {
            IsError = true,
            ErrorCount = 1,
            ErrorCode = code,
            Message = message
        };
    }
}
