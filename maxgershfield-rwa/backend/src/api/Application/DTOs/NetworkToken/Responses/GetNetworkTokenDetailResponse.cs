namespace Application.DTOs.NetworkToken.Responses;

/// <summary>
/// Provides detailed information about a specific network token,
/// typically used for displaying full token data in views or APIs.
/// </summary>
/// <param name="Id">The unique identifier of the network token.</param>
/// <param name="Symbol">The short code or symbol representing the token (e.g., ETH, USDT).</param>
/// <param name="Description">An optional human-readable description of the token.</param>
/// <param name="NetworkId">The identifier of the blockchain network to which the token belongs.</param>
public record GetNetworkTokenDetailResponse(
    Guid Id,
    string Symbol,
    string? Description,
    Guid NetworkId
);