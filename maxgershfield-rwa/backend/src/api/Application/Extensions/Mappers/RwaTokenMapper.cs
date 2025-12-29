namespace Application.Extensions.Mappers;

public static class RwaTokenMapper
{
    public static GetRwaTokensResponse ToRead(this RwaToken entity)
        => new(
            entity.Id,
            entity.Title,
            entity.Price,
            entity.AssetType,
            entity.InsuranceStatus,
            entity.Geolocation,
            entity.Image,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);

    public static CreateRwaTokenResponse ToCreateResponse(this RwaToken entity)
        => new(
            entity.Id,
            entity.Title,
            entity.Price,
            entity.MintAccountType.ToString(),
            entity.Royalty,
            entity.OwnerContact,
            entity.Metadata,
            entity.MintAccount,
            entity.TransactionHash,
            entity.Version);


    public static UpdateRwaTokenResponse ToUpdateResponse(this RwaToken entity)
        => new(
            entity.Id,
            entity.Title,
            entity.Price,
            entity.MintAccountType.ToString(),
            entity.Royalty,
            entity.OwnerContact,
            entity.Metadata,
            entity.MintAccount,
            entity.TransactionHash,
            entity.Version);


    public static GetRwaTokenDetailResponse ToReadDetail(this RwaToken entity)
        => new(
            entity.Id,
            entity.Title,
            entity.AssetDescription,
            entity.ProofOfOwnershipDocument,
            entity.UniqueIdentifier,
            entity.Royalty,
            entity.Price,
            entity.VirtualAccount?.Network.Name
            ?? entity.WalletLinkedAccount?.Network.Name,
            entity.Image,
            entity.OwnerContact,
            entity.AssetType,
            entity.InsuranceStatus,
            entity.Geolocation,
            entity.ValuationDate,
            entity.PropertyType,
            entity.Area,
            entity.ConstructionYear,
            entity.Metadata,
            entity.MintAccount,
            entity.TransactionHash,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.VirtualAccount?.User.Email
            ?? entity.WalletLinkedAccount?.User.Email,
            entity.VirtualAccount?.User.UserName
            ?? entity.WalletLinkedAccount?.User.UserName
        );

    public static RwaToken ToEntity(this CreateRwaTokenRequest request, IHttpContextAccessor accessor,
        NftMintingResponse minting, Guid vaId)
    {
        return new()
        {
            Area = request.Area,
            ValuationDate = request.ValuationDate,
            PropertyType = request.PropertyType,
            Geolocation = request.Geolocation,
            Image = request.Image,
            AssetType = request.AssetType,
            InsuranceStatus = request.InsuranceStatus,
            Metadata = minting.Metadata,
            AssetDescription = request.AssetDescription,
            ConstructionYear = request.ConstructionYear,
            Price = request.Price,
            MintAccount = minting.MintAccount,
            Royalty = request.Royalty,
            TransactionHash = minting.TransactionHash,
            OwnerContact = request.OwnerContact,
            UniqueIdentifier = request.UniqueIdentifier,
            ProofOfOwnershipDocument = request.ProofOfOwnershipDocument,
            Title = request.Title,
            CreatedBy = accessor.GetId(),
            CreatedByIp = accessor.GetRemoteIpAddress(),
            MintAccountType = request.Network == Networks.Solana
                ? NetworkType.Solana
                : NetworkType.Radix,
            VirtualAccountId = vaId
        };
    }

    public static RwaToken ToEntity(this RwaToken rwaToken,
        UpdateRwaTokenRequest request,
        IHttpContextAccessor accessor,
        NftMintingResponse nftMintingResponse)
    {
        rwaToken.AssetType = request.AssetType;
        rwaToken.Title = request.Title;
        rwaToken.OwnerContact = request.OwnerContact;
        rwaToken.ValuationDate = request.ValuationDate;
        rwaToken.AssetDescription = request.AssetDescription;
        rwaToken.ProofOfOwnershipDocument = request.ProofOfOwnershipDocument;
        rwaToken.Price = request.Price;
        rwaToken.Royalty = request.Royalty;
        rwaToken.Update(accessor.GetId());
        rwaToken.UpdatedByIp?.Add(accessor.GetRemoteIpAddress());
        rwaToken.Metadata = nftMintingResponse.Metadata;
        rwaToken.MintAccount = nftMintingResponse.MintAccount;
        rwaToken.TransactionHash = nftMintingResponse.TransactionHash;
        rwaToken.MintAccountType = nftMintingResponse.Network == Networks.Solana
            ? NetworkType.Solana
            : NetworkType.Radix;
        return rwaToken;
    }
}