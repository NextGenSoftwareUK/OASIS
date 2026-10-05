namespace Application.DTOs.NetworkToken.Responses;

/// <summary>
/// Represents the result of a successful network token creation operation.
/// </summary>
/// <param name="Id">
/// The unique identifier assigned to the newly created network token.
/// </param>
public record CreateNetworkTokenResponse(
    Guid Id
);