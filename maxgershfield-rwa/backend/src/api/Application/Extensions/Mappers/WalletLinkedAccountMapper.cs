namespace Application.Extensions.Mappers;

/// <summary>
/// Provides mapping functionality for converting WalletLinkedAccount-related request models into domain entities.
/// This class includes methods for creating new WalletLinkedAccount entities and mapping them to response models.
/// </summary>
public static class WalletLinkedAccountMapper
{
    /// <summary>
    /// Maps a CreateWalletLinkedAccountRequest to a new WalletLinkedAccount entity.
    /// </summary>
    /// <param name="request">The CreateWalletLinkedAccountRequest containing the wallet link data.</param>
    /// <param name="networkId">The network ID associated with the wallet.</param>
    /// <param name="accessor">The IHttpContextAccessor to retrieve the current user and IP address information.</param>
    /// <returns>A new WalletLinkedAccount entity with the provided data.</returns>
    public static WalletLinkedAccount ToEntity(this CreateWalletLinkedAccountRequest request,
        Guid networkId, IHttpContextAccessor accessor) => new()
        {
            CreatedBy = accessor.GetId(),
            CreatedByIp = accessor.GetRemoteIpAddress(),
            NetworkId = networkId,
            PublicKey = request.WalletAddress,
            UserId = accessor.GetId()
        };

    /// <summary>
    /// Maps a WalletLinkedAccount entity to a GetWalletLinkedAccountDetailResponse, including details about the user, wallet, and network.
    /// </summary>
    /// <param name="entity">The WalletLinkedAccount entity to be mapped.</param>
    /// <returns>A GetWalletLinkedAccountDetailResponse containing the mapped data.</returns>
    public static GetWalletLinkedAccountDetailResponse ToRead(this WalletLinkedAccount entity)
        => new(
            entity.UserId,
            entity.PublicKey,
            entity.Network.Name,
            entity.LinkedAt
        );
}