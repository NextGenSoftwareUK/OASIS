using System;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

/// <summary>Host-only lifecycle on the existing billing authority, never a caller-selected plan.</summary>
public interface ISandboxSubscriptionStore
{
    Task CreateSandboxSubscriptionAsync(string avatarId, DateTime createdUtc, DateTime expiresUtc, CancellationToken token);
    Task<SandboxSubscriptionSweep> RetireSandboxSubscriptionsAsync(int maximumRecords, CancellationToken token);
}

public sealed record SandboxSubscriptionSweep(int Removed, int Deferred);
