using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Edge.Runtime.Holochain;
using UnityEngine;

namespace NextGenSoftware.OASIS.Edge.Unity.Holochain
{
    /// <summary>
    /// Unity JNI implementation of the official Holochain Android foreground-service boundary.
    /// This source is copied into Runtime only for the HoloEnabled deployment profile.
    /// </summary>
    public sealed class UnityHolochainAndroidServiceBridge : IHolochainAndroidServiceBridge,
        IHolochainZomeCallSigningBridge
    {
        private const string BridgeClass = "one.oasisomniverse.holooasis.unity.HolochainUnityBridge";
        private readonly SemaphoreSlim _operationLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        /// <summary>
        /// Shared-runtime progress ("awaiting_approval", "shared", "fallback", "shared_unavailable"), e.g.
        /// to tell the player to approve Holochain access in the notification shade.
        /// </summary>
        public event Action<string, string> StatusChanged;

        public Task<OASISResult<HolochainAndroidAppSession>> StartAndSetupAsync(
            HolochainAndroidRuntimeOptions options, CancellationToken cancellationToken) =>
            RunSessionOperationAsync("startAndSetup", cancellationToken, (bridge, callback) =>
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                bridge.CallStatic("startAndSetup", activity, options.AppBundle, options.InstalledAppId,
                    options.NetworkSeed, options.BootstrapUrl, options.RelayUrl,
                    options.PreferSharedRuntime, options.SharedRuntimeApprovalTimeoutMs,
                    new StatusCallback(this), callback);
            });

        public async Task<OASISResult<HolochainSignedZomeCall>> SignZomeCallAsync(
            HolochainZomeCallToSign call, CancellationToken cancellationToken)
        {
            // Not serialized with the lifecycle lock: signing runs alongside normal zome traffic.
            cancellationToken.ThrowIfCancellationRequested();
            if (call == null) return Error<HolochainSignedZomeCall>("HOLO_ANDROID_SIGN_INVALID", "No zome call to sign.");
            try
            {
                ThrowIfDisposed();
#if UNITY_ANDROID && !UNITY_EDITOR
                using var bridge = new AndroidJavaClass(BridgeClass);
                var completion = new TaskCompletionSource<OASISResult<HolochainSignedZomeCall>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                bridge.CallStatic("signZomeCall", B64(call.Provenance), B64(call.DnaHash), B64(call.AgentPubKey),
                    call.ZomeName, call.FnName, call.CapSecret == null ? "" : B64(call.CapSecret),
                    B64(call.Payload), B64(call.Nonce), call.ExpiresAt, new SignCallback(completion));
                return await completion.Task.ConfigureAwait(false);
#else
                await Task.CompletedTask.ConfigureAwait(false);
                return Error<HolochainSignedZomeCall>("HOLO_ANDROID_PLATFORM_REQUIRED",
                    "Zome-call signing through the Android service requires an Android player.");
#endif
            }
            catch (Exception error) when (!(error is OperationCanceledException))
            {
                return Error<HolochainSignedZomeCall>("HOLO_ANDROID_JNI_FAILED",
                    $"The Android Holochain 'signZomeCall' IPC call failed: {error.Message}");
            }
        }

        private static string B64(byte[] value) => Convert.ToBase64String(value ?? Array.Empty<byte>());

        public Task<OASISResult<HolochainAndroidAppSession>> ResumeAsync(
            string installedAppId, CancellationToken cancellationToken) =>
            RunSessionOperationAsync("resume", cancellationToken,
                (bridge, callback) => bridge.CallStatic("resume", installedAppId, callback));

        public Task<OASISResult<bool>> SuspendAsync(CancellationToken cancellationToken) =>
            RunOperationAsync("suspend", cancellationToken,
                (bridge, callback) => bridge.CallStatic("suspend", callback));

        public Task<OASISResult<bool>> StopAsync(CancellationToken cancellationToken) =>
            RunOperationAsync("stop", cancellationToken,
                (bridge, callback) => bridge.CallStatic("stop", callback));

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            await _operationLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_disposed) return;
#if UNITY_ANDROID && !UNITY_EDITOR
                using var bridge = new AndroidJavaClass(BridgeClass);
                var completion = new TaskCompletionSource<OASISResult<bool>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                bridge.CallStatic("dispose", new OperationCallback(completion));
                var result = await completion.Task.ConfigureAwait(false);
                if (result.IsError || !result.Result)
                    throw new InvalidOperationException(result.Message ?? "The Android Holochain bridge could not be disposed.");
#endif
                _disposed = true;
            }
            finally
            {
                _operationLock.Release();
                if (_disposed) _operationLock.Dispose();
            }
        }

        private async Task<OASISResult<HolochainAndroidAppSession>> RunSessionOperationAsync(
            string operation, CancellationToken cancellationToken,
            Action<AndroidJavaClass, SessionCallback> invoke)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
#if UNITY_ANDROID && !UNITY_EDITOR
                using var bridge = new AndroidJavaClass(BridgeClass);
                var completion = new TaskCompletionSource<OASISResult<HolochainAndroidAppSession>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                invoke(bridge, new SessionCallback(completion));
                // Once IPC owns the operation, await its terminal callback. Abandoning the task on
                // cancellation would leak an unowned foreground service or installed application.
                var session = await completion.Task.ConfigureAwait(false);
                if (!session.IsError && session.Result != null)
                    session.Result.Mode = bridge.CallStatic<string>("getMode");
                return session;
