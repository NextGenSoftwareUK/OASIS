using System.Threading;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STARAPI.Client;

internal sealed class PendingUseItemJob
{
    public PendingUseItemJob(string itemName, string? context, int quantity, CancellationToken cancellationToken, TaskCompletionSource<OASISResult<bool>>? completion)
    {
        ItemName = itemName;
        Context = context;
        Quantity = quantity > 0 ? quantity : 1;
        CancellationToken = cancellationToken;
        Completion = completion;
    }

    public string ItemName { get; }
    public string? Context { get; }
    public int Quantity { get; }
    public CancellationToken CancellationToken { get; }
    public TaskCompletionSource<OASISResult<bool>>? Completion { get; }
}
