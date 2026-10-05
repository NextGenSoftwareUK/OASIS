using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge
{
    /// <summary>
    /// Owns the mobile conductor session and exposes the Holochain replication target. Service
    /// setup and authenticated app connection form one transition: a failed connection is stopped
    /// before an error is returned, so no orphan foreground service remains.
    /// </summary>
    public sealed class HoloEdgeRuntimeHost : IAsyncDisposable
    {
        private readonly IHolochainAndroidServiceBridge _bridge;
        private readonly IHoloEdgeSessionFactory _sessionFactory;
        private readonly HolochainAndroidRuntimeOptions _options;
        private readonly SemaphoreSlim _transitionLock = new SemaphoreSlim(1, 1);
        private bool _started;
        private bool _suspended;
        private bool _disposed;
        private IHoloEdgeAppClient _client;
        private HolochainAndroidAppSession _session;

        public HoloEdgeMutationRepository Repository { get; private set; }

        public HoloEdgeRuntimeHost(IHolochainAndroidServiceBridge bridge,
            HolochainAndroidRuntimeOptions options, IHoloEdgeSessionFactory sessionFactory = null)
        {
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _sessionFactory = sessionFactory ?? new HoloNetEdgeSessionFactory();
        }

        public async Task<OASISResult<HoloEdgeMutationRepository>> StartAsync(
            CancellationToken cancellationToken = default)
        {
            await _transitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                if (_started) return Error("HOLO_EDGE_ALREADY_STARTED", "The Holo Edge host is already started.");
                var session = await _bridge.StartAndSetupAsync(_options, cancellationToken).ConfigureAwait(false);
                if (session == null || session.IsError || session.Result == null)
                    return Error(session?.ErrorCode ?? "HOLO_EDGE_START_FAILED", session?.Message ?? "The conductor returned no session.");
                _started = true;
                _session = session.Result;
                var client = await _sessionFactory.ConnectAsync(_options.InstalledAppId, session.Result, cancellationToken)
                    .ConfigureAwait(false);
                if (client == null || client.IsError || client.Result == null)
                {
                    var stopped = await _bridge.StopAsync(CancellationToken.None).ConfigureAwait(false);
                    _started = false;
                    if (stopped == null || stopped.IsError || !stopped.Result)
                        return Error("HOLO_EDGE_CONNECT_CLEANUP_FAILED", stopped?.Message ?? "The failed session could not be stopped.");
                    return Error(client?.ErrorCode ?? "HOLO_EDGE_CONNECT_FAILED", client?.Message ?? "The app interface did not connect.");
                }
                _client = client.Result;
                Repository = new HoloEdgeMutationRepository(_client);
                return new OASISResult<HoloEdgeMutationRepository>(Repository);
            }
            finally { _transitionLock.Release(); }
        }

        public async Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken = default)
        {
            await _transitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                if (!_started || _suspended) return ErrorBool("HOLO_EDGE_NOT_RUNNING", "The Holo Edge host is not running.");
                try
                {
                    await Repository.DetachClientAsync().ConfigureAwait(false);
                    _client = null;
                }
                catch (Exception error)
                {
                    return ErrorBool("HOLO_EDGE_DISCONNECT_FAILED", error.Message);
                }
                var suspended = await _bridge.SuspendAsync(cancellationToken).ConfigureAwait(false);
                if (suspended == null || suspended.IsError || !suspended.Result)
                {
                    var restored = await _sessionFactory.ConnectAsync(_options.InstalledAppId, _session,
                        CancellationToken.None).ConfigureAwait(false);
                    if (restored == null || restored.IsError || restored.Result == null)
                        return ErrorBool("HOLO_EDGE_SUSPEND_RESTORE_FAILED",
                            restored?.Message ?? "The app session could not be restored after suspend failed.");
                    await Repository.AttachClientAsync(restored.Result, CancellationToken.None).ConfigureAwait(false);
                    _client = restored.Result;
                    return ErrorBool(suspended?.ErrorCode ?? "HOLO_EDGE_SUSPEND_FAILED",
                        suspended?.Message ?? "The conductor did not suspend.");
                }
                _suspended = true;
                return new OASISResult<bool>(true);
            }
            finally { _transitionLock.Release(); }
        }

        public async Task<OASISResult<HoloEdgeMutationRepository>> ResumeAsync(
            CancellationToken cancellationToken = default)
        {
            await _transitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
                if (!_started || !_suspended) return Error("HOLO_EDGE_NOT_SUSPENDED", "The Holo Edge host is not suspended.");
                var session = await _bridge.ResumeAsync(_options.InstalledAppId, cancellationToken).ConfigureAwait(false);
                if (session == null || session.IsError || session.Result == null)
                    return Error(session?.ErrorCode ?? "HOLO_EDGE_RESUME_FAILED", session?.Message ?? "The conductor returned no resumed session.");
                var connected = await _sessionFactory.ConnectAsync(_options.InstalledAppId, session.Result, cancellationToken)
                    .ConfigureAwait(false);
                if (connected == null || connected.IsError || connected.Result == null)
                {
                    var stopped = await _bridge.StopAsync(CancellationToken.None).ConfigureAwait(false);
                    _started = false;
                    _suspended = false;
                    if (stopped == null || stopped.IsError || !stopped.Result)
                        return Error("HOLO_EDGE_RESUME_CLEANUP_FAILED",
                            stopped?.Message ?? "The failed resumed service could not be stopped.");
                    return Error(connected?.ErrorCode ?? "HOLO_EDGE_RECONNECT_FAILED",
                        connected?.Message ?? "The resumed app interface did not connect.");
                }
                await Repository.AttachClientAsync(connected.Result, cancellationToken).ConfigureAwait(false);
                _client = connected.Result;
                _session = session.Result;
                _suspended = false;
                return new OASISResult<HoloEdgeMutationRepository>(Repository);
            }
            finally { _transitionLock.Release(); }
        }

        public async ValueTask DisposeAsync()
        {
            await _transitionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed) return;
                if (_client != null)
                {
                    await Repository.DetachClientAsync().ConfigureAwait(false);
                    _client = null;
                }
                if (_started)
                {
                    var stopped = await _bridge.StopAsync(CancellationToken.None).ConfigureAwait(false);
                    if (stopped == null || stopped.IsError || !stopped.Result)
                        throw new InvalidOperationException(stopped?.Message ?? "The Holo Edge service could not be stopped.");
                    _started = false;
                }
                await _bridge.DisposeAsync().ConfigureAwait(false);
                Repository = null;
                _disposed = true;
            }
            finally
            {
                _transitionLock.Release();
                if (_disposed) _transitionLock.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HoloEdgeRuntimeHost));
        }

        private static OASISResult<HoloEdgeMutationRepository> Error(string code, string message) =>
            new OASISResult<HoloEdgeMutationRepository>
            {
                IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
            };

        private static OASISResult<bool> ErrorBool(string code, string message) => new OASISResult<bool>
        {
            IsError = true, ErrorCount = 1, ErrorCode = code, Message = message
        };
    }
}
