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
            Karma = avatarDetail.Karma,
            Mobile = avatarDetail.Mobile,
            Xp = avatarDetail.XP,
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

    public static IOASISNFT ToOasisNft(this GetNftMetadataResult nft) =>
        new OASISNFT
        {
            Title = nft.Name,
            OASISMintWalletAddress = nft.Owner,
            Symbol = nft.Symbol,
            SellerFeeBasisPoints = nft.SellerFeeBasisPoints,
            UpdateAuthority = nft.UpdateAuthority,
            //MintAddress = nft.Owner, //TODO: What is MintAddress?! How is it different to MintByAddress?!
            NFTTokenAddress = nft.Mint, //TODO: What is MintAddress?! How is it different to MintByAddress?!
            JSONMetaDataURL = nft.Url,
            OnChainProvider = new EnumValue<ProviderType>(ProviderType.SolanaOASIS),
            OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        };
    
    
    public static IOASISNFT ToOasisNft(this GetNftResult nft) =>
        new OASISNFT
        {
            Title = nft.Name,
            Symbol = nft.Symbol,
            SellerFeeBasisPoints = nft.SellerFeeBasisPoints,
            JSONMetaDataURL = nft.Url,
            OnChainProvider = new EnumValue<ProviderType>(ProviderType.SolanaOASIS),
            OffChainProvider = new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        };
}