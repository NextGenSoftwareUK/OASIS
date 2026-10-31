using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Sandbox;

public sealed record SandboxSubscriptionPolicy(bool Enabled, string UsernamePrefix, string EmailDomain, TimeSpan Retention);

/// <summary>Issues only the host's verified temporary identities, using the existing billing authority.</summary>
public sealed class SandboxSubscriptionLifecycle
{
    private readonly Func<ISandboxSubscriptionStore> store;
    private readonly Func<bool> admissionEnabled;
    private readonly Func<SandboxSubscriptionPolicy> policy;
    private readonly TimeProvider clock;

    public SandboxSubscriptionLifecycle(Func<ISandboxSubscriptionStore> store, Func<bool> admissionEnabled,
        Func<SandboxSubscriptionPolicy> policy, TimeProvider clock = null)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.admissionEnabled = admissionEnabled ?? throw new ArgumentNullException(nameof(admissionEnabled));
        this.policy = policy ?? throw new ArgumentNullException(nameof(policy));
        this.clock = clock ?? TimeProvider.System;
    }

    public async Task ProvisionAsync(IAvatar avatar, CancellationToken token)
    {
        var settings = policy();
        if (!settings.Enabled || !admissionEnabled()) return; // Explicitly disabled optional demo feature.
        token.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(settings.UsernamePrefix) || settings.UsernamePrefix.Length < 3 ||
            string.IsNullOrWhiteSpace(settings.EmailDomain) || !settings.EmailDomain.Contains('.') || settings.EmailDomain.StartsWith('@') ||
            settings.Retention < TimeSpan.FromMinutes(1)) throw new InvalidOperationException("Valid sandbox identity markers and retention are required.");
        if (avatar == null || avatar.Id == Guid.Empty || avatar.AvatarType?.Value != AvatarType.User || !avatar.IsActive ||
            !avatar.Verified.HasValue || avatar.CreatedDate == default ||
            avatar.Username == null || !avatar.Username.StartsWith(settings.UsernamePrefix, StringComparison.Ordinal) ||
            !string.Equals(avatar.Email, avatar.Username + "@" + settings.EmailDomain, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only a verified active sandbox-issued User can receive a temporary free subscription.");

        // BSON dates have millisecond precision; the same issuance must compare identically after reload.
        var created = DateTimeOffset.FromUnixTimeMilliseconds(new DateTimeOffset(avatar.CreatedDate.ToUniversalTime()).ToUnixTimeMilliseconds()).UtcDateTime;
        var expires = created.Add(settings.Retention);
        expires = DateTimeOffset.FromUnixTimeMilliseconds(new DateTimeOffset(expires).ToUnixTimeMilliseconds()).UtcDateTime;
        if (created > clock.GetUtcNow().UtcDateTime || expires <= clock.GetUtcNow().UtcDateTime)
            throw new InvalidOperationException("The demo avatar lifetime is invalid or has expired.");
        await store().CreateSandboxSubscriptionAsync(avatar.Id.ToString("D"), created, expires, token);
    }

    public Task<SandboxSubscriptionSweep> SweepAsync(int maximumRecords, CancellationToken token)
    {
        if (!policy().Enabled || !admissionEnabled()) return Task.FromResult(new SandboxSubscriptionSweep(0, 0));
        token.ThrowIfCancellationRequested();
        return store().RetireSandboxSubscriptionsAsync(maximumRecords, token);
    }
}
