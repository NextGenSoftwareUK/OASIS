using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

public partial class IPFSOASIS : IBoundAtomicHolonReplicationStorageProvider, IBoundHolonReplicationOutboxSource
{
    public OASISResult<System.Collections.Generic.IEnumerable<IHolon>> ExecuteReplicatingHolonWrite(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment enrollment)
        => ExecuteReplicatingHolonWriteAsync(request, enrollment).GetAwaiter().GetResult();

    public Task<OASISResult<System.Collections.Generic.IEnumerable<IHolon>>> ExecuteReplicatingHolonWriteAsync(
        ConditionalHolonWriteRequest request, HolonReplicationEnrollment enrollment, CancellationToken cancellationToken = default)
        => ExecuteEnrolledCatalogueWriteAsync(request, enrollment, cancellationToken);

    public OASISResult<IHolonReplicationRouteInventoryOutbox> GetBoundHolonReplicationOutbox(CosmicStorageIdentity expectedSource)
    {
        ArgumentNullException.ThrowIfNull(expectedSource);
        return IsProviderActivated && expectedSource.Authority == ReplicaAuthority
            ? new(new CatalogueOutbox(this, expectedSource))
            : new() { IsError = true, ErrorCount = 1, Message = "An active IPFS provider and an IPFS source binding are required." };
    }

    public OASISResult<IHolonReplicationReadinessOutbox> GetHolonReplicationOutbox()
    {
        var captured = CaptureStorageIdentityAsync().GetAwaiter().GetResult();
        return captured.IsError
            ? new() { IsError = true, ErrorCount = 1, Message = captured.Message, Exception = captured.Exception }
            : new(new CatalogueOutbox(this, captured.Result));
    }

    private sealed class CatalogueOutbox(IPFSOASIS provider, CosmicStorageIdentity source) : IHolonReplicationRouteInventoryOutbox
    {
        private static long Sequence(JProperty entry) => long.Parse(entry.Name, CultureInfo.InvariantCulture);
        private static HolonDeliveryState State(JToken row) => Enum.Parse<HolonDeliveryState>(ReplicaText(row["state"]));

