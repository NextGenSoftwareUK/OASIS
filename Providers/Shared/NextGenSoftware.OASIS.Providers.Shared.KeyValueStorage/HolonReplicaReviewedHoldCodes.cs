using System;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>The held-head codes a trusted operator may resolve with a reviewed baseline
/// install or retirement. A lineage conflict means the target diverged; an unbound route
/// means the obligation was frozen to a pre-V2 legacy target id that the worker will
/// never deliver. Both stop delivery without native execution, so the same exact
/// source fence, target evidence and receipt rules apply to either.</summary>
public static class HolonReplicaReviewedHoldCodes
{
    public const string LineageConflict = "COSMIC_REPLICA_LINEAGE_CONFLICT";
    public const string RouteUnbound = "COSMIC_REPLICATION_ROUTE_UNBOUND";

    public static bool IsReviewable(string code)
        => string.Equals(code, LineageConflict, StringComparison.Ordinal) || string.Equals(code, RouteUnbound, StringComparison.Ordinal);
}
