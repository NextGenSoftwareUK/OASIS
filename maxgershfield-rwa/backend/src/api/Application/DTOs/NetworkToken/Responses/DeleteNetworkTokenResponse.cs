namespace Application.DTOs.NetworkToken.Responses;

/// <summary>
/// Represents the response after successfully deleting a network token.
/// </summary>
/// <param name="Id">
/// The unique identifier of the deleted network token.
/// </param>
public record DeleteNetworkTokenResponse(
    Guid Id
);