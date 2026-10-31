using System;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>A confirmed approval cannot be discarded when its subsequent escrow mutation fails.</summary>
public sealed class TronEscrowReconciliationException : InvalidOperationException,Core.Interfaces.INativeMutationReconciliationRequired
{
    public string ApprovalTransactionId { get; }
    public string MutationTransactionId { get; }
    public string OriginalTransactionId => MutationTransactionId??ApprovalTransactionId;
    public string ChainGenesisId { get; }
    public TronEscrowReconciliationException(string approvalId,string genesisId,Exception cause)
        :base($"TRON escrow failed after confirmed approval {approvalId}; reconcile original transactions and allowance before retrying.",cause)
    {
        if(!Regex.IsMatch(approvalId??"",@"\A[0-9a-f]{64}\z")||!Regex.IsMatch(genesisId??"",@"\A[0-9a-fA-F]{64}\z"))
            throw new ArgumentException("Exact confirmed approval and chain identity required.");
        ArgumentNullException.ThrowIfNull(cause);
        ApprovalTransactionId=approvalId;ChainGenesisId=genesisId.ToLowerInvariant();
        MutationTransactionId=cause switch{
            TronBroadcastOutcomeUnknownException unknown=>unknown.OriginalTransactionId,
            TronCommittedReadbackException committed=>committed.OriginalTransactionId,
            _=>null
        };
    }
}
