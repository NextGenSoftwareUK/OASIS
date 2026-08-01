namespace NextGenSoftware.OASIS.STARAPI.Client;

internal sealed class PendingPickupWithMintJob
{
    public PendingPickupWithMintJob(string itemName, string description, string gameSource, string itemType, bool doMint, string? provider, string? sendToAddressAfterMinting, int quantity)
    {
        ItemName = itemName;
        Description = description;
        GameSource = gameSource;
        ItemType = itemType;
        DoMint = doMint;
        Provider = provider;
        SendToAddressAfterMinting = sendToAddressAfterMinting;
        Quantity = quantity < 1 ? 1 : quantity;
    }

    public string ItemName { get; }
    public string Description { get; }
    public string GameSource { get; }
    public string ItemType { get; }
    public bool DoMint { get; }
    public string? Provider { get; }
    public string? SendToAddressAfterMinting { get; }
    public int Quantity { get; }
}
