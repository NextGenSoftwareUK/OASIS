namespace Application.DTOs.NetworkToken.Requests;

/// <summary>
/// Represents the data required to create a new network token.
/// </summary>
/// <param name="Symbol">The unique symbol of the token (e.g., ETH, USDT). Must be non-empty and unique within a network.</param>
/// <param name="Description">An optional description providing additional context about the token.</param>
/// <param name="NetworkId">The identifier of the network to which this token belongs.</param>
public record CreateNetworkTokenRequest(
    string Symbol,
    string? Description,
    Guid NetworkId
);