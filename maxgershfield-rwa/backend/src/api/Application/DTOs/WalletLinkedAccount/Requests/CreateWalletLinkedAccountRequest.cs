namespace Application.DTOs.WalletLinkedAccount.Requests;

/// <summary>
/// Represents a request to create a linked wallet account.
/// </summary>
/// <param name="WalletAddress">
/// The address of the wallet that is being linked.
/// </param>
/// <param name="Network">
/// The name of the blockchain network to which the wallet is associated (e.g., Ethereum, Bitcoin).
/// </param>
public sealed record CreateWalletLinkedAccountRequest(
    string WalletAddress,
    string Network
);