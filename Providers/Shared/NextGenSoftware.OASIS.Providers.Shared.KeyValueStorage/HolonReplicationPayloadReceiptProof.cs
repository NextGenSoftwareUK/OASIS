using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Redacted equivalence proof; preserves the original frozen bytes and
/// receipt fingerprints. Not a native fence, signature or migration permission.</summary>
public sealed class HolonReplicationPayloadReceiptProof
{
    public string Format => "cosmic-payload-source-receipt-proof-v1";
    public Guid OperationId { get; }
    public Guid ActorId { get; }
    public long Sequence { get; }
    public string TargetId { get; }
    public string SourceStorageFingerprint { get; }
    public string TargetStorageFingerprint { get; }
    public string PayloadDigest { get; }
    public string StateDigest { get; }
    public string OriginalRequestFingerprint { get; }
    public string SourceReceiptObservationDigest { get; }
    public string ReplicaIntentFingerprint { get; }
    public HolonReplicaHeadReadConsistency RequiredSourceConsistency { get; }
    public string ProofDigest { get; }
    internal HolonReplicationPayloadReceiptProof(HolonReplicationOperationObligation row,
        HolonReplicationReceiptPreflightReport receipt, string replicaIntent, HolonReplicaHeadReadConsistency requiredSourceConsistency)
    {
        OperationId = row.OperationId; ActorId = row.ActorId; Sequence = row.Sequence; TargetId = row.TargetId;
        SourceStorageFingerprint = receipt.SourceStorageFingerprint; TargetStorageFingerprint = receipt.TargetStorageFingerprint;
        PayloadDigest = row.PayloadDigest; StateDigest = row.StateDigest;
        OriginalRequestFingerprint = receipt.OriginalRequestFingerprint;
        SourceReceiptObservationDigest = receipt.ObservationDigest; ReplicaIntentFingerprint = replicaIntent;
        RequiredSourceConsistency = requiredSourceConsistency;
        ProofDigest = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new {
            Format, OperationId, ActorId, Sequence, TargetId, SourceStorageFingerprint, TargetStorageFingerprint,
            PayloadDigest, StateDigest, OriginalRequestFingerprint, SourceReceiptObservationDigest, ReplicaIntentFingerprint, RequiredSourceConsistency })));
    }
}

