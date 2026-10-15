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
            Username = avatarDetailDto.Username,
            Email = avatarDetailDto.Email,
            FirstName = avatarDetailDto.FirstName,
            LastName = avatarDetailDto.LastName,
            ModifiedDate = avatarDetailDto.ModifiedDate,
            AvatarType = ParseStoredAvatarType(avatarDetailDto.AvatarType),
            Description = avatarDetailDto.Description,
            Country = avatarDetailDto.Country,
            Town = avatarDetailDto.Town,
            County = avatarDetailDto.County,
            Postcode = avatarDetailDto.Postcode,
            Landline = avatarDetailDto.Landline,
            Title = avatarDetailDto.Title,
            UmaJson = avatarDetailDto.UmaJson,
            Portrait = avatarDetailDto.Portrait,
            Model3D = avatarDetailDto.Model3D,
            MetaData = avatarDetailDto.MetaData,
            DOB = avatarDetailDto.DOB,
            CreatedDate = avatarDetailDto.CreatedDate,
            XP = avatarDetailDto.XP,
            Karma = avatarDetailDto.Karma,
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
            IsActive = !avatarDetailDto.IsDeleted,
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
            FirstName = avatarDto.FirstName,
            LastName = avatarDto.LastName,
            CreatedDate = avatarDto.CreatedDate,
            ModifiedDate = avatarDto.ModifiedDate,
            AvatarType = ParseStoredAvatarType(avatarDto.AvatarType),
            Description = avatarDto.Description,
            MetaData = avatarDto.MetaData,
            Version = avatarDto.Version,
            IsActive = !avatarDto.IsDeleted,
            PreviousVersionId = avatarDto.PreviousVersionId
        };
    }

    private static NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType> ParseStoredAvatarType(string stored)
    {
        if(stored == null) return null;
        if(!Enum.TryParse<Core.Enums.AvatarType>(stored,out var parsed) || !Enum.IsDefined(typeof(Core.Enums.AvatarType),parsed))
            throw new InvalidOperationException("Stored Solana avatar type is invalid.");
        return new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(parsed);
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
            ParentHolonId = holonDto.ParentHolonId,
            HolonType = holonDto.HolonType,
            CustomHolonType = holonDto.CustomHolonType,
            CreatedByAvatarId = holonDto.CreatedByAvatarId,
            ModifiedByAvatarId = holonDto.ModifiedByAvatarId,
            DeletedByAvatarId = holonDto.DeletedByAvatarId,
            DeletedDate = holonDto.DeletedDate,
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
            ParentPlanetId = holonDto.ParentPlanetId,
            ParentMoonId = holonDto.ParentMoonId,
            ParentCelestialSpaceId = holonDto.ParentCelestialSpaceId,
            ParentCelestialBodyId = holonDto.ParentCelestialBodyId,
            ParentZomeId = holonDto.ParentZomeId,
            Version = holonDto.Version,
            IsActive = !holonDto.IsDeleted,
            PreviousVersionId = holonDto.PreviousVersionId
        };
    }
}
