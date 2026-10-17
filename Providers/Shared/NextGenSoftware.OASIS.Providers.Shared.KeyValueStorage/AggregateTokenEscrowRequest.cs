using System;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Validation for an explicitly selected lock-all aggregate escrow ABI.
/// Does not provide amount-aware/individual-deposit support or authorize spending.</summary>
public static class AggregateTokenEscrowRequest
{
    public static void Validate(decimal amount, Guid lockId, string requestedEscrow, string configuredEscrow)
    {
        if (amount != 0)
            throw new ArgumentException("Configured aggregate escrow supports lock-all only; a nonzero Amount cannot be replaced with the whole balance.");
        if (lockId != Guid.Empty)
            throw new ArgumentException("Configured escrow tracks an aggregate token/depositor balance, not individual LockId deposits.");
        if (!string.IsNullOrWhiteSpace(requestedEscrow)
            && !string.Equals(requestedEscrow, configuredEscrow, StringComparison.Ordinal))
            throw new ArgumentException("Requested escrow differs from the explicitly configured aggregate escrow contract.");
    }
}
