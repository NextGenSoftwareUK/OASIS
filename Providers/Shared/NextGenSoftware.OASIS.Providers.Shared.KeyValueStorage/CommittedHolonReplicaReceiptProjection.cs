using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>One owner for historical replica receipt validation. Uses the exact
/// validator already used by write replay, without reading today's record/head.</summary>
public static class CommittedHolonReplicaReceiptProjection
{
    public static CommittedHolonReplicaPlan Prepare(CommittedHolonReplicaReceiptReadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        CommittedHolonReplicaPlan.ValidateNativeDestination(request.Original, request.ExpectedStorage);
        var plan = CommittedHolonReplicaPlan.Create(request.Original, request.ExpectedStorage.Authority);
        if (plan.Nodes.Count < 1 || plan.Nodes.Count > 256) throw new ArgumentException("Historical receipt reviews select 1–256 records.");
        return plan;
    }

    public static CommittedHolonReplicaReceiptEvidence Project(CommittedHolonReplicaReceiptReadRequest request,
        CommittedHolonReplicaPlan plan, string storedFingerprint, string storedAcknowledgement,
        HolonReplicaHeadReadConsistency consistency)
    {
        ArgumentNullException.ThrowIfNull(plan);
        // Bind a supplied prepared plan to this exact request before consuming it.
        var expected = Prepare(request);
        if (expected.Fingerprint != plan.Fingerprint)
            throw new InvalidDataException("The original committed transport changed during receipt observation.");
        if (storedFingerprint == null && storedAcknowledgement == null)
            return new(request, plan.Fingerprint, ConditionalHolonReceiptState.Absent, null, plan.Nodes.Count, consistency);
        if (storedFingerprint != plan.Fingerprint || storedAcknowledgement == null)
            throw new InvalidDataException("Historical receipt is bound to another intent or lacks an acknowledgement.");
        _ = ConditionalHolonReceipt.Read(storedAcknowledgement, plan);
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(storedAcknowledgement)));
        return new(request, plan.Fingerprint, ConditionalHolonReceiptState.Committed, digest, plan.Nodes.Count, consistency);
    }
}
