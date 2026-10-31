using Solnet.Rpc;
using Solnet.Rpc.Models;
using Solnet.Rpc.Types;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

// Existing model-level storage/history fixtures deliberately use synthetic IDs.
// Inject their typed dependency explicitly; production always uses the public
// version-capable SDK reader. Actual wire tests do not use this fixture adapter.
internal static class SolanaReadFixture
{
    internal static Func<string,Task<TransactionMetaSlotInfo>> From(IRpcClient rpc) => async signature =>
    {
        var response=await rpc.GetTransactionAsync(signature,Commitment.Finalized);
        if(response == null || !response.WasSuccessful || response.HttpStatusCode!=System.Net.HttpStatusCode.OK)
            throw new InvalidOperationException("Controlled transaction source failed.");
        return response.Result;
    };

    internal static Func<string,CancellationToken,Task<TransactionMetaSlotInfo>> WithCancellation(IRpcClient rpc) =>
        (signature,cancellationToken)=>{cancellationToken.ThrowIfCancellationRequested();return From(rpc)(signature);};
}
