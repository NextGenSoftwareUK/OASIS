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
    public sealed class UnityHolochainAndroidServiceBridge : IHolochainAndroidServiceBridge
    {
        private const string BridgeClass = "one.oasisomniverse.holooasis.unity.HolochainUnityBridge";
        private readonly SemaphoreSlim _operationLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        public Task<OASISResult<HolochainAndroidAppSession>> StartAndSetupAsync(
            HolochainAndroidRuntimeOptions options, CancellationToken cancellationToken) =>
            RunSessionOperationAsync("startAndSetup", cancellationToken, (bridge, callback) =>
            {
                using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                bridge.CallStatic("startAndSetup", activity, options.AppBundle, options.InstalledAppId,
                    options.NetworkSeed, options.BootstrapUrl, options.RelayUrl, callback);
            });

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
                return await completion.Task.ConfigureAwait(false);
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
