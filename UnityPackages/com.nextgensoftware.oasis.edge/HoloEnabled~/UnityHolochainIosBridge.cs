using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;

namespace NextGenSoftware.OASIS.Edge.Unity.Holochain
{
    /// <summary>
    /// Unity iOS implementation of the Holochain service boundary. The conductor runs in-process from
    /// HolochainConductor.xcframework (Holochain 0.7, wasmi interpreter: iOS forbids JIT) through the C ABI in
    /// HolochainUnityBridge.swift. iOS has no long-lived background service, so the conductor runs while the
    /// app is active; Suspend keeps it (iOS freezes the process), and Resume re-issues the app websocket.
    /// This source is copied into Runtime only for the HoloEnabled deployment profile.
    /// </summary>
    public sealed class UnityHolochainIosBridge : IHolochainAndroidServiceBridge, IHolochainZomeCallSigningBridge
    {
        private delegate void SessionCallback(int requestId, int port, IntPtr tokenBase64, IntPtr errorCode, IntPtr message);
        private delegate void OperationCallback(int requestId, bool ok, IntPtr errorCode, IntPtr message);
        private delegate void SignCallback(int requestId, IntPtr bytesBase64, IntPtr signatureBase64, IntPtr errorCode, IntPtr message);

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void holo_ios_start_and_setup(int requestId, byte[] appBundle, int appBundleLength,
            string installedAppId, string networkSeed, string bootstrapUrl, string relayUrl, SessionCallback callback);
        [DllImport("__Internal")]
        private static extern void holo_ios_resume(int requestId, string installedAppId, SessionCallback callback);
        [DllImport("__Internal")]
        private static extern void holo_ios_sign_zome_call(int requestId, string provenance, string dnaHash,
            string agentPubKey, string zomeName, string fnName, string capSecret, string payload, string nonce,
            long expiresAt, SignCallback callback);
        [DllImport("__Internal")]
        private static extern void holo_ios_stop(int requestId, OperationCallback callback);
#endif

        // IL2CPP needs static callbacks; pending requests are matched by id.
        private static readonly ConcurrentDictionary<int, TaskCompletionSource<OASISResult<HolochainAndroidAppSession>>> Sessions =
            new ConcurrentDictionary<int, TaskCompletionSource<OASISResult<HolochainAndroidAppSession>>>();
        private static readonly ConcurrentDictionary<int, TaskCompletionSource<OASISResult<bool>>> Operations =
            new ConcurrentDictionary<int, TaskCompletionSource<OASISResult<bool>>>();
        private static readonly ConcurrentDictionary<int, TaskCompletionSource<OASISResult<HolochainSignedZomeCall>>> Signs =
            new ConcurrentDictionary<int, TaskCompletionSource<OASISResult<HolochainSignedZomeCall>>>();
        private static int _nextRequestId;

        private readonly SemaphoreSlim _operationLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public Task<OASISResult<HolochainAndroidAppSession>> StartAndSetupAsync(
            HolochainAndroidRuntimeOptions options, CancellationToken cancellationToken) =>
            RunSessionAsync("startAndSetup", cancellationToken, id =>
            {
#if UNITY_IOS && !UNITY_EDITOR
                holo_ios_start_and_setup(id, options.AppBundle, options.AppBundle?.Length ?? 0, options.InstalledAppId,
                    options.NetworkSeed, options.BootstrapUrl, options.RelayUrl, OnSession);
#endif
            });

        public Task<OASISResult<HolochainAndroidAppSession>> ResumeAsync(
            string installedAppId, CancellationToken cancellationToken) =>
            RunSessionAsync("resume", cancellationToken, id =>
            {
#if UNITY_IOS && !UNITY_EDITOR
                holo_ios_resume(id, installedAppId, OnSession);
#endif
            });

        /// <summary>iOS freezes the process in the background; the conductor resumes with it.</summary>
        public Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return Task.FromResult(new OASISResult<bool>(true));
        }

