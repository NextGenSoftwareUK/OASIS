using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests")]

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Immutable exact evidence to approve before starting native mutation.
/// This is neither approval nor a durable commit decision. Native CAS must still
/// verify the observed states; constructing this object never touches a provider.</summary>
public sealed class HolonMigrationMutationPreparation
{
    public string PlanFingerprint { get; }
    public string AuthorizationContextDigest { get; }
    public string DeliveryObservationDigest { get; }
    public string ReplacementObservationDigest { get; }
    public string MutationPolicyBinding { get; }
    public string CanonicalJson { get; }
    public string Fingerprint { get; }

    public HolonMigrationMutationPreparation(HolonReplicationMigrationBarrierPlan plan,
        HolonMigrationDeliveryReconciliationReport delivery,
        HolonMigrationReplacementReport replacement, string mutationPolicyBinding)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(delivery);
        ArgumentNullException.ThrowIfNull(replacement);
        HolonReplicationMigrationAuthorizationContext.ValidateToken(mutationPolicyBinding);

        if (delivery.PlanFingerprint != plan.Fingerprint || replacement.PlanFingerprint != plan.Fingerprint
            || replacement.StorageFingerprint != plan.Context.ReplacementRoute.TargetStorage.BindingFingerprint)
            throw new ArgumentException("Mutation preparation must use observations of the exact original plan and replacement store.");

        // These observations cannot authorize treatment of another route or an
        // unresolved worker. A wider migration needs its own reviewed disposition.
        if (delivery.SourceConsistency != HolonReplicaHeadReadConsistency.NativeSnapshot
            || delivery.Obligations.Any(r => r.RequiresNativeReconciliation
                || r.SourceState == HolonDeliveryState.InFlight
                || r.SourceState == HolonDeliveryState.ReconciliationRequired
                || r.Kind == HolonMigrationDeliveryObservationKind.OtherAffectedRoute
                || r.Kind == HolonMigrationDeliveryObservationKind.AdditionalAffectedObligation))
            throw new ArgumentException("Unresolved workers, affected routes or weak observations require fresh migration review.");

        if (replacement.RequiredConsistency != HolonReplicaHeadReadConsistency.NativeSnapshot
            || replacement.PageConsistencies.Count == 0
            || replacement.PageConsistencies.Any(c => c != HolonReplicaHeadReadConsistency.NativeSnapshot)
            || replacement.Records.Any(r => r.Kind != HolonMigrationReplacementKind.EmptyAtObservation
                && r.Kind != HolonMigrationReplacementKind.MatchingOriginalLineage
                && r.Kind != HolonMigrationReplacementKind.MatchingReplacementLineage)
            || !replacement.Records.Select(r => r.Expected.HolonId).SequenceEqual(plan.Scopes[0].HolonIds))
            throw new ArgumentException("Replacement selection, consistency or ownership conflicts prevent mutation preparation.");

        PlanFingerprint = plan.Fingerprint;
        AuthorizationContextDigest = plan.Context.ContextDigest;
        DeliveryObservationDigest = delivery.ObservationDigest;
        ReplacementObservationDigest = replacement.ObservationDigest;
        MutationPolicyBinding = mutationPolicyBinding;
        CanonicalJson = JsonSerializer.Serialize(new {
            format = "cosmic-migration-mutation-preparation-v1", PlanFingerprint,
            AuthorizationContextDigest, DeliveryObservationDigest,
            ReplacementObservationDigest, MutationPolicyBinding });
        Fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalJson)));
    }
}