        private async Task<OASISResult<T>> ExecuteAsync<T>(Func<JObject, (T Value, bool Changed)> operation, CancellationToken token)
        {
            try
            {
                var gate = provider.CatalogueGate;
                await gate.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    using var lease = await provider.AcquireCatalogueFileLeaseAsync(token).ConfigureAwait(false);
                    var dna = provider.ReadPersistedCatalogueDna();
                    var (catalogue, records, storage) = await provider.ReadBoundCatalogueAsync(dna, token).ConfigureAwait(false);
                    if (storage.BindingFingerprint != source.BindingFingerprint)
                        throw new ReplicaStorageBindingException("The IPFS outbox source binding changed.");
                    var deliveries = GetReplicaObject(catalogue, "outboundDeliveries");
                    var (value, changed) = operation(deliveries);
                    if (changed)
                    {
                        catalogue["outboundDeliveries"] = deliveries;
                        await provider.PublishCatalogueAsync(catalogue, records, dna, token).ConfigureAwait(false);
                    }
                    return new(value);
                }
                finally { gate.Release(); }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception cause)
            {
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_IPFS_OUTBOX_FAILED",
                    Message = cause.Message, Exception = cause };
            }
        }

        private static JProperty Head(JObject deliveries, string target) => deliveries.Properties()
            .Where(entry => ReplicaText(entry.Value["target"]) == target && State(entry.Value) != HolonDeliveryState.Completed)
            .OrderBy(Sequence).FirstOrDefault();

        private static CommittedHolonReplicaRequest ReadPayload(JToken row)
        {
            var payload = ReplicaText(row["payload"]);
            if (HolonRecoveryEvidenceJson.Digest(payload) != ReplicaText(row["digest"]))
                throw new InvalidDataException("The IPFS outbox payload checksum differs.");
            var request = CommittedHolonReplicaPayload.ReadForTarget(payload, ReplicaText(row["target"]));
            if (request.OperationId != Guid.Parse(ReplicaText(row["operation"])) || request.SourceAuthority != ReplicaAuthority)
                throw new InvalidDataException("The IPFS delivery identity differs from its payload.");
            return request;
        }

        private HolonDeliveryInspection Inspect(JProperty entry)
        {
            if (entry == null) return null;
            var row = entry.Value;
            var target = ReplicaText(row["target"]);
            var state = State(row);
            var route = ReadPayload(row).NativeRoutes.Single(binding => binding.TargetId == target);
            if (route.SourceStorage.BindingFingerprint != source.BindingFingerprint)
                throw new InvalidDataException("The IPFS delivery source differs from its catalogue.");
            return new(Sequence(entry), Guid.Parse(ReplicaText(row["operation"])), target, state, row.Value<int>("attempts"),
                row.Value<string>("error"), row.Value<string>("message"),
                HolonRecoveryEvidenceJson.ReadStored(row.Value<string>("recovery"), row.Value<string>("recoveryDigest"), state), route);
        }

        public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string targetId, CancellationToken cancellationToken = default)
            => ExecuteAsync(rows => (Inspect(Head(rows, targetId)), false), cancellationToken);

        public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence, CancellationToken cancellationToken = default)
            => ExecuteAsync(rows => (Inspect(rows.Property(sequence.ToString(CultureInfo.InvariantCulture))), false), cancellationToken);

        public Task<OASISResult<HolonReplicationRoutePage>> ReadRouteHeadsAsync(int pageSize = 64,
            string afterTargetId = null, CancellationToken cancellationToken = default)
            => ExecuteAsync(rows =>
            {
                HolonReplicationRoutePage.ValidateRequest(pageSize, afterTargetId);
                var targets = rows.Properties().Where(entry => State(entry.Value) != HolonDeliveryState.Completed)
                    .Select(entry => ReplicaText(entry.Value["target"])).Distinct(StringComparer.Ordinal)
                    .Where(target => afterTargetId == null || HolonReplicationRoutePage.CompareTargetIds(target, afterTargetId) > 0)
                    .OrderBy(target => target, System.Collections.Generic.Comparer<string>.Create(HolonReplicationRoutePage.CompareTargetIds))
                    .Take(pageSize + 1).ToArray();
                return (new HolonReplicationRoutePage(targets.Take(pageSize).Select(target => Inspect(Head(rows, target))),
                    targets.Length > pageSize), false);
            }, cancellationToken);

        public Task<OASISResult<HolonDeliveryClaim>> ClaimNextAsync(string targetId, string workerId, TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
            => ExecuteAsync<HolonDeliveryClaim>(rows =>
            {
                ValidateLeaseDuration(leaseDuration);
                if (string.IsNullOrWhiteSpace(targetId) || string.IsNullOrWhiteSpace(workerId) || workerId.Length > 256)
                    throw new ArgumentException("A target and worker identity are required.");
                var head = Head(rows, targetId);
                if (head == null) return (null, false);
                var row = (JObject)head.Value;
                var now = DateTime.UtcNow;
                if (State(row) == HolonDeliveryState.ReconciliationRequired ||
                    row.Value<long?>("leaseUntil") > now.Ticks || row.Value<long?>("retryAt") > now.Ticks)
                    return (null, false);
                var inspection = Inspect(head);
                var lease = Guid.NewGuid();
                var until = now.Add(leaseDuration);
                var attempts = checked(inspection.AttemptCount + 1);
                row["state"] = nameof(HolonDeliveryState.InFlight);
                row["lease"] = lease.ToString("D");
                row["worker"] = workerId;
                row["leaseUntil"] = until.Ticks;
                row["attempts"] = attempts;
                return (new HolonDeliveryClaim(inspection.Sequence, inspection.OperationId, targetId, workerId, lease,
                    ReplicaText(row["payload"]), attempts, until), true);
            }, cancellationToken);

        public Task<OASISResult<bool>> RenewAsync(HolonDeliveryClaim claim, TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
            => ExecuteAsync(rows =>
            {
                ValidateLeaseDuration(leaseDuration);
                var now = DateTime.UtcNow;
                var row = Owned(rows, claim, now);
                if (row == null) return (false, false);
                row["leaseUntil"] = Math.Max(row.Value<long>("leaseUntil"), now.Add(leaseDuration).Ticks);
                return (true, true);
            }, cancellationToken);

        public Task<OASISResult<bool>> FinishAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            string errorCode = null, string message = null, DateTime? nextAttemptUtc = null,
            CancellationToken cancellationToken = default)
            => FinishCoreAsync(claim, outcome, null, errorCode, message, nextAttemptUtc, cancellationToken);

        public Task<OASISResult<bool>> FinishWithRecoveryAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            HolonDeliveryRecoveryEvidence recoveryEvidence, string errorCode = null, string message = null,
            DateTime? nextAttemptUtc = null, CancellationToken cancellationToken = default)
            => FinishCoreAsync(claim, outcome, recoveryEvidence ?? throw new ArgumentNullException(nameof(recoveryEvidence)),
                errorCode, message, nextAttemptUtc, cancellationToken);

        private Task<OASISResult<bool>> FinishCoreAsync(HolonDeliveryClaim claim, HolonDeliveryOutcome outcome,
            HolonDeliveryRecoveryEvidence evidence, string errorCode, string message, DateTime? nextAttemptUtc, CancellationToken token)
            => ExecuteAsync(rows =>
            {
                var now = DateTime.UtcNow;
                var state = outcome switch
                {
                    HolonDeliveryOutcome.Completed => HolonDeliveryState.Completed,
                    HolonDeliveryOutcome.Retry => HolonDeliveryState.Pending,
                    HolonDeliveryOutcome.ReconciliationRequired => HolonDeliveryState.ReconciliationRequired,
                    _ => throw new ArgumentOutOfRangeException(nameof(outcome))
                };
                if ((outcome == HolonDeliveryOutcome.Completed && (errorCode != null || message != null)) ||
                    (outcome != HolonDeliveryOutcome.Completed && (string.IsNullOrWhiteSpace(errorCode) || errorCode.Length > 128 || message?.Length > 4096)) ||
                    (outcome == HolonDeliveryOutcome.Retry ? nextAttemptUtc?.Kind != DateTimeKind.Utc || nextAttemptUtc <= now : nextAttemptUtc != null) ||
                    (evidence?.RequiresNativeReconciliation == true && state != HolonDeliveryState.ReconciliationRequired))
                    throw new ArgumentException("Delivery diagnostics and retry time must match the outcome.");
                var row = Owned(rows, claim, now);
                if (row == null) return (false, false);
                row["state"] = state.ToString();
                row.Remove("lease");
                row.Remove("worker");
                row.Remove("leaseUntil");
                row["retryAt"] = nextAttemptUtc?.Ticks ?? 0;
                row["error"] = errorCode;
                row["message"] = message;
                var recovery = evidence == null ? null : HolonRecoveryEvidenceJson.Write(evidence);
                row["recovery"] = recovery;
                row["recoveryDigest"] = recovery == null ? null : HolonRecoveryEvidenceJson.Digest(recovery);
                return (true, true);
            }, token);

        private static void ValidateLeaseDuration(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(1))
                throw new ArgumentOutOfRangeException(nameof(duration));
        }

        private static JObject Owned(JObject rows, HolonDeliveryClaim claim, DateTime now)
        {
            ArgumentNullException.ThrowIfNull(claim);
            var row = rows[claim.Sequence.ToString(CultureInfo.InvariantCulture)] as JObject;
            return row != null && State(row) == HolonDeliveryState.InFlight && row.Value<long>("leaseUntil") > now.Ticks &&
                row.Value<string>("operation") == claim.OperationId.ToString("D") && row.Value<string>("target") == claim.TargetId &&
                row.Value<string>("lease") == claim.LeaseToken.ToString("D") && row.Value<string>("worker") == claim.WorkerId
                ? row : null;
        }
    }
}
