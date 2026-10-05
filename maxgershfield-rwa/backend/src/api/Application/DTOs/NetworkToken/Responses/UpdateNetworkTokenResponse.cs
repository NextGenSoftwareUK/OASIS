namespace Application.DTOs.NetworkToken.Responses;

/// <summary>
/// Represents the response after updating a network token.
/// </summary>
/// <param name="Id">
/// The identifier of the updated network token.
/// </param>
public record UpdateNetworkTokenResponse(
    Guid Id
);