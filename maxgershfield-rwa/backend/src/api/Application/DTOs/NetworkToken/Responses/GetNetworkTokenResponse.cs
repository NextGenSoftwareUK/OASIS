namespace Application.DTOs.NetworkToken.Responses;

/// <summary>
/// Represents a summary view of a network token returned in a list response.
/// </summary>
/// <param name="Id">The unique identifier of the network token.</param>
/// <param name="Symbol">The symbol representing the token (e.g., BTC, ETH).</param>
/// <param name="Description">A brief optional description of the token.</param>
/// <param name="NetworkId">The identifier of the associated blockchain network.</param>
public record GetNetworkTokenResponse(
    Guid Id,
    string Symbol,
    string? Description,
    Guid NetworkId
);