namespace Application.DTOs.Network.Responses;

/// <summary>
/// Represents the response data returned after a network is deleted.
/// This record contains the unique identifier of the deleted network.
/// </summary>
/// <param name="Id">
/// The unique identifier of the deleted network.
/// </param>
public record DeleteNetworkResponse(
    Guid Id);