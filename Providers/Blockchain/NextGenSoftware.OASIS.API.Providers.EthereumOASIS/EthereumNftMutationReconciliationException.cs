using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public sealed class EthereumNftMutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    public EthereumNftMutationReconciliationException(string transactionId, Exception cause)
        : base("Ethereum NFT send outcome requires reconciliation before replay.", cause)
        => OriginalTransactionId = transactionId;
}
