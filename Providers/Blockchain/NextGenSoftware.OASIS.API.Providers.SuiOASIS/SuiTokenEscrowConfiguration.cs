using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS;

public sealed record SuiTokenEscrowConfiguration(string EscrowObjectId)
{
    public void Validate()
    {
        if (EscrowObjectId?.Length != 66 || !EscrowObjectId.StartsWith("0x", StringComparison.Ordinal))
            throw new ArgumentException("Explicit canonical 32-byte shared escrow object ID required.");
        foreach (var c in EscrowObjectId.AsSpan(2))
            if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f'))
                throw new ArgumentException("Escrow object ID must be canonical lowercase hexadecimal.");
    }
}

public sealed class SuiIdBoundEscrowResponse : ITransactionResponse
{
    public string TransactionResult { get; set; }
    public Guid LockId { get; }
    public IReadOnlyList<string> NativeTransactions { get; }
    internal SuiIdBoundEscrowResponse(Guid id, string digest)
    {
        LockId = id; TransactionResult = digest; NativeTransactions = Array.AsReadOnly(new[] { digest });
    }
}
