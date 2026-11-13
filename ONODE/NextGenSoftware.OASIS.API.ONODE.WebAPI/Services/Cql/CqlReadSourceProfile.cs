using System;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>An explicitly configured query-only representation. Its lifetime and
/// activation are independent of the canonical wallet/storage provider registry.</summary>
public sealed class CqlReadSourceProfile : IDisposable
{
    public IOASISStorageProvider Storage { get; }
    public CqlReadSourceActivationPolicy ActivationPolicy { get; }

    public CqlReadSourceProfile(IOASISStorageProvider storage)
        : this(storage, CqlReadSourceActivationPolicy.None) { }

    public CqlReadSourceProfile(IOASISStorageProvider storage, CqlReadSourceActivationPolicy activationPolicy)
    {
        if (storage?.ProviderType == null || storage is not ICosmicQueryStorageProvider query
            || query.QueryProviderType != storage.ProviderType.Value || query.QueryCapabilities == null
            || storage is not ICosmicReadOnlyQueryActivation)
            throw new ArgumentException("An explicit read-only query adapter is required.", nameof(storage));
        Storage = storage;
        ActivationPolicy = activationPolicy ?? throw new ArgumentNullException(nameof(activationPolicy));
    }

    public void Dispose() => (Storage as IDisposable)?.Dispose();
}

/// <summary>Host-owned health-check recovery only. Never retries a user query,
/// save, signing operation, authentication failure or malformed native response.</summary>
public sealed class CqlReadSourceActivationPolicy
{
    public static CqlReadSourceActivationPolicy None { get; } = new(1, TimeSpan.Zero);
    public static CqlReadSourceActivationPolicy TransientServerErrorOnce { get; } = new(2, TimeSpan.FromSeconds(5));
    public int MaximumAttempts { get; }
    public TimeSpan RetryDelay { get; }

    private CqlReadSourceActivationPolicy(int attempts, TimeSpan delay)
    {
        MaximumAttempts = attempts;
        RetryDelay = delay;
    }

    internal int? RetryStatus(Exception cause, int completedAttempt)
    {
        if (completedAttempt >= MaximumAttempts) return null;
        for (int depth = 0; cause != null && depth < 8; depth++, cause = cause.InnerException)
            if (cause is HttpRequestException http && http.StatusCode.HasValue)
            {
                int status = (int)http.StatusCode.Value;
                return status is 500 or 502 or 503 or 504 ? status : null;
            }
        return null;
    }
}
