namespace Application.DTOs.WalletLinkedAccount.Responses;

/// <summary>
/// Represents the response that contains details about a linked wallet account for a user.
/// </summary>
/// <param name="UserId">
/// The unique identifier of the user associated with the linked wallet.
/// </param>
/// <param name="WalletAddress">
/// The address of the wallet that is linked to the user.
/// </param>
/// <param name="Network">
/// The name of the blockchain network that the wallet is connected to (e.g., Ethereum, Bitcoin).
/// </param>
/// <param name="LinkedAt">
/// The timestamp when the wallet was linked to the user's account.
/// </param>
public sealed record GetWalletLinkedAccountDetailResponse(
    Guid UserId,
    string WalletAddress,
    string Network,
    DateTimeOffset LinkedAt
);