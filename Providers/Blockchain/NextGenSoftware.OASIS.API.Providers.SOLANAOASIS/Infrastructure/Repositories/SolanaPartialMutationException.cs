using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public sealed class SolanaPartialMutationException:Exception,INativeMutationReconciliationRequired
{
    public IReadOnlyList<string> CommittedSignatures {get;}
    public string OriginalTransactionId => string.Join("|",CommittedSignatures);
    public SolanaPartialMutationException(IEnumerable<string> committedSignatures,Exception cause)
        :base("Solana batch committed native transactions before failing; reconcile before cross-provider replay.",cause)
    {
        var signatures=committedSignatures?.ToArray() ?? throw new ArgumentNullException(nameof(committedSignatures));
        if(signatures.Length==0 || signatures.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Committed native signatures are required.",nameof(committedSignatures));
        CommittedSignatures=Array.AsReadOnly(signatures);
    }
}
