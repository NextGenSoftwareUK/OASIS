using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public sealed class SolanaTransactionReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId { get; }
    public SolanaTransactionReconciliationException(string signature, Exception cause)
        : base("Submitted Solana transaction requires reconciliation before replay.", cause)
        => OriginalTransactionId = signature;
}
