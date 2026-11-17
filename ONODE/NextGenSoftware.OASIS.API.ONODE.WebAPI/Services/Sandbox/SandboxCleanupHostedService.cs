using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using CleanupRouter = NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive;

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
        private readonly SandboxSubscriptionLifecycle _subscriptions;

        public SandboxCleanupHostedService(ILogger<SandboxCleanupHostedService> logger, SandboxSubscriptionLifecycle subscriptions)
        { _logger = logger; _subscriptions = subscriptions; }

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
                        var billing = await _subscriptions.SweepAsync(
                            SandboxOptions.AtLeastOne(SandboxOptions.Current.MaximumAvatarsRemovedPerSweep,
                                nameof(SandboxOptions.Current.MaximumAvatarsRemovedPerSweep)), stoppingToken);
                        if (billing.Removed + billing.Deferred > 0)
                            _logger.LogInformation("Sandbox subscription cleanup: retired {Removed}; {Deferred} retained for unresolved usage or changed ownership.",
                                billing.Removed, billing.Deferred);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                    catch (Exception exception) { _logger.LogError(exception, "Sandbox cleanup pass failed."); }

                    await Task.Delay(SandboxOptions.CleanupInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }

        private SandboxAvatarCleanup CreateCleanup()
        {
            var settings = SandboxOptions.Current;
            var providers = SandboxOptions.Providers();
            var holonManagers = providers.ToDictionary(provider => provider, CreatePinnedHolonManager);
            var routing = new CleanupRouter(ProviderManager.Instance);
            return new SandboxAvatarCleanup(
                async () =>
                {
                    var copies = new List<IAvatar>();
                    foreach (var provider in providers)
                    {
                        var loaded = await routing.RouteRequestToProviderAsync<IEnumerable<IAvatar>>(
                            new StorageOperationRequest { Operation = "LoadAllAvatars", Version = 0,
                                PreferredProvider = provider, RequirePreferredProvider = true, AutoReplicate = false }, provider);
                        if (loaded == null || loaded.IsError || loaded.Result == null)
                            throw new InvalidOperationException($"Listing sandbox avatars failed on {provider}; code {loaded?.ErrorCode ?? "CLEANUP_NOT_ACKNOWLEDGED"}.");
                        copies.AddRange(loaded.Result);
                    }
                    return MergeAvatarCopies(copies);
                },
                async (avatarId, provider) =>
                {
                    var loaded = await holonManagers[provider].LoadAllHolonsAsync(HolonType.All, loadChildren: false, recursive: false, maxChildDepth: 0,
                        continueOnError: true, loadChildrenFromProvider: false, childHolonType: HolonType.All, version: 0, providerType: provider,
                        avatarId: avatarId, includePublic: false);
                    if (loaded == null || loaded.IsError || loaded.Result == null)
                        throw new InvalidOperationException($"Listing sandbox holons failed on {provider}; code {loaded?.ErrorCode ?? "CLEANUP_NOT_ACKNOWLEDGED"}.");
                    return (IReadOnlyList<IHolon>)loaded.Result.ToList();
                },
                async (holonId, avatarId, provider) =>
                {
                    var deleted = await holonManagers[provider].DeleteHolonAsync(holonId, avatarId, softDelete: false, providerType: provider);
                    if (deleted == null || deleted.IsError)
                        _logger.LogWarning("Sandbox holon cleanup failed on {Provider}; code {Code}.",
                            provider, deleted?.ErrorCode ?? "CLEANUP_NOT_ACKNOWLEDGED");
                    return deleted != null && !deleted.IsError;
                },
                async (avatarId, provider) =>
                {
                    var deleted = await routing.RouteRequestToProviderAsync<bool>(new StorageOperationRequest
                        { Operation = "DeleteAvatar", AvatarId = avatarId, SoftDelete = false,
                            PreferredProvider = provider, RequirePreferredProvider = true, AutoReplicate = false }, provider);
                    if (deleted == null || deleted.IsError || !deleted.Result)
                        _logger.LogWarning("Sandbox avatar cleanup failed on {Provider}; code {Code}.",
                            provider, deleted?.ErrorCode ?? "CLEANUP_NOT_ACKNOWLEDGED");
                    return deleted != null && !deleted.IsError && deleted.Result;
                },
                providers, settings.DemoAvatarUsernamePrefix, settings.DemoAvatarEmailDomain, SandboxOptions.Retention,
                SandboxOptions.AtLeastOne(settings.MaximumAvatarsRemovedPerSweep, nameof(settings.MaximumAvatarsRemovedPerSweep)));
        }

        private static HolonManager CreatePinnedHolonManager(ProviderType provider)
        {
            var context = HolonManager.Instance.CreateRequestContext(new ProviderRoutingOverrides(
                autoFailoverEnabled: false, autoReplicationEnabled: false, autoLoadBalancingEnabled: false), provider);
            if (context == null || context.IsError || context.Result == null)
                throw new InvalidOperationException($"Sandbox routing context failed on {provider}; code {context?.ErrorCode ?? "CLEANUP_NOT_ACKNOWLEDGED"}.");
            return context.Result.Manager;
        }

        private static IReadOnlyList<IAvatar> MergeAvatarCopies(IEnumerable<IAvatar> copies)
        {
            var merged = new Dictionary<Guid, IAvatar>();
            foreach (var avatar in copies)
            {
                if (avatar == null || avatar.Id == Guid.Empty)
                    throw new InvalidOperationException("A sandbox provider returned an invalid avatar identity.");
                if (merged.TryGetValue(avatar.Id, out var previous))
                {
                    // Never choose an expired demo copy over a conflicting real/current identity.
                    if (!string.Equals(previous.Username, avatar.Username, StringComparison.Ordinal)
                        || !string.Equals(previous.Email, avatar.Email, StringComparison.OrdinalIgnoreCase)
                        || previous.AvatarType?.Value != avatar.AvatarType?.Value
                        || previous.CreatedDate.ToUniversalTime() != avatar.CreatedDate.ToUniversalTime())
                        throw new InvalidOperationException("Sandbox avatar identity differs between configured providers.");
                }
                else merged.Add(avatar.Id, avatar);
            }
            return merged.Values.ToList();
        }
    }
}
