using System.Threading;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.STARAPI.Client;

internal sealed class PendingAddItemJob
{
    public PendingAddItemJob(string itemName, string description, string gameSource, string itemType, string? nftId, int quantity, bool stack, CancellationToken cancellationToken, TaskCompletionSource<OASISResult<StarItem>>? completion)
    {
        ItemName = itemName;
        Description = description;
        GameSource = gameSource;
        ItemType = string.IsNullOrWhiteSpace(itemType) ? "KeyItem" : itemType;
        NftId = string.IsNullOrWhiteSpace(nftId) ? null : nftId;
        Quantity = quantity < 1 ? 1 : quantity;
        Stack = stack;
        CancellationToken = cancellationToken;
        Completion = completion;
    }

    public string ItemName { get; }
    public string Description { get; }
    public string GameSource { get; }
    public string ItemType { get; }
    public string? NftId { get; }
    public int Quantity { get; }
    public bool Stack { get; }
    public CancellationToken CancellationToken { get; }
    public TaskCompletionSource<OASISResult<StarItem>>? Completion { get; }
}
