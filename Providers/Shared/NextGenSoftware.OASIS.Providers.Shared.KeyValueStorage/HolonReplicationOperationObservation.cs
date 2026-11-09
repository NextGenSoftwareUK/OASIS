using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Only after native lifecycle/payload validation. Hidden lease/recovery
/// values participate in the checksum but are not exposed as evidence fields.</summary>
public static class HolonReplicationOperationObservation
{
    public static HolonReplicationOperationObligation Capture(HolonDeliveryInspection state, string sourceId,
        ProviderType authority, Guid actor, string payloadDigest, string leaseToken, string workerId,
        long leaseUntilTicks, long nextAttemptTicks, long completedAtTicks, string recoveryDigest)
    {
        ArgumentNullException.ThrowIfNull(state);
        foreach (var value in new[] { leaseUntilTicks, nextAttemptTicks, completedAtTicks })
            if (value < 0 || value > DateTime.MaxValue.Ticks) throw new ArgumentOutOfRangeException(nameof(leaseUntilTicks));
        var material = JsonSerializer.Serialize(new { format = "cosmic-obligation-observation-v1", state.Sequence,
            state.OperationId, state.TargetId, state.State, state.AttemptCount, state.ErrorCode,
            sourceId, authority, actor, payloadDigest, leaseToken, workerId,
            leaseUntilTicks, nextAttemptTicks, completedAtTicks, recoveryDigest, state.NativeRoute });
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));
        return new(state, sourceId, authority, actor, payloadDigest, digest);
    }
}