        public async Task<OASISResult<bool>> StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
#if UNITY_IOS && !UNITY_EDITOR
                var id = Interlocked.Increment(ref _nextRequestId);
                var completion = new TaskCompletionSource<OASISResult<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
                Operations[id] = completion;
                holo_ios_stop(id, OnOperation);
                return await completion.Task.ConfigureAwait(false);
#else
                return Error<bool>("HOLO_IOS_PLATFORM_REQUIRED", "The Holochain iOS 'stop' operation requires an iOS player.");
#endif
            }
            finally { _operationLock.Release(); }
        }

        public async Task<OASISResult<HolochainSignedZomeCall>> SignZomeCallAsync(
            HolochainZomeCallToSign call, CancellationToken cancellationToken)
        {
            // Not serialized with the lifecycle lock: signing runs alongside normal zome traffic.
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (call == null) return Error<HolochainSignedZomeCall>("HOLO_IOS_SIGN_INVALID", "No zome call to sign.");
#if UNITY_IOS && !UNITY_EDITOR
            var id = Interlocked.Increment(ref _nextRequestId);
            var completion = new TaskCompletionSource<OASISResult<HolochainSignedZomeCall>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Signs[id] = completion;
            holo_ios_sign_zome_call(id, B64(call.Provenance), B64(call.DnaHash), B64(call.AgentPubKey), call.ZomeName,
                call.FnName, call.CapSecret == null ? "" : B64(call.CapSecret), B64(call.Payload), B64(call.Nonce),
                call.ExpiresAt, OnSign);
            return await completion.Task.ConfigureAwait(false);
#else
            await Task.CompletedTask.ConfigureAwait(false);
            return Error<HolochainSignedZomeCall>("HOLO_IOS_PLATFORM_REQUIRED",
                "Zome-call signing through the iOS conductor requires an iOS player.");
#endif
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            var stopped = await StopAsync(CancellationToken.None).ConfigureAwait(false);
            if (stopped.IsError && stopped.ErrorCode != "HOLO_IOS_PLATFORM_REQUIRED")
                throw new InvalidOperationException(stopped.Message ?? "The iOS Holochain conductor could not be stopped.");
            _disposed = true;
            _operationLock.Dispose();
        }

        private async Task<OASISResult<HolochainAndroidAppSession>> RunSessionAsync(string operation,
            CancellationToken cancellationToken, Action<int> invoke)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
#if UNITY_IOS && !UNITY_EDITOR
                var id = Interlocked.Increment(ref _nextRequestId);
                var completion = new TaskCompletionSource<OASISResult<HolochainAndroidAppSession>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                Sessions[id] = completion;
                invoke(id);
                // Once native code owns the operation, await its terminal callback (as on Android).
                var session = await completion.Task.ConfigureAwait(false);
                if (!session.IsError && session.Result != null) session.Result.Mode = "embedded";
                return session;
#else
                await Task.CompletedTask.ConfigureAwait(false);
                return Error<HolochainAndroidAppSession>("HOLO_IOS_PLATFORM_REQUIRED",
                    $"The Holochain iOS '{operation}' operation requires an iOS player.");
#endif
            }
            catch (Exception error) when (!(error is OperationCanceledException))
            {
                return Error<HolochainAndroidAppSession>("HOLO_IOS_NATIVE_FAILED",
                    $"The iOS Holochain '{operation}' call failed: {error.Message}");
            }
            finally { _operationLock.Release(); }
        }

        [AOT.MonoPInvokeCallback(typeof(SessionCallback))]
        private static void OnSession(int requestId, int port, IntPtr tokenBase64, IntPtr errorCode, IntPtr message)
        {
            if (!Sessions.TryRemove(requestId, out var completion)) return;
            if (errorCode != IntPtr.Zero)
            {
                completion.TrySetResult(Error<HolochainAndroidAppSession>(Marshal.PtrToStringAnsi(errorCode),
                    Marshal.PtrToStringAnsi(message)));
                return;
            }
            try
            {
                completion.TrySetResult(new OASISResult<HolochainAndroidAppSession>(new HolochainAndroidAppSession
                {
                    Port = port,
                    AuthenticationToken = Convert.FromBase64String(Marshal.PtrToStringAnsi(tokenBase64) ?? "")
                }));
            }
            catch (FormatException error)
            {
                completion.TrySetResult(Error<HolochainAndroidAppSession>("HOLO_IOS_APP_TOKEN_INVALID", error.Message));
            }
        }

        [AOT.MonoPInvokeCallback(typeof(OperationCallback))]
        private static void OnOperation(int requestId, bool ok, IntPtr errorCode, IntPtr message)
        {
            if (!Operations.TryRemove(requestId, out var completion)) return;
            completion.TrySetResult(ok
                ? new OASISResult<bool>(true)
                : Error<bool>(Marshal.PtrToStringAnsi(errorCode), Marshal.PtrToStringAnsi(message)));
        }

        [AOT.MonoPInvokeCallback(typeof(SignCallback))]
        private static void OnSign(int requestId, IntPtr bytesBase64, IntPtr signatureBase64, IntPtr errorCode, IntPtr message)
        {
            if (!Signs.TryRemove(requestId, out var completion)) return;
            if (errorCode != IntPtr.Zero)
            {
                completion.TrySetResult(Error<HolochainSignedZomeCall>(Marshal.PtrToStringAnsi(errorCode),
                    Marshal.PtrToStringAnsi(message)));
                return;
            }
            try
            {
                completion.TrySetResult(new OASISResult<HolochainSignedZomeCall>(new HolochainSignedZomeCall
                {
                    Bytes = Convert.FromBase64String(Marshal.PtrToStringAnsi(bytesBase64) ?? ""),
                    Signature = Convert.FromBase64String(Marshal.PtrToStringAnsi(signatureBase64) ?? "")
                }));
            }
            catch (FormatException error)
            {
                completion.TrySetResult(Error<HolochainSignedZomeCall>("HOLO_IOS_SIGNATURE_INVALID", error.Message));
            }
        }

        private static string B64(byte[] value) => Convert.ToBase64String(value ?? Array.Empty<byte>());

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnityHolochainIosBridge));
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
