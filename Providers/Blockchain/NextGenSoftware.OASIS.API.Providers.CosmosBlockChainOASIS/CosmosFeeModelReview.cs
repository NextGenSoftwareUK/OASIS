using System;
using System.IO;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

/// <summary>Observation from a trusted deployment inspector, NOT RPC-reported code attestation or request JSON.
/// Hash comparison cannot establish that an untrusted observer actually inspected the deployed binary.</summary>
public sealed record CosmosDeploymentObservation(string ChainId, string GenesisSha256, string BinarySha256,
    string ApplicationVersion, string ApplicationCommit, string SdkVersion, long CommittedHeight, DateTimeOffset ObservedAtUtc);

/// <summary>Immutable, expiring review of one exact deployment/height interval. This guard alone neither
/// upgrades fee coverage nor proves a receipt, a refund, tip, charge or permission to spend.</summary>
public sealed class CosmosFeeModelReview
{
    // Reviewed primary source: wasmd app.setPostHandler -> SDK v0.54.0 empty post-decorator chain.
    public const string WasmdVersion = "0.70.3";
    public const string WasmdCommit = "47e9cb8aa080eba5db31bc294f614a2e15b3c82e";
    public const string SdkVersion = "v0.54.0";
    private readonly TimeProvider _time;
    public string ReviewId { get; }
    public string ChainId { get; }
    public string GenesisSha256 { get; }
    public string BinarySha256 { get; }
    public long FirstHeight { get; }
    public long LastHeight { get; }
    public DateTimeOffset ReviewedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public TimeSpan MaximumObservationAge { get; }

    private static bool CanonicalHash(string value) => value?.Length == 64 && value.All(c => c is >= '0' and <= '9' or >= 'A' and <= 'F');
    private static bool Identifier(string value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && !value.Any(c => char.IsWhiteSpace(c) || char.IsControl(c));

    /// <summary>Explicit server-owned review for the audited wasmd build only. Other applications, SDK versions,
    /// custom post-handlers and deployments remain unsupported by this particular review model.</summary>
    public static CosmosFeeModelReview ForWasmd0703(string reviewId, string chainId, string genesisSha256,
        string binarySha256, long firstHeight, long lastHeight, DateTimeOffset reviewedAtUtc,
        DateTimeOffset expiresAtUtc, TimeSpan maximumObservationAge, TimeProvider timeProvider = null)
        => new(reviewId, chainId, genesisSha256, binarySha256, firstHeight, lastHeight, reviewedAtUtc,
            expiresAtUtc, maximumObservationAge, timeProvider ?? TimeProvider.System);

    private CosmosFeeModelReview(string reviewId, string chainId, string genesisSha256, string binarySha256,
        long firstHeight, long lastHeight, DateTimeOffset reviewedAtUtc, DateTimeOffset expiresAtUtc,
        TimeSpan maximumObservationAge, TimeProvider time)
    {
        _time = time;
        var now = _time.GetUtcNow();
        if (!Identifier(reviewId, 128) || !Identifier(chainId, 128) || !CanonicalHash(genesisSha256) || !CanonicalHash(binarySha256)
            || firstHeight <= 0 || lastHeight < firstHeight || reviewedAtUtc.Offset != TimeSpan.Zero || expiresAtUtc.Offset != TimeSpan.Zero
            || reviewedAtUtc > now || expiresAtUtc <= now || expiresAtUtc <= reviewedAtUtc
            || maximumObservationAge <= TimeSpan.Zero || maximumObservationAge > TimeSpan.FromMinutes(5))
            throw new ArgumentException("Canonical exact deployment, bounded heights and current UTC fee-model review required.");
        ReviewId = reviewId; ChainId = chainId; GenesisSha256 = genesisSha256; BinarySha256 = binarySha256;
        FirstHeight = firstHeight; LastHeight = lastHeight; ReviewedAtUtc = reviewedAtUtc; ExpiresAtUtc = expiresAtUtc;
        MaximumObservationAge = maximumObservationAge;
    }

    /// <summary>Validate AFTER the inspector returns; sample time internally so pre-query timestamps cannot
    /// keep an expired review/observation valid. Caller must separately verify original signed fee evidence.</summary>
    public void ValidateObservation(CosmosDeploymentObservation observation, long originalHeight)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var now = _time.GetUtcNow();
        if (now < ReviewedAtUtc || now >= ExpiresAtUtc || originalHeight < FirstHeight || originalHeight > LastHeight
            || observation.ChainId != ChainId || observation.GenesisSha256 != GenesisSha256 || observation.BinarySha256 != BinarySha256
            || observation.ApplicationVersion != WasmdVersion || observation.ApplicationCommit != WasmdCommit || observation.SdkVersion != SdkVersion
            || observation.CommittedHeight < originalHeight || observation.CommittedHeight > LastHeight
            || observation.ObservedAtUtc.Offset != TimeSpan.Zero || observation.ObservedAtUtc < ReviewedAtUtc
            || observation.ObservedAtUtc > now || now - observation.ObservedAtUtc > MaximumObservationAge)
            throw new InvalidDataException("Cosmos deployment/height evidence does not satisfy the current exact fee-model review.");
    }
}
