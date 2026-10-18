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
            VersionId = avatarDetailDto.VersionId,
            IsPublic = avatarDetailDto.IsPublic,
            CreatedProviderType = avatarDetailDto.CreatedProviderType.HasValue ? new EnumValue<ProviderType>(avatarDetailDto.CreatedProviderType.Value) : null,
            CreatedOASISType = avatarDetailDto.CreatedOASISType.HasValue ? new EnumValue<OASISType>(avatarDetailDto.CreatedOASISType.Value) : null,
            Address = avatarDetailDto.Address,
            CreatedByAvatarId = avatarDetailDto.CreatedByAvatarId,
            ModifiedByAvatarId = avatarDetailDto.ModifiedByAvatarId,
            DeletedByAvatarId = avatarDetailDto.DeletedByAvatarId,
            DeletedDate = avatarDetailDto.DeletedDate,
            ParentHolonId = avatarDetailDto.ParentHolonId,
            CustomHolonType = avatarDetailDto.CustomHolonType,
            Mobile = avatarDetailDto.Mobile,
            Username = avatarDetailDto.Username,
            ActiveQuestId = avatarDetailDto.ActiveQuestId,
            ActiveObjectiveId = avatarDetailDto.ActiveObjectiveId,
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
            VersionId = avatarDto.VersionId,
            IsPublic = avatarDto.IsPublic,
            CreatedProviderType = avatarDto.CreatedProviderType.HasValue ? new EnumValue<ProviderType>(avatarDto.CreatedProviderType.Value) : null,
            CreatedOASISType = avatarDto.CreatedOASISType.HasValue ? new EnumValue<OASISType>(avatarDto.CreatedOASISType.Value) : null,
            AvatarId = avatarDto.AvatarId,
            Title = avatarDto.Title,
            AcceptTerms = avatarDto.AcceptTerms,
            PasswordReset = avatarDto.PasswordReset,
            Verified = avatarDto.Verified,
            LastBeamedIn = avatarDto.LastBeamedIn,
            LastBeamedOut = avatarDto.LastBeamedOut,
            IsBeamedIn = avatarDto.IsBeamedIn,
            DID = avatarDto.DID,
            DIDPublicKey = avatarDto.DIDPublicKey,
            HerzId = avatarDto.HerzId,
            HerzCountryCode = avatarDto.HerzCountryCode,
            HerzSequentialNumber = avatarDto.HerzSequentialNumber,
            HerzClearanceLevel = avatarDto.HerzClearanceLevel,
            HerzVoucherId = avatarDto.HerzVoucherId,
            HerzVouchesRemaining = avatarDto.HerzVouchesRemaining,
            HerzIdAssignedDate = avatarDto.HerzIdAssignedDate,
            HerzVoiceprintId = avatarDto.HerzVoiceprintId,
            HerzQeaProfile = avatarDto.HerzQeaProfile,
            VoiceprintId = avatarDto.VoiceprintId,
            BiometricEnrolled = avatarDto.BiometricEnrolled,
            ProviderUsername = avatarDto.ProviderUsername == null ? null : new Dictionary<ProviderType,string>(avatarDto.ProviderUsername),
            CreatedByAvatarId = avatarDto.CreatedByAvatarId,
            ModifiedByAvatarId = avatarDto.ModifiedByAvatarId,
            DeletedByAvatarId = avatarDto.DeletedByAvatarId,
            DeletedDate = avatarDto.DeletedDate,
            ParentHolonId = avatarDto.ParentHolonId,
            CustomHolonType = avatarDto.CustomHolonType,
            Email = avatarDto.Email,
            Password = avatarDto.Password,
            VerificationToken = avatarDto.VerificationToken,
            ResetToken = avatarDto.ResetToken,
            ResetTokenExpires = avatarDto.ResetTokenExpires,
            RefreshToken = avatarDto.RefreshToken,
            RefreshTokens = avatarDto.RefreshTokens ?? new(),
            ProviderWallets = NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.DeserializeStored<Dictionary<ProviderType,List<IProviderWallet>>>(avatarDto.ProviderWalletsJson)
                ?? throw new System.IO.InvalidDataException("Stored Solana avatar wallets must be a JSON object."),
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
            VersionId = holonDto.VersionId,
            IsPublic = holonDto.IsPublic,
            CreatedProviderType = holonDto.CreatedProviderType.HasValue ? new EnumValue<ProviderType>(holonDto.CreatedProviderType.Value) : null,
            CreatedOASISType = holonDto.CreatedOASISType.HasValue ? new EnumValue<OASISType>(holonDto.CreatedOASISType.Value) : null,
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
