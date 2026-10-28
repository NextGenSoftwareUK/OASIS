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
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Sandbox
{
    /// <summary>
    /// Deletes the sandbox's throwaway demo avatars, and everything they own, once they are older than the retention period
    /// (<c>OASIS_SANDBOX_RETENTION_MINUTES</c>, default 24 hours). It is registered only when <c>OASIS_SANDBOX=true</c>, so no
    /// other WEB4 deployment ever runs it. See <see cref="SandboxAvatarCleanup"/> for what counts as a demo avatar.
    /// </summary>
    public sealed class SandboxCleanupHostedService : BackgroundService
    {
        public const string RetentionVariable = "OASIS_SANDBOX_RETENTION_MINUTES";
        private static readonly TimeSpan DefaultRetention = TimeSpan.FromHours(24);
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
        private readonly ILogger<SandboxCleanupHostedService> _logger;

        public SandboxCleanupHostedService(ILogger<SandboxCleanupHostedService> logger) => _logger = logger;

        public static bool IsSandbox =>
            string.Equals(Environment.GetEnvironmentVariable("OASIS_SANDBOX"), "true", StringComparison.OrdinalIgnoreCase);

        public static TimeSpan Retention =>
            int.TryParse(Environment.GetEnvironmentVariable(RetentionVariable), out var minutes) && minutes >= 1 ? TimeSpan.FromMinutes(minutes) : DefaultRetention;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Sandbox cleanup is on: demo avatars older than {Retention} are removed with their data.", Retention);
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
                catch (OperationCanceledException) { break; }

                if (OASISBootLoader.OASISBootLoader.IsOASISBooted)
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
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                    catch (Exception exception) { _logger.LogError(exception, "Sandbox cleanup pass failed."); }
                }

                try { await Task.Delay(Interval, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        private static SandboxAvatarCleanup CreateCleanup()
        {
            var providers = SandboxController.SandboxProviders.Split(',').Select(name => Enum.Parse<ProviderType>(name.Trim())).ToList();
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
                providers, Retention);
        }
    }
}