#else
                return Error<HolochainAndroidAppSession>("HOLO_ANDROID_PLATFORM_REQUIRED",
                    $"The Holochain Android '{operation}' operation requires an Android player.");
#endif
            }
            catch (Exception error) when (!(error is OperationCanceledException))
            {
                return Error<HolochainAndroidAppSession>("HOLO_ANDROID_JNI_FAILED",
                    $"The Android Holochain '{operation}' IPC call failed: {error.Message}");
            }
            finally { _operationLock.Release(); }
        }

        private async Task<OASISResult<bool>> RunOperationAsync(string operation,
            CancellationToken cancellationToken, Action<AndroidJavaClass, OperationCallback> invoke)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _operationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                ThrowIfDisposed();
#if UNITY_ANDROID && !UNITY_EDITOR
                using var bridge = new AndroidJavaClass(BridgeClass);
                var completion = new TaskCompletionSource<OASISResult<bool>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                invoke(bridge, new OperationCallback(completion));
                return await completion.Task.ConfigureAwait(false);
#else
                return Error<bool>("HOLO_ANDROID_PLATFORM_REQUIRED",
                    $"The Holochain Android '{operation}' operation requires an Android player.");
#endif
            }
            catch (Exception error) when (!(error is OperationCanceledException))
            {
                return Error<bool>("HOLO_ANDROID_JNI_FAILED",
                    $"The Android Holochain '{operation}' IPC call failed: {error.Message}");
            }
            finally { _operationLock.Release(); }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(UnityHolochainAndroidServiceBridge));
        }

        private static OASISResult<T> Error<T>(string code, string message) => new OASISResult<T>
        {
            IsError = true,
            ErrorCount = 1,
            ErrorCode = code,
            Message = message
        };

        private sealed class SessionCallback : AndroidJavaProxy
        {
            private readonly TaskCompletionSource<OASISResult<HolochainAndroidAppSession>> _completion;

            internal SessionCallback(TaskCompletionSource<OASISResult<HolochainAndroidAppSession>> completion)
                : base("one.oasisomniverse.holooasis.unity.HolochainUnityBridge$SessionCallback") =>
                _completion = completion;

            public void onSuccess(int port, string authenticationTokenBase64)
            {
                try
                {
                    _completion.TrySetResult(new OASISResult<HolochainAndroidAppSession>(
                        new HolochainAndroidAppSession
                        {
                            Port = port,
                            AuthenticationToken = Convert.FromBase64String(authenticationTokenBase64)
                        }));
                }
                catch (FormatException error)
                {
                    _completion.TrySetResult(Error<HolochainAndroidAppSession>(
                        "HOLO_ANDROID_APP_TOKEN_INVALID", error.Message));
                }
            }

            public void onError(string errorCode, string message) =>
                _completion.TrySetResult(Error<HolochainAndroidAppSession>(errorCode, message));
        }

        private sealed class StatusCallback : AndroidJavaProxy
        {
            private readonly UnityHolochainAndroidServiceBridge _owner;

            internal StatusCallback(UnityHolochainAndroidServiceBridge owner)
                : base("one.oasisomniverse.holooasis.unity.HolochainUnityBridge$StatusCallback") => _owner = owner;

            public void onStatus(string status, string message) => _owner.StatusChanged?.Invoke(status, message);
        }

        private sealed class SignCallback : AndroidJavaProxy
        {
            private readonly TaskCompletionSource<OASISResult<HolochainSignedZomeCall>> _completion;

            internal SignCallback(TaskCompletionSource<OASISResult<HolochainSignedZomeCall>> completion)
                : base("one.oasisomniverse.holooasis.unity.HolochainUnityBridge$SignCallback") =>
                _completion = completion;

            public void onSuccess(string bytesBase64, string signatureBase64)
            {
                try
                {
                    _completion.TrySetResult(new OASISResult<HolochainSignedZomeCall>(new HolochainSignedZomeCall
                    {
                        Bytes = Convert.FromBase64String(bytesBase64),
                        Signature = Convert.FromBase64String(signatureBase64)
                    }));
                }
                catch (FormatException error)
                {
                    _completion.TrySetResult(Error<HolochainSignedZomeCall>("HOLO_ANDROID_SIGNATURE_INVALID", error.Message));
                }
            }

            public void onError(string errorCode, string message) =>
                _completion.TrySetResult(Error<HolochainSignedZomeCall>(errorCode, message));
        }

        private sealed class OperationCallback : AndroidJavaProxy
        {
            private readonly TaskCompletionSource<OASISResult<bool>> _completion;

            internal OperationCallback(TaskCompletionSource<OASISResult<bool>> completion)
                : base("one.oasisomniverse.holooasis.unity.HolochainUnityBridge$OperationCallback") =>
                _completion = completion;

            public void onSuccess() => _completion.TrySetResult(new OASISResult<bool>(true));
            public void onError(string errorCode, string message) =>
                _completion.TrySetResult(Error<bool>(errorCode, message));
        }
    }
}