public static class HolonReplicationPayloadReceiptVerifier
{
    /// <summary>Read exact bound source bytes transiently, then return only proof.
    /// No raw graph is retained in the result, and no mutation is performed.</summary>
    public static async Task<OASISResult<HolonReplicationPayloadReceiptProof>> ReadAsync(
        IHolonReplicationPayloadAuditOutbox source, CosmicStorageIdentity expectedSource,
        HolonReplicationOperationObligation row, ConditionalHolonWriteRequest original,
        HolonReplicationReceiptPreflightReport receipt,
        HolonReplicaHeadReadConsistency requiredConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<HolonReplicationPayloadReceiptProof>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (source == null || expectedSource == null || row == null || original == null || receipt == null
                || receipt.SourceStorageFingerprint != expectedSource.BindingFingerprint || row.SourceAuthority != expectedSource.Authority)
                return Failure(result, "COSMIC_PAYLOAD_RECEIPT_PROOF_OPTIONS_INVALID", "An exact physical source and verified original receipt scope are required.");
            var read = await source.ReadFrozenPayloadAsync(expectedSource, row, requiredConsistency, cancellationToken).ConfigureAwait(false);
            if (read != null)
            {
                result.IsWarning = read.IsWarning; result.WarningCount = read.WarningCount;
                result.Exception = read.Exception; result.InnerMessages.AddRange(read.InnerMessages);
            }
            if (read == null || read.IsError || read.Result == null)
            {
                result.ErrorCount = read?.ErrorCount ?? 1;
                return Failure(result, read?.ErrorCode ?? "COSMIC_PAYLOAD_RECEIPT_PROOF_UNVERIFIED", "The exact frozen source transport could not be verified.");
            }
            cancellationToken.ThrowIfCancellationRequested();
            result.Result = Verify(read.Result, row, original, receipt, requiredConsistency);
            cancellationToken.ThrowIfCancellationRequested(); result.IsLoaded = true;
            return result;
        }
        catch (Exception cause)
        {
            return Failure(result, cause is OperationCanceledException ? "COSMIC_PAYLOAD_RECEIPT_PROOF_CANCELLED" : "COSMIC_PAYLOAD_RECEIPT_PROOF_UNVERIFIED",
                "Frozen source transport does not have verified original receipt equivalence.", cause);
        }
    }

    private static OASISResult<HolonReplicationPayloadReceiptProof> Failure(OASISResult<HolonReplicationPayloadReceiptProof> result,
        string code, string message, Exception cause = null)
    {
        result.Result = null; result.IsLoaded = false; result.IsError = true; result.ErrorCount = Math.Max(1, result.ErrorCount);
        result.ErrorCode = code; result.Message = message;
        if (cause != null && result.Exception != cause) result.Exception = result.Exception == null ? cause : new AggregateException(result.Exception, cause);
        return result;
    }

    /// <summary>All native frozen-payload readers share this exact observation CAS
    /// check. A changed lease/state invalidates the read; no claim/reset is used.</summary>
    public static void ValidateObservation(HolonReplicationOperationObligation expected, HolonReplicationOperationObligation actual)
    {
        if (expected == null || actual == null || expected.Sequence != actual.Sequence || expected.OperationId != actual.OperationId
            || expected.ActorId != actual.ActorId || expected.SourceId != actual.SourceId || expected.SourceAuthority != actual.SourceAuthority
            || expected.TargetId != actual.TargetId || expected.PayloadDigest != actual.PayloadDigest || expected.StateDigest != actual.StateDigest)
            throw new InvalidDataException("The original obligation changed after its inventory observation.");
    }

    public static HolonReplicationPayloadReceiptProof Verify(string frozenPayload, HolonReplicationOperationObligation row,
        ConditionalHolonWriteRequest original, HolonReplicationReceiptPreflightReport receipt,
        HolonReplicaHeadReadConsistency requiredSourceConsistency = HolonReplicaHeadReadConsistency.NativeSnapshot)
    {
        ArgumentNullException.ThrowIfNull(row); ArgumentNullException.ThrowIfNull(original); ArgumentNullException.ThrowIfNull(receipt);
        if (!Enum.IsDefined(typeof(HolonReplicaHeadReadConsistency), requiredSourceConsistency)) throw new ArgumentOutOfRangeException(nameof(requiredSourceConsistency));
        var enrollment = original.Write.ReplicationEnrollment;
        var primary = ConditionalHolonPlan.CreateReplicating(original, original.Write.Authority, enrollment);
        if (enrollment == null || !enrollment.TargetIds.Contains(row.TargetId)
            || receipt.OperationId != row.OperationId || receipt.ActorId != row.ActorId || receipt.Operation != original.Operation
            || original.Write.OperationId != row.OperationId || original.Write.ActorId != row.ActorId
            || row.SourceAuthority != original.Write.Authority || receipt.SourceAuthority != row.SourceAuthority
            || receipt.OriginalSourceId != row.SourceId || enrollment.SourceId != row.SourceId
            || receipt.OriginalRequestFingerprint != primary.Fingerprint || !receipt.IsCompleteOriginalGraph
            || receipt.OriginalSelectedRecordCount != primary.Nodes.Count
            || row.NativeRoute != null && (row.NativeRoute.SourceStorage.BindingFingerprint != receipt.SourceStorageFingerprint
                || row.NativeRoute.TargetStorage.BindingFingerprint != receipt.TargetStorageFingerprint)
            || enrollment.SourceStorageIdentity != null && enrollment.SourceStorageIdentity.BindingFingerprint != receipt.SourceStorageFingerprint)
            throw new InvalidDataException("Complete original source receipt, enrollment and selected obligation must agree.");
        if (frozenPayload == null || Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(frozenPayload))) != row.PayloadDigest)
            throw new InvalidDataException("Frozen transport bytes differ from the observed source payload digest.");
        var transport = CommittedHolonReplicaPayload.ReadForTarget(frozenPayload, row.TargetId);
        if (transport.OperationId != row.OperationId || transport.ActorId != row.ActorId || transport.SourceId != row.SourceId
            || transport.SourceAuthority != row.SourceAuthority || transport.Operation != original.Operation
            || transport.SaveChildren != original.SaveChildren || transport.Recursive != original.Recursive
            || transport.MaxChildDepth != original.MaxChildDepth || transport.NativeRoutes.Count != enrollment.NativeRoutes.Count
            || transport.NativeRoutes.Count > 0 && HolonReplicationRouteDescriptors.Capture(transport.NativeRoutes) != HolonReplicationRouteDescriptors.Capture(enrollment.NativeRoutes))
            throw new InvalidDataException("Frozen transport identity, selection or native route descriptors differ from the source receipt intent.");
        var replica = CommittedHolonReplicaPlan.Create(transport, receipt.TargetAuthority);
        if (!replica.RootIds.SequenceEqual(primary.RootIds)
            || !replica.Nodes.Select(n => n.Snapshot.Id).ToHashSet().SetEquals(primary.Nodes.Select(n => n.Snapshot.Id)))
            throw new InvalidDataException("Frozen transport roots or selected membership differ from the original graph.");
        var evidence = receipt.Records.ToDictionary(r => r.Original.HolonId, r => r.Original);
        foreach (var node in replica.Nodes)
        {
            var record = node.Snapshot;
            if (!evidence.TryGetValue(record.Id, out var committed)
                || !replica.ChildIds[record.Id].SequenceEqual(primary.ChildIds[record.Id])
                || record.VersionId != committed.VersionId || record.PreviousVersionId != committed.PreviousVersionId
                || record.Version != committed.Version || CosmicConcurrency.GetProviderRevisionId(record, row.SourceAuthority) != committed.SourceProviderRevisionId
                || committed.HardDeleted != (transport.Operation == ConditionalHolonOperation.HardDelete)
                || CommittedHolonReplicaPlan.PortableContentDigest(record, receipt.TargetAuthority) != committed.PortableContentDigest)
                throw new InvalidDataException("Frozen transport topology, content or committed logical/source revisions differ from the original receipt.");
        }
        return new(row, receipt, replica.Fingerprint, requiredSourceConsistency);
    }
}
