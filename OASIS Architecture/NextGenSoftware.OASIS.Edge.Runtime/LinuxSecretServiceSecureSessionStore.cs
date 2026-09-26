using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>
    /// Linux Secret Service adapter through libsecret's official <c>secret-tool</c> client. The grant
    /// is supplied over stdin, never as a command-line argument. Missing Secret Service support is a
    /// visible configuration error and is never replaced by file storage.
    /// </summary>
    public sealed class LinuxSecretServiceSecureSessionStore : IEdgeSecureSessionStore
    {
        private readonly string _account;

        public LinuxSecretServiceSecureSessionStore(Guid avatarId, Guid deviceId)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                throw new PlatformNotSupportedException("Linux Secret Service is available only on Linux.");
            if (avatarId == Guid.Empty || deviceId == Guid.Empty)
                throw new ArgumentException("Avatar and device ids are required for secure-session isolation.");
            _account = $"{avatarId:N}.{deviceId:N}";
        }

        public async Task<OASISResult<bool>> SaveAsync(HyperDriveOfflineSessionGrant grant,
            CancellationToken cancellationToken)
        {
            if (grant == null) return SecureSessionGrantJson.Error<bool>("EDGE_SECURE_SESSION_GRANT_REQUIRED",
                "A signed offline-session grant is required.");
            string secret = Convert.ToBase64String(SecureSessionGrantJson.Serialize(grant));
            var result = await RunAsync(new[] { "store", "--label=OASIS Edge Offline Session",
                "service", "one.oasisomniverse.edge", "account", _account }, secret, cancellationToken)
                .ConfigureAwait(false);
            return result.IsError ? SecureSessionGrantJson.Error<bool>(result.ErrorCode, result.Message)
                : new OASISResult<bool>(true) { IsSaved = true };
        }

        public async Task<OASISResult<HyperDriveOfflineSessionGrant>> LoadAsync(CancellationToken cancellationToken)
        {
            var result = await RunAsync(new[] { "lookup", "service", "one.oasisomniverse.edge", "account", _account },
                null, cancellationToken, notFoundExitCode: 1).ConfigureAwait(false);
            if (result.IsError) return SecureSessionGrantJson.Error<HyperDriveOfflineSessionGrant>(result.ErrorCode, result.Message);
            if (string.IsNullOrWhiteSpace(result.Result))
                return new OASISResult<HyperDriveOfflineSessionGrant> { IsLoaded = true };
            try { return SecureSessionGrantJson.Deserialize(Convert.FromBase64String(result.Result.Trim())); }
            catch (FormatException ex) { return SecureSessionGrantJson.Error<HyperDriveOfflineSessionGrant>(
                "EDGE_SECURE_SESSION_INVALID", $"The Secret Service value is invalid: {ex.Message}"); }
        }

        public async Task<OASISResult<bool>> DeleteAsync(CancellationToken cancellationToken)
        {
            var result = await RunAsync(new[] { "clear", "service", "one.oasisomniverse.edge", "account", _account },
                null, cancellationToken, notFoundExitCode: 1).ConfigureAwait(false);
            return result.IsError ? SecureSessionGrantJson.Error<bool>(result.ErrorCode, result.Message)
                : new OASISResult<bool>(true) { IsSaved = true };
        }

        private static async Task<OASISResult<string>> RunAsync(string[] arguments, string stdin,
            CancellationToken cancellationToken, int notFoundExitCode = -1)
        {
            try
            {
                var start = new ProcessStartInfo("secret-tool")
                {
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                foreach (string argument in arguments) start.ArgumentList.Add(argument);
                using (var process = Process.Start(start))
                {
                    if (process == null) return Error("EDGE_LINUX_SECRET_SERVICE_START_FAILED", "secret-tool did not start.");
                    if (stdin != null) await process.StandardInput.WriteAsync(stdin).ConfigureAwait(false);
                    process.StandardInput.Close();
                    string output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                    string error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                    await WaitForExitAsync(process, cancellationToken).ConfigureAwait(false);
                    if (process.ExitCode == 0 || process.ExitCode == notFoundExitCode)
                        return new OASISResult<string>(process.ExitCode == 0 ? output : null) { IsLoaded = true };
                    return Error("EDGE_LINUX_SECRET_SERVICE_FAILED",
                        string.IsNullOrWhiteSpace(error) ? $"secret-tool exited with code {process.ExitCode}." : error.Trim());
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                return Error("EDGE_LINUX_SECRET_SERVICE_UNAVAILABLE",
                    $"Linux Secret Service requires libsecret secret-tool and an unlocked session collection: {ex.Message}");
            }
        }

        private static Task WaitForExitAsync(Process process, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => completion.TrySetResult(true);
            if (process.HasExited) completion.TrySetResult(true);
            cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            return completion.Task;
        }

        private static OASISResult<string> Error(string code, string message) =>
            SecureSessionGrantJson.Error<string>(code, message);
    }
}
