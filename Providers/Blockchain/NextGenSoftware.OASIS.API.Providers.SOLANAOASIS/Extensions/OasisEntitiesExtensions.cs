namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;

public static class OasisEntitiesExtensions
{
    public static SolanaAvatarDto GetSolanaAvatarDto(this IAvatar avatar)
    {
        if (avatar == null)
            throw new ArgumentNullException(nameof(avatar));

        return new SolanaAvatarDto()
        {
            Email = avatar.Email,
            Id = avatar.Id,
            Password = avatar.Password,
            AvatarId = avatar.AvatarId,
            UserName = avatar.Username,
            Version = avatar.Version,
            IsDeleted = avatar.IsActive,
            PreviousVersionId = avatar.PreviousVersionId
        };
    }

    public static SolanaAvatarDetailDto GetSolanaAvatarDetailDto(this IAvatarDetail avatarDetail)
    {
        if (avatarDetail == null)
            throw new ArgumentNullException(nameof(avatarDetail));

        return new SolanaAvatarDetailDto()
        {
            Address = avatarDetail.Address,
            Id = avatarDetail.Id,
            Mobile = avatarDetail.Mobile,
            Level = avatarDetail.Level,
            XP = avatarDetail.XP,
            Karma = avatarDetail.Karma,
            Username = avatarDetail.Username,
            Email = avatarDetail.Email,
            DOB = avatarDetail.DOB,
            CreatedDate = avatarDetail.CreatedDate,
            Version = avatarDetail.Version,
            IsDeleted = avatarDetail.IsActive,
            PreviousVersionId = avatarDetail.PreviousVersionId
        };
    }

    public static SolanaHolonDto GetSolanaHolonDto(this IHolon holon)
    {
        if (holon == null)
            throw new ArgumentNullException(nameof(holon));

        return new SolanaHolonDto()
        {
            Id = holon.Id,
            ParentMultiverseId = holon.ParentMultiverseId,
            ParentOmniverseId = holon.ParentOmniverseId,
            ParentUniverseId = holon.ParentUniverseId,
            Version = holon.Version,
            IsDeleted = holon.IsActive,
            PreviousVersionId = holon.PreviousVersionId,
        };
    }

    public static IWeb3NFT ToOasisNft(this GetNftMetadataResult nft) =>
        new Web3NFT
        {
            Title = nft.Name,
            OASISMintWalletAddress = nft.Owner,
            Symbol = nft.Symbol,
            SellerFeeBasisPoints = nft.SellerFeeBasisPoints,
            UpdateAuthority = nft.UpdateAuthority,
            // MintAddress vs NFTTokenAddress: In Solana, the Mint address is the unique identifier for the NFT token type
            // NFTTokenAddress is the mint address (the token's unique identifier on Solana)
            // OASISMintWalletAddress is the owner's wallet address (who owns the NFT)
            NFTTokenAddress = nft.Mint, // Mint address = unique token identifier on Solana
            JSONMetaDataURL = nft.Url,
            OnChainProvider = new EnumValue<ProviderType>(ProviderType.SolanaOASIS),
            OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        };
    
    
    public static IWeb3NFT ToOasisNft(this GetNftResult nft) =>
        new Web3NFT
        {
            Title = nft.Name,
            Symbol = nft.Symbol,
            SellerFeeBasisPoints = nft.SellerFeeBasisPoints,
            JSONMetaDataURL = nft.Url,
            OnChainProvider = new EnumValue<ProviderType>(ProviderType.SolanaOASIS),
            OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        };
}