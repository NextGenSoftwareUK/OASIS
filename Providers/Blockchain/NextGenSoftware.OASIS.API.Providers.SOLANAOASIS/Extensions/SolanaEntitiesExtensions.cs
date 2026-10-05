namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;

public static class SolanaEntitiesExtensions
{
    public static IAvatarDetail GetAvatarDetail(this SolanaAvatarDetailDto avatarDetailDto)
    {
        if (avatarDetailDto == null)
            throw new ArgumentNullException(nameof(avatarDetailDto));
        return new AvatarDetail()
        {
            Id = avatarDetailDto.Id,
            Address = avatarDetailDto.Address,
            Mobile = avatarDetailDto.Mobile,
            Achievements = avatarDetailDto.Achievements,
            Attributes = avatarDetailDto.Attributes,
            Aura = avatarDetailDto.Aura,
            Chakras = avatarDetailDto.Chakras,
            DimensionLevelIds = avatarDetailDto.DimensionLevelIds,
            DimensionLevels = avatarDetailDto.DimensionLevels,
            FavouriteColour = avatarDetailDto.FavouriteColour,
            GeneKeys = avatarDetailDto.GeneKeys,
            Gifts = avatarDetailDto.Gifts,
            HeartRateData = avatarDetailDto.HeartRateData,
            HumanDesign = avatarDetailDto.HumanDesign,
            Inventory = avatarDetailDto.Inventory,
            KarmaAkashicRecords = avatarDetailDto.KarmaAkashicRecords,
            Omniverse = avatarDetailDto.Omniverse,
            Skills = avatarDetailDto.Skills,
            Spells = avatarDetailDto.Spells,
            STARCLIColour = avatarDetailDto.STARCLIColour,
            Stats = avatarDetailDto.Stats,
            SuperPowers = avatarDetailDto.SuperPowers,
            Version = avatarDetailDto.Version,
            IsActive = avatarDetailDto.IsDeleted,
            PreviousVersionId = avatarDetailDto.PreviousVersionId
        };
    }

    public static IAvatar GetAvatar(this SolanaAvatarDto avatarDto)
    {
        if (avatarDto == null)
            throw new ArgumentNullException(nameof(avatarDto));
        return new Avatar()
        {
            Id = avatarDto.Id,
            AvatarId = avatarDto.AvatarId,
            Email = avatarDto.Email,
            Password = avatarDto.Password,
            Username = avatarDto.UserName,
            Version = avatarDto.Version,
            IsActive = avatarDto.IsDeleted,
            PreviousVersionId = avatarDto.PreviousVersionId
        };
    }

    public static IHolon GetHolon(this SolanaHolonDto holonDto)
    {
        if (holonDto == null)
            throw new ArgumentNullException(nameof(holonDto));

        return new Holon()
        {
            Id = holonDto.Id,
            Name = holonDto.Name,
            Description = holonDto.Description,
            CreatedDate = holonDto.CreatedDate,
            ModifiedDate = holonDto.ModifiedDate,
            MetaData = holonDto.MetaData,
            ParentOmniverseId = holonDto.ParentOmniverseId,
            ParentOmniverse = holonDto.ParentOmniverse,
            ParentMultiverseId = holonDto.ParentMultiverseId,
            ParentMultiverse = holonDto.ParentMultiverse,
            ParentUniverseId = holonDto.ParentUniverseId,
            ParentUniverse = holonDto.ParentUniverse,
            ParentDimensionId = holonDto.ParentDimensionId,
            ParentDimension = holonDto.ParentDimension,
            DimensionLevel = holonDto.DimensionLevel,
            SubDimensionLevel = holonDto.SubDimensionLevel,
            ParentGalaxyClusterId = holonDto.ParentGalaxyClusterId,
            ParentGalaxyCluster = holonDto.ParentGalaxyCluster,
            ParentGalaxyId = holonDto.ParentGalaxyId,
            ParentGalaxy = holonDto.ParentGalaxy,
            ParentSolarSystemId = holonDto.ParentSolarSystemId,
            ParentSolarSystem = holonDto.ParentSolarSystem,
            ParentGreatGrandSuperStarId = holonDto.ParentGreatGrandSuperStarId,
            ParentGreatGrandSuperStar = holonDto.ParentGreatGrandSuperStar,
            ParentGrandSuperStarId = holonDto.ParentGrandSuperStarId,
            ParentGrandSuperStar = holonDto.ParentGrandSuperStar,
            ParentSuperStarId = holonDto.ParentSuperStarId,
            ParentSuperStar = holonDto.ParentSuperStar,
            ParentStarId = holonDto.ParentStarId,
            ParentStar = holonDto.ParentStar,
            Version = holonDto.Version,
            IsActive = !holonDto.IsDeleted,
            PreviousVersionId = holonDto.PreviousVersionId
        };
    }
}