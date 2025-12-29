namespace Application.DTOs.VirtualAccount.Responses;

/// <summary>
/// Represents detailed information about a user's virtual account across networks.
/// </summary>
/// <param name="Address">
/// Blockchain address of the virtual account.
/// </param>
/// <param name="Network">
/// Name of the blockchain network (e.g., Solana, Radix).
/// </param>
/// <param name="Token">
/// Symbol of the token associated with the account (e.g., SOL, XRD).
/// </param>
/// <param name="Balance">
/// Current balance of the account in the specified token.
/// </param>
public record GetVirtualAccountDetailResponse(
    string Address,
    string Network,
    string Token,
    decimal Balance
);