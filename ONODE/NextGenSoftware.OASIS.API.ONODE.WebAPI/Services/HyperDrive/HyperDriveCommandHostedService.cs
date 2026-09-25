using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Executes the ordered durable command inbox through authoritative OASIS domain managers.</summary>
    public sealed class HyperDriveCommandHostedService : BackgroundService
    {
        private readonly ILogger<HyperDriveCommandHostedService> _logger;
        private readonly string _workerId = $"{Environment.MachineName}:{Guid.NewGuid():N}";

        public HyperDriveCommandHostedService(ILogger<HyperDriveCommandHostedService> logger) => _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (OASISBootLoader.OASISBootLoader.OASISDNA == null)
            {
                var boot = await OASISBootLoader.OASISBootLoader.BootOASISASync(false).ConfigureAwait(false);
                if (boot == null || boot.IsError || !boot.Result)
                    throw new InvalidOperationException(boot?.Message ?? "OASIS could not boot for HyperDrive commands.");
            }
            if (OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.OASISHyperDriveConfig?.EnableHostedSync != true)
            {
                _logger.LogInformation("Durable hosted HyperDrive command execution is disabled in OASIS DNA.");
                return;
            }
            var providerResult = await OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync()
                .ConfigureAwait(false);
            if (providerResult == null || providerResult.IsError || providerResult.Result == null)
                throw new InvalidOperationException(providerResult?.Message ?? "The hosted command provider could not be activated.");
            if (!(providerResult.Result is IHostedHyperDriveCommandStore store))
                throw new InvalidOperationException(
                    $"Hosted sync provider '{providerResult.Result.ProviderName}' must expose its durable command inbox.");

            var executor = new HyperDriveCommandExecutor(providerResult.Result);
            _logger.LogInformation("HyperDrive ordered command worker {WorkerId} started.", _workerId);
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var claim = await store.ClaimCommandsAsync(_workerId, 1, DateTime.UtcNow.AddMinutes(5),
                        stoppingToken).ConfigureAwait(false);
                    if (claim.IsError)
                    {
                        _logger.LogError("HyperDrive command claim failed ({Code}): {Message}", claim.ErrorCode, claim.Message);
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                        continue;
                    }
                    if (!claim.Result.LeaseAcquired || claim.Result.Items.Count == 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken).ConfigureAwait(false);
                        continue;
                    }
                    var command = claim.Result.Items[0];
                    try
                    {
                        var execution = executor.ExecuteAsync(command, stoppingToken);
                        Exception renewalFailure = null;
                        while (!execution.IsCompleted)
                        {
                            var interval = Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                            if (await Task.WhenAny(execution, interval).ConfigureAwait(false) == execution) break;
                            var renewed = await store.RenewCommandLeaseAsync(command.OperationId, _workerId,
                                DateTime.UtcNow.AddMinutes(5), stoppingToken).ConfigureAwait(false);
                            if (renewed.IsError)
                            {
                                renewalFailure = new InvalidOperationException(
                                    $"Command lease renewal failed ({renewed.ErrorCode}): {renewed.Message}",
                                    renewed.Exception);
                                break;
                            }
                        }
                        var outcome = await execution.ConfigureAwait(false);
                        if (renewalFailure != null) throw renewalFailure;
                        var completed = await store.CompleteCommandAsync(command.OperationId, _workerId, outcome,
                            stoppingToken).ConfigureAwait(false);
                        if (completed.IsError)
                            throw new InvalidOperationException(
                                $"Command completion could not commit ({completed.ErrorCode}): {completed.Message}",
                                completed.Exception);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        // Specified durable-I/O retry: 2^attempt seconds, capped at five minutes.
                        int seconds = Math.Min(300, 1 << Math.Min(command.AttemptCount, 8));
                        var failed = await store.FailCommandAttemptAsync(command.OperationId, _workerId,
                            "COMMAND_EXECUTION_TRANSIENT", ex.Message, DateTime.UtcNow.AddSeconds(seconds),
                            stoppingToken).ConfigureAwait(false);
                        if (failed.IsError)
                            throw new InvalidOperationException(
                                $"Command retry state could not commit ({failed.ErrorCode}): {failed.Message}",
                                failed.Exception ?? ex);
                        _logger.LogWarning(ex,
                            "HyperDrive command {OperationId} attempt {Attempt} deferred for {DelaySeconds} seconds.",
                            command.OperationId, command.AttemptCount, seconds);
                    }
                }
            }
            finally
            {
                var released = await store.ReleaseCommandWorkerAsync(_workerId, CancellationToken.None)
                    .ConfigureAwait(false);
                if (released.IsError)
                    _logger.LogError("HyperDrive command worker lease release failed ({Code}): {Message}",
                        released.ErrorCode, released.Message);
            }
        }
    }
}
