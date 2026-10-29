using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

internal static class HolonMigrationBarrierParticipants
{
    internal static HolonMigrationBarrierParticipant[] Bind(HolonReplicationMigrationBarrierPlan plan,
        IEnumerable<HolonMigrationBarrierParticipant> participants)
    {
        var supplied = participants?.Take(4).ToArray() ?? Array.Empty<HolonMigrationBarrierParticipant>();
        if (supplied.Length != plan.Scopes.Count || supplied.Any(p => p?.Storage == null || p.Provider == null)
            || supplied.Select(p => p.Storage.BindingFingerprint).Distinct(StringComparer.Ordinal).Count() != supplied.Length
            || plan.Scopes.Any(s => !supplied.Any(p => p.Storage.BindingFingerprint == s.ExpectedStorage.BindingFingerprint)))
            throw new HolonMigrationBarrierPlanException("COSMIC_MIGRATION_BARRIER_CAPABILITY_REQUIRED",
                "Supply one explicit native barrier provider for every distinct physical participant.");
        return plan.Scopes.Select(s => supplied.Single(p => p.Storage.BindingFingerprint == s.ExpectedStorage.BindingFingerprint)).ToArray();
    }
}
