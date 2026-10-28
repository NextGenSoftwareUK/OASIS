using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Sandbox
{
    /// <summary>
    /// Deletes the sandbox's throwaway demo avatars, and everything they own, once they are older than the retention period.
    /// Every setting comes from DNA (<c>OASIS.Sandbox</c>). The service is registered on every WEB4 but does nothing unless that
    /// node's DNA has <c>OASIS.Sandbox.Enabled</c> set, so no other deployment ever deletes anything. See
    /// <see cref="SandboxAvatarCleanup"/> for what counts as a demo avatar.
    /// </summary>
    public sealed class SandboxCleanupHostedService : BackgroundService
    {
        private static readonly TimeSpan BootPoll = TimeSpan.FromSeconds(5);
        private readonly ILogger<SandboxCleanupHostedService> _logger;

        public SandboxCleanupHostedService(ILogger<SandboxCleanupHostedService> logger) => _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // The DNA is only loaded once OASIS has booted, so wait for that before reading the sandbox settings.
                while (!OASISBootLoader.OASISBootLoader.IsOASISBooted)
                    await Task.Delay(BootPoll, stoppingToken);

                if (!SandboxOptions.Enabled) return;
                _logger.LogInformation("Sandbox cleanup is on: demo avatars older than {Retention} are removed with their data, every {Interval}.",
                    SandboxOptions.Retention, SandboxOptions.CleanupInterval);

                await Task.Delay(SandboxOptions.CleanupInitialDelay, stoppingToken);
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var report = await CreateCleanup().SweepAsync(stoppingToken);
                        if (report.AvatarsRemoved + report.AvatarsKept > 0)
                            _logger.LogInformation("Sandbox cleanup: removed {Avatars} demo avatars and {Holons} holons; {Kept} kept for retry.",
                                report.AvatarsRemoved, report.HolonsRemoved, report.AvatarsKept);
                        foreach (var failure in report.Failures)
                            _logger.LogWarning("Sandbox cleanup: {Failure}", failure);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                    catch (Exception exception) { _logger.LogError(exception, "Sandbox cleanup pass failed."); }

                    await Task.Delay(SandboxOptions.CleanupInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }

        private static SandboxAvatarCleanup CreateCleanup()
        {
            var settings = SandboxOptions.Current;
            return new SandboxAvatarCleanup(
                async () =>
                {
                    var loaded = await Program.AvatarManager.LoadAllAvatarsAsync(loadPrivateKeys: false, hideAuthDetails: true);
                    if (loaded.IsError) throw new InvalidOperationException($"Listing avatars failed: {loaded.Message}");
                    return (IReadOnlyList<IAvatar>)(loaded.Result ?? Enumerable.Empty<IAvatar>()).ToList();
                },
                async (avatarId, provider) =>
                {
                    var loaded = await HolonManager.Instance.LoadAllHolonsAsync(HolonType.All, loadChildren: false, recursive: false, maxChildDepth: 0,
                        continueOnError: true, loadChildrenFromProvider: false, childHolonType: HolonType.All, version: 0, providerType: provider,
                        avatarId: avatarId, includePublic: false);
                    if (loaded.IsError) throw new InvalidOperationException($"Listing {provider} holons of {avatarId} failed: {loaded.Message}");
                    return (IReadOnlyList<IHolon>)(loaded.Result ?? Enumerable.Empty<IHolon>()).ToList();
                },
                async (holonId, avatarId, provider) => !(await HolonManager.Instance.DeleteHolonAsync(holonId, avatarId, softDelete: false, providerType: provider)).IsError,
                async (avatarId, provider) =>
                {
                    var deleted = await Program.AvatarManager.DeleteAvatarAsync(avatarId, softDelete: false, providerType: provider);
                    return !deleted.IsError && deleted.Result;
                },
                SandboxOptions.Providers(), settings.DemoAvatarUsernamePrefix, settings.DemoAvatarEmailDomain, SandboxOptions.Retention,
                SandboxOptions.AtLeastOne(settings.MaximumAvatarsRemovedPerSweep, nameof(settings.MaximumAvatarsRemovedPerSweep)));
        }
    }
}
