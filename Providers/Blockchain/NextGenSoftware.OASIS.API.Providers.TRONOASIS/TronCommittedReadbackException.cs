using System;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>A confirmed original submission whose application readback did not establish success. Never retry the mutation.</summary>
public sealed class TronCommittedReadbackException : InvalidOperationException,Core.Interfaces.INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    public string ChainGenesisId { get; }
    public TronCommittedReadbackException(string transactionId,string genesisId,Exception cause)
        :base($"TRON original transaction {transactionId} was confirmed but readback failed; reconcile before retrying.",cause)
    {
        if(!Regex.IsMatch(transactionId??"",@"\A[0-9a-f]{64}\z")||!Regex.IsMatch(genesisId??"",@"\A[0-9a-fA-F]{64}\z"))
            throw new ArgumentException("Exact confirmed original transaction and chain identity required.");
        ArgumentNullException.ThrowIfNull(cause);
        OriginalTransactionId=transactionId;ChainGenesisId=genesisId.ToLowerInvariant();
    }
}
