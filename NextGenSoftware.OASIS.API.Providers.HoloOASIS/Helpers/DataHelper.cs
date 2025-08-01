using System;
using System.Text.Json;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers
{
    public static class DataHelper
    {
        public static bool UseReflection { get; set; }

        public static IHcAvatar ConvertAvatarToHoloOASISAvatar(IAvatar avatar, IHcAvatar hcAvatar)
        {
            hcAvatar.Id = avatar.Id;
            hcAvatar.Username = avatar.Username;
            hcAvatar.Password = avatar.Password;
            hcAvatar.Email = avatar.Email;
            hcAvatar.Title = avatar.Title;
            hcAvatar.Name = avatar.Name;
            hcAvatar.Description = avatar.Description;
            hcAvatar.FirstName = avatar.FirstName;
            hcAvatar.LastName = avatar.LastName;
            hcAvatar.CreatedBy = avatar.CreatedByAvatarId.ToString();
            hcAvatar.CreatedDate = avatar.CreatedDate;
            hcAvatar.ModifiedBy = avatar.ModifiedByAvatarId.ToString();
            hcAvatar.ModifiedDate = avatar.ModifiedDate;
            hcAvatar.DeletedBy = avatar.DeletedByAvatarId.ToString();
            hcAvatar.DeletedDate = avatar.DeletedDate;
            hcAvatar.HolonType = avatar.HolonType;
            hcAvatar.AcceptTerms = avatar.AcceptTerms;
            hcAvatar.AllChildIdListCache = avatar.AllChildIdListCache;
            //hcAvatar.AllChildren = avatar.AllChildren;
            hcAvatar.AvatarType = avatar.AvatarType;
            hcAvatar.ChildIdListCache = avatar.ChildIdListCache;
            hcAvatar.Children = avatar.Children;
            hcAvatar.CreatedOASISType = avatar.CreatedOASISType;
            hcAvatar.CreatedProviderType = avatar.CreatedProviderType;
            //hcAvatar.CustomKey = avatar.CustomKey;
            hcAvatar.IsActive = avatar.IsActive;
            hcAvatar.IsBeamedIn = avatar.IsBeamedIn;
            hcAvatar.JwtToken = avatar.JwtToken;
            hcAvatar.LastBeamedIn = avatar.LastBeamedIn;
            hcAvatar.LastBeamedOut = avatar.LastBeamedOut;
            hcAvatar.MetaData = avatar.MetaData;
            //hcAvatar.ParentHolon = avatar.ParentHolon;
            hcAvatar.ParentHolonId = avatar.ParentHolonId;
            hcAvatar.PasswordReset = avatar.PasswordReset;
            hcAvatar.PreviousVersionId = avatar.PreviousVersionId;
            hcAvatar.PreviousVersionProviderUniqueStorageKey = avatar.PreviousVersionProviderUniqueStorageKey;
            hcAvatar.ProviderMetaData = avatar.ProviderMetaData;
            hcAvatar.ProviderUniqueStorageKey = avatar.ProviderUniqueStorageKey;
            hcAvatar.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = hcAvatar.EntryHash;
            hcAvatar.ProviderUsername = avatar.ProviderUsername;
            hcAvatar.ProviderWallets = avatar.ProviderWallets;
            hcAvatar.RefreshToken = avatar.RefreshToken;
            hcAvatar.RefreshTokens = avatar.RefreshTokens;
            hcAvatar.ResetToken = avatar.ResetToken;
            hcAvatar.ResetTokenExpires = avatar.ResetTokenExpires;
            hcAvatar.VerificationToken = avatar.VerificationToken;
            hcAvatar.Verified = avatar.Verified;
            hcAvatar.Version = avatar.Version;
            hcAvatar.VersionId = avatar.VersionId;

            return hcAvatar;
        }

        public static IAvatar ConvertHcAvatarToAvatar(IHcAvatar hcAvatar)
        {
            Avatar avatar = new Avatar
            {
                Id = hcAvatar.Id,
                Username = hcAvatar.Username,
                Password = hcAvatar.Password,
                Email = hcAvatar.Email,
                Title = hcAvatar.Title,
                Description = hcAvatar.Description,
                FirstName = hcAvatar.FirstName,
                LastName = hcAvatar.LastName,
                CreatedByAvatarId = new Guid(hcAvatar.CreatedBy),
                CreatedDate = hcAvatar.CreatedDate,
                ModifiedByAvatarId = new Guid(hcAvatar.ModifiedBy),
                ModifiedDate = hcAvatar.ModifiedDate,
                DeletedByAvatarId = new Guid(hcAvatar.DeletedBy),
                DeletedDate = hcAvatar.DeletedDate,
                HolonType = hcAvatar.HolonType,
                AcceptTerms = hcAvatar.AcceptTerms,
                AllChildIdListCache = hcAvatar.AllChildIdListCache,
                AvatarType = hcAvatar.AvatarType,
                ChildIdListCache = hcAvatar.ChildIdListCache,
                Children = hcAvatar.Children,
                CreatedOASISType = hcAvatar.CreatedOASISType,
                CreatedProviderType = hcAvatar.CreatedProviderType,
                CustomKey = hcAvatar.CustomKey,
                IsActive = hcAvatar.IsActive,
                IsBeamedIn = hcAvatar.IsBeamedIn,
                JwtToken = hcAvatar.JwtToken,
                LastBeamedIn = hcAvatar.LastBeamedIn,
                LastBeamedOut = hcAvatar.LastBeamedOut,
                MetaData = hcAvatar.MetaData,
                //ParentHolon = hcAvatar.ParentHolon,
                ParentHolonId = hcAvatar.ParentHolonId,
                PasswordReset = hcAvatar.PasswordReset,
                PreviousVersionId = hcAvatar.PreviousVersionId,
                PreviousVersionProviderUniqueStorageKey = hcAvatar.PreviousVersionProviderUniqueStorageKey,
                ProviderMetaData = hcAvatar.ProviderMetaData,
                ProviderUniqueStorageKey = hcAvatar.ProviderUniqueStorageKey,
                ProviderUsername = hcAvatar.ProviderUsername,
                ProviderWallets = hcAvatar.ProviderWallets,
                RefreshToken = hcAvatar.RefreshToken,
                RefreshTokens = hcAvatar.RefreshTokens,
                ResetToken = hcAvatar.ResetToken,
                ResetTokenExpires = hcAvatar.ResetTokenExpires,
                VerificationToken = hcAvatar.VerificationToken,
                Verified = hcAvatar.Verified,
                Version = hcAvatar.Version,
                VersionId = hcAvatar.VersionId
            };

            avatar.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = hcAvatar.EntryHash;
            return avatar;
        }

        public static IAvatar ConvertKeyValuePairToAvatar(Dictionary<string, string> keyValuePair)
        {
            Avatar avatar = new Avatar
            {
                Id = new Guid(keyValuePair["id"]),
                Username = keyValuePair["username"],
                Password = keyValuePair["password"],
                Email = keyValuePair["email"],
                Title = keyValuePair["title"],
                //Name = keyValuePair["name"],
                Description = keyValuePair["description"],
                FirstName = keyValuePair["first_name"],
                LastName = keyValuePair["last_name"],
                CreatedByAvatarId = new Guid(keyValuePair["created_by"]),
                CreatedDate = Convert.ToDateTime(keyValuePair["created_date"]),
                ModifiedByAvatarId = new Guid(keyValuePair["modified_by"]),
                ModifiedDate = Convert.ToDateTime(keyValuePair["modified_date"]),
                DeletedByAvatarId = new Guid(keyValuePair["deleted_by"]),
                DeletedDate = Convert.ToDateTime(keyValuePair["deleted_date"]),
                HolonType = (HolonType)Enum.Parse(typeof(HolonType), keyValuePair["holon_type"]),
                AcceptTerms = Convert.ToBoolean(keyValuePair["accept_terms"]),
                AllChildIdListCache = keyValuePair["all_child_id_list_cache"],
                AvatarType = new Utilities.EnumValue<AvatarType>((AvatarType)Enum.Parse(typeof(AvatarType), keyValuePair["avatar_type"])),
                ChildIdListCache = keyValuePair["child_id_list_cache"],
                Children = (IList<IHolon>)JsonSerializer.Deserialize(keyValuePair["children"], typeof(IList<IHolon>)),
                CreatedOASISType = new Utilities.EnumValue<OASISType>((OASISType)Enum.Parse(typeof(OASISType), keyValuePair["created_oasis_type"])),
                CreatedProviderType = new Utilities.EnumValue<ProviderType>((ProviderType)Enum.Parse(typeof(ProviderType), keyValuePair["created_provider_type"])),
                CustomKey = keyValuePair["custom_key"],
                IsActive = Convert.ToBoolean(keyValuePair["is_active"]),
                IsBeamedIn = Convert.ToBoolean(keyValuePair["is_beamed_in"]),
                JwtToken = keyValuePair["jwt_token"],
                LastBeamedIn = Convert.ToDateTime(keyValuePair["last_beamed_in"]),
                LastBeamedOut = Convert.ToDateTime(keyValuePair["last_beamed_out"]),
                MetaData = (Dictionary<string, object>)JsonSerializer.Deserialize(keyValuePair["meta_data"], typeof(Dictionary<string, object>)),
                //ParentHolon = keyValuePair["parent_holon"],
                ParentHolonId = new Guid(keyValuePair["parent_holon_id"]),
                PasswordReset = Convert.ToDateTime(keyValuePair["password_reset"]),
                PreviousVersionId = new Guid(keyValuePair["previous_version_id"]),
                PreviousVersionProviderUniqueStorageKey = (Dictionary<ProviderType, string>)JsonSerializer.Deserialize(keyValuePair["previous_version_provider_unique_storage_key"], typeof(Dictionary<ProviderType, string>)),
                ProviderMetaData = (Dictionary<ProviderType, Dictionary<string, string>>)JsonSerializer.Deserialize(keyValuePair["provider_meta_data"], typeof(Dictionary<ProviderType, Dictionary<string, string>>)),
                ProviderUniqueStorageKey = (Dictionary<ProviderType, string>)JsonSerializer.Deserialize(keyValuePair["provider_unique_storage_key"], typeof(Dictionary<ProviderType, string>)),
                ProviderUsername = (Dictionary<ProviderType, string>)JsonSerializer.Deserialize(keyValuePair["provider_user_name"], typeof(Dictionary<ProviderType, string>)),
                ProviderWallets = (Dictionary<ProviderType, List<IProviderWallet>>)JsonSerializer.Deserialize(keyValuePair["provider_wallets"], typeof(Dictionary<ProviderType, List<IProviderWallet>>)),
                RefreshToken = keyValuePair["refresh_token"],
                RefreshTokens = (List<RefreshToken>)JsonSerializer.Deserialize(keyValuePair["refresh_tokens"], typeof(List<RefreshToken>)),
                ResetToken = keyValuePair["reset_token"],
                ResetTokenExpires = Convert.ToDateTime(keyValuePair["reset_token_expires"]),
                VerificationToken = keyValuePair["verification_token"],
                Verified = Convert.ToDateTime(keyValuePair["verified"]),
                Version = Convert.ToInt32(keyValuePair["version"]),
                VersionId = new Guid(keyValuePair["version_id"])
            };

            avatar.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = keyValuePair["entry_hash"]; //TODO: Might not need this if it has already been set?
            return avatar;
        }

        public static dynamic ConvertAvatarToParamsObject(IAvatar avatar)
        {
            return new
            {
                id = avatar.Id.ToString(),
                username = avatar.Username,
                password = avatar.Password,
                email = avatar.Email,
                name = avatar.Name,
                description = avatar.Description,
                title = avatar.Title,
                first_name = avatar.FirstName,
                last_name = avatar.LastName,
                created_by = avatar.CreatedByAvatarId.ToString(),
                created_date = avatar.CreatedDate.ToString(),
                modified_by = avatar.ModifiedByAvatarId.ToString(),
                modified_date = avatar.ModifiedDate.ToString(),
                deleted_by = avatar.DeletedByAvatarId.ToString(),
                deleted_date = avatar.DeletedDate,
                holon_type = avatar.HolonType,
                accept_terms = avatar.AcceptTerms,
                all_child_id_list_cache = avatar.AllChildIdListCache,
                avatar_type = avatar.AvatarType,
                child_id_list_cache = avatar.ChildIdListCache,
                children = avatar.Children, //JsonSerializer.Serialize(avatar.Children), //TODO: We may need to serialize a few of these props if they are not supported...
                created_oasis_type = avatar.CreatedOASISType,
                created_provider_type = avatar.CreatedProviderType,
                //custom_key = avatar.CustomKey,
                is_active = avatar.IsActive,
                is_beamed_in = avatar.IsBeamedIn,
                jwt_token = avatar.JwtToken,
                last_beamed_in = avatar.LastBeamedIn,
                last_beamed_out = avatar.LastBeamedOut,
                meta_data = avatar.MetaData,
                //ParentHolon = avatar.ParentHolon,
                parent_holon_id = avatar.ParentHolonId,
                password_reset = avatar.PasswordReset,
                previous_version_id = avatar.PreviousVersionId,
                previous_version_provider_unique_storage_key = avatar.PreviousVersionProviderUniqueStorageKey,
                provider_meta_data = avatar.ProviderMetaData,
                provider_unique_storage_key = avatar.ProviderUniqueStorageKey,
                provider_username = avatar.ProviderUsername,
                provider_wallets = avatar.ProviderWallets,
                refresh_token = avatar.RefreshToken,
                refresh_tokens = avatar.RefreshTokens,
                reset_token = avatar.ResetToken,
                reset_token_expires = avatar.ResetTokenExpires,
                verification_token = avatar.VerificationToken,
                verified = avatar.Verified,
                version = avatar.Version,
                version_id = avatar.VersionId
            };
        }

        public static IHcAvatarDetail ConvertAvatarDetailToHoloOASISAvatarDetail(IAvatarDetail avatarDetail, IHcAvatarDetail hcAvatarDetail)
        {
            hcAvatarDetail.Id = avatarDetail.Id;
            hcAvatarDetail.Username = avatarDetail.Username;
            hcAvatarDetail.Email = avatarDetail.Email;
            hcAvatarDetail.Achievements = avatarDetail.Achievements;
            hcAvatarDetail.Address = avatarDetail.Address;
            hcAvatarDetail.AllChildIdListCache = avatarDetail.AllChildIdListCache;
            //hcAvatarDetail.AllChildren = avatarDetail.AllChildren;
            hcAvatarDetail.Attributes = avatarDetail.Attributes;
            hcAvatarDetail.Aura = avatarDetail.Aura;
            hcAvatarDetail.Chakras = avatarDetail.Chakras;
            hcAvatarDetail.ChildIdListCache = avatarDetail.ChildIdListCache;
            hcAvatarDetail.Children = avatarDetail.Children;
            hcAvatarDetail.Country = avatarDetail.Country;
            hcAvatarDetail.County = avatarDetail.County;
            hcAvatarDetail.CreatedBy = avatarDetail.CreatedByAvatarId.ToString();
            hcAvatarDetail.CreatedDate = avatarDetail.CreatedDate;
            hcAvatarDetail.CreatedOASISType = avatarDetail.CreatedOASISType;
            hcAvatarDetail.CreatedProviderType = avatarDetail.CreatedProviderType;
            //hcAvatarDetail.CustomKey = avatarDetail.CustomKey;
            hcAvatarDetail.DeletedBy = avatarDetail.DeletedByAvatarId.ToString();
            hcAvatarDetail.DeletedDate = avatarDetail.DeletedDate;
            hcAvatarDetail.Description = avatarDetail.Description;
            hcAvatarDetail.DimensionLevelIds = avatarDetail.DimensionLevelIds;
            hcAvatarDetail.DimensionLevels = avatarDetail.DimensionLevels;
            hcAvatarDetail.DOB = avatarDetail.DOB.ToString();
            hcAvatarDetail.FavouriteColour = avatarDetail.FavouriteColour;
            hcAvatarDetail.GeneKeys = avatarDetail.GeneKeys;
            hcAvatarDetail.Gifts = avatarDetail.Gifts;
            hcAvatarDetail.HeartRateData = avatarDetail.HeartRateData;
            hcAvatarDetail.HolonType = avatarDetail.HolonType;
            hcAvatarDetail.HumanDesign = avatarDetail.HumanDesign;
            hcAvatarDetail.InstanceSavedOnProviderType = avatarDetail.InstanceSavedOnProviderType;
            hcAvatarDetail.Inventory = avatarDetail.Inventory;
            hcAvatarDetail.IsActive = avatarDetail.IsActive;
            hcAvatarDetail.Karma = avatarDetail.Karma;
            hcAvatarDetail.KarmaAkashicRecords = avatarDetail.KarmaAkashicRecords;
            hcAvatarDetail.Landline = avatarDetail.Landline;
            //hcAvatarDetail.Level = avatarDetail.Level;
            hcAvatarDetail.MetaData = avatarDetail.MetaData;
            hcAvatarDetail.Mobile = avatarDetail.Mobile;
            hcAvatarDetail.Model3D = avatarDetail.Model3D;
            hcAvatarDetail.ModifiedBy = avatarDetail.ModifiedByAvatarId.ToString();
            hcAvatarDetail.ModifiedDate = avatarDetail.ModifiedDate;
            hcAvatarDetail.Name = avatarDetail.Name;
            hcAvatarDetail.Omniverse = avatarDetail.Omniverse;
            //hcAvatarDetail.Original = avatarDetail.Original;
            hcAvatarDetail.ParentHolonId = avatarDetail.ParentHolonId;
            hcAvatarDetail.Portrait = avatarDetail.Portrait;
            hcAvatarDetail.Postcode = avatarDetail.Postcode;
            hcAvatarDetail.PreviousVersionId = avatarDetail.PreviousVersionId;
            hcAvatarDetail.PreviousVersionProviderUniqueStorageKey = avatarDetail.PreviousVersionProviderUniqueStorageKey;
            hcAvatarDetail.ProviderMetaData = avatarDetail.ProviderMetaData;
            hcAvatarDetail.ProviderUniqueStorageKey = avatarDetail.ProviderUniqueStorageKey;
            //hcAvatarDetail.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = hcAvatarDetail.EntryHash; //TODO: Not sure if needed?
            hcAvatarDetail.Skills = avatarDetail.Skills;
            hcAvatarDetail.Spells = avatarDetail.Spells;
            hcAvatarDetail.STARCLIColour = avatarDetail.STARCLIColour;
            hcAvatarDetail.Stats = avatarDetail.Stats;
            hcAvatarDetail.SuperPowers = avatarDetail.SuperPowers;
            hcAvatarDetail.Town = avatarDetail.Town;
            hcAvatarDetail.UmaJson = avatarDetail.UmaJson;
            hcAvatarDetail.Version = avatarDetail.Version;
            hcAvatarDetail.VersionId = avatarDetail.VersionId;
            hcAvatarDetail.XP = avatarDetail.XP;

            return hcAvatarDetail;
        }

        public static IAvatarDetail ConvertHcAvatarDetailToAvatarDetail(IHcAvatarDetail hcAvatarDetail)
        {
            AvatarDetail avatarDetail = new AvatarDetail
            {
                Id = hcAvatarDetail.Id,
                Email = hcAvatarDetail.Email,
                Username = hcAvatarDetail.Username,
                Achievements = (List<IAchievement>)hcAvatarDetail.Achievements,
                Address = hcAvatarDetail.Address,
                AllChildIdListCache = hcAvatarDetail.AllChildIdListCache,
                //AllChildren = hcAvatarDetail.AllChildren,
                Attributes = hcAvatarDetail.Attributes,
                Aura = hcAvatarDetail.Aura,
                Chakras = hcAvatarDetail.Chakras,
                ChildIdListCache = hcAvatarDetail.ChildIdListCache,
                Children = hcAvatarDetail.Children,
                Country = hcAvatarDetail.Country,
                County = hcAvatarDetail.County,
                CreatedByAvatarId = new Guid(hcAvatarDetail.CreatedBy),
                CreatedDate = hcAvatarDetail.CreatedDate,
                CreatedOASISType = hcAvatarDetail.CreatedOASISType,
                CreatedProviderType = hcAvatarDetail.CreatedProviderType,
                CustomKey = hcAvatarDetail.CustomKey,
                DeletedByAvatarId = new Guid(hcAvatarDetail.DeletedBy),
                DeletedDate = hcAvatarDetail.DeletedDate,
                Description = hcAvatarDetail.Description,
                DimensionLevelIds = (Dictionary<DimensionLevel, Guid>)hcAvatarDetail.DimensionLevelIds,
                DimensionLevels = (Dictionary<DimensionLevel, IHolon>)hcAvatarDetail.DimensionLevels,
                DOB = Convert.ToDateTime(hcAvatarDetail.DOB),
                FavouriteColour = hcAvatarDetail.FavouriteColour,
                GeneKeys = hcAvatarDetail.GeneKeys,
                Gifts = hcAvatarDetail.Gifts,
                HeartRateData = hcAvatarDetail.HeartRateData,
                HolonType = hcAvatarDetail.HolonType,
                HumanDesign = hcAvatarDetail.HumanDesign,
                InstanceSavedOnProviderType = hcAvatarDetail.InstanceSavedOnProviderType,
                Inventory = hcAvatarDetail.Inventory,
                IsActive = hcAvatarDetail.IsActive,
                Karma = hcAvatarDetail.Karma,
                KarmaAkashicRecords = hcAvatarDetail.KarmaAkashicRecords,
                Landline = hcAvatarDetail.Landline,
                //Level = hcAvatarDetail.Level;
                MetaData = hcAvatarDetail.MetaData,
                Mobile = hcAvatarDetail.Mobile,
                Model3D = hcAvatarDetail.Model3D,
                ModifiedByAvatarId = new Guid(hcAvatarDetail.ModifiedBy),
                ModifiedDate = hcAvatarDetail.ModifiedDate,
                Name = hcAvatarDetail.Name,
                Omniverse = hcAvatarDetail.Omniverse,
                //Original = hcAvatarDetail.Original,
                ParentHolonId = hcAvatarDetail.ParentHolonId,
                Portrait = hcAvatarDetail.Portrait,
                Postcode = hcAvatarDetail.Postcode,
                PreviousVersionId = hcAvatarDetail.PreviousVersionId,
                PreviousVersionProviderUniqueStorageKey = hcAvatarDetail.PreviousVersionProviderUniqueStorageKey,
                ProviderMetaData = hcAvatarDetail.ProviderMetaData,
                ProviderUniqueStorageKey = hcAvatarDetail.ProviderUniqueStorageKey,
                Skills = hcAvatarDetail.Skills,
                Spells = hcAvatarDetail.Spells,
                STARCLIColour = hcAvatarDetail.STARCLIColour,
                Stats = hcAvatarDetail.Stats,
                SuperPowers = hcAvatarDetail.SuperPowers,
                Town = hcAvatarDetail.Town,
                UmaJson = hcAvatarDetail.UmaJson,
                Version = hcAvatarDetail.Version,
                VersionId = hcAvatarDetail.VersionId,
                XP = hcAvatarDetail.XP
            };

            avatarDetail.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = hcAvatarDetail.EntryHash;
            return avatarDetail;
        }

        public static IAvatarDetail ConvertKeyValuePairToAvatarDetail(Dictionary<string, string> keyValuePair)
        {
            AvatarDetail avatarDetail = new AvatarDetail
            {
                Id = new Guid(keyValuePair["id"]),
                Email = keyValuePair["email"],
                Username = keyValuePair["username"],
                HolonType = (HolonType)Enum.Parse(typeof(HolonType), keyValuePair["holon_type"]),
                Name = keyValuePair["name"],
                Description = keyValuePair["description"],
                AllChildIdListCache = keyValuePair["all_child_id_list_cache"],
                ChildIdListCache = keyValuePair["child_id_list_cache"],
                Children = (IList<IHolon>)JsonSerializer.Deserialize(keyValuePair["children"], typeof(IList<IHolon>)),
                CreatedOASISType = new Utilities.EnumValue<OASISType>((OASISType)Enum.Parse(typeof(OASISType), keyValuePair["created_oasis_type"])),
                CreatedProviderType = new Utilities.EnumValue<ProviderType>((ProviderType)Enum.Parse(typeof(ProviderType), keyValuePair["created_provider_type"])),
                CustomKey = keyValuePair["custom_key"],
                IsActive = Convert.ToBoolean(keyValuePair["is_active"]),
                CreatedByAvatarId = new Guid(keyValuePair["created_by"]),
                CreatedDate = Convert.ToDateTime(keyValuePair["created_date"]),
                ModifiedByAvatarId = new Guid(keyValuePair["modified_by"]),
                ModifiedDate = Convert.ToDateTime(keyValuePair["modified_date"]),
                DeletedByAvatarId = new Guid(keyValuePair["deleted_by"]),
                DeletedDate = Convert.ToDateTime(keyValuePair["deleted_date"]),
                MetaData = (Dictionary<string, object>)JsonSerializer.Deserialize(keyValuePair["meta_data"], typeof(Dictionary<string, object>)),
                ParentHolonId = new Guid(keyValuePair["parent_holon_id"]),
                PreviousVersionId = new Guid(keyValuePair["previous_version_id"]),
                PreviousVersionProviderUniqueStorageKey = (Dictionary<ProviderType, string>)JsonSerializer.Deserialize(keyValuePair["previous_version_provider_unique_storage_key"], typeof(Dictionary<ProviderType, string>)),
                ProviderMetaData = (Dictionary<ProviderType, Dictionary<string, string>>)JsonSerializer.Deserialize(keyValuePair["provider_meta_data"], typeof(Dictionary<ProviderType, Dictionary<string, string>>)),
                ProviderUniqueStorageKey = (Dictionary<ProviderType, string>)JsonSerializer.Deserialize(keyValuePair["provider_unique_storage_key"], typeof(Dictionary<ProviderType, string>)),
                Version = Convert.ToInt32(keyValuePair["version"]),
                VersionId = new Guid(keyValuePair["version_id"]),
                Achievements = (IList<IAchievement>)JsonSerializer.Deserialize(keyValuePair["achievements"], typeof(IList<IAchievement>)),
                Address = keyValuePair["address"],
                Attributes = (IAvatarAttributes)JsonSerializer.Deserialize(keyValuePair["attributes"], typeof(IAvatarAttributes)),
                Aura = (IAvatarAura)JsonSerializer.Deserialize(keyValuePair["aura"], typeof(IAvatarAura)),
                Chakras = (IAvatarChakras)JsonSerializer.Deserialize(keyValuePair["chakras"], typeof(IAvatarChakras)),
                Country = keyValuePair["country"],
                County = keyValuePair["county"],
                DimensionLevelIds = (Dictionary<DimensionLevel, Guid>)JsonSerializer.Deserialize(keyValuePair["dimension_level_ids"], typeof(Dictionary<DimensionLevel, Guid>)),
                DimensionLevels = (Dictionary<DimensionLevel, IHolon>)JsonSerializer.Deserialize(keyValuePair["dimension_levels"], typeof(Dictionary<DimensionLevel, IHolon>)),
                DOB = Convert.ToDateTime(keyValuePair["dob"]),
                FavouriteColour = (ConsoleColor)JsonSerializer.Deserialize(keyValuePair["favourite_colour"], typeof(ConsoleColor)),
                GeneKeys = (IList<IGeneKey>)JsonSerializer.Deserialize(keyValuePair["gene_keys"], typeof(IList<IGeneKey>)),
                Gifts = (IList<IAvatarGift>)JsonSerializer.Deserialize(keyValuePair["gifts"], typeof(IList<IAvatarGift>)),
                HeartRateData = (IList<IHeartRateEntry>)JsonSerializer.Deserialize(keyValuePair["heart_rate_data"], typeof(IList<IHeartRateEntry>)),
                HumanDesign = (IHumanDesign)JsonSerializer.Deserialize(keyValuePair["human_design"], typeof(IHumanDesign)),
                Inventory = (IList<IInventoryItem>)JsonSerializer.Deserialize(keyValuePair["inventory"], typeof(IList<IInventoryItem>)),
                Karma = Convert.ToInt64(keyValuePair["karma"]),
                KarmaAkashicRecords = (IList<IKarmaAkashicRecord>)JsonSerializer.Deserialize(keyValuePair["karma_akashic_records"], typeof(IList<IKarmaAkashicRecord>)),
                Landline = keyValuePair["land_line"],
                Mobile = keyValuePair["mobile"],
                Model3D = keyValuePair["model_3d"],
                Omniverse = (IOmiverse)JsonSerializer.Deserialize(keyValuePair["omniverse"], typeof(IOmiverse)),
                Portrait = keyValuePair["portrait"],
                Postcode = keyValuePair["postcode"],
                Skills = (IAvatarSkills)JsonSerializer.Deserialize(keyValuePair["skills"], typeof(IAvatarSkills)),
                Spells = (IList<ISpell>)JsonSerializer.Deserialize(keyValuePair["spells"], typeof(IList<ISpell>)),
                STARCLIColour = (ConsoleColor)JsonSerializer.Deserialize(keyValuePair["star_cli_colour"], typeof(ConsoleColor)),
                Stats = (IAvatarStats)JsonSerializer.Deserialize(keyValuePair["stats"], typeof(IAvatarStats)),
                SuperPowers = (IAvatarSuperPowers)JsonSerializer.Deserialize(keyValuePair["super_powers"], typeof(IAvatarSuperPowers)),
                Town = keyValuePair["town"],
                UmaJson = keyValuePair["uma_json"],
                XP = Convert.ToInt32(keyValuePair["xp"])
            };

            //avatarDetail.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = keyValuePair["entry_hash"]; //TODO: Dont think this is needed?
            return avatarDetail;
        }

        public static dynamic ConvertAvatarDetailToParamsObject(IAvatarDetail avatar)
        {
            return new
            {
                id = avatar.Id.ToString(),
                username = avatar.Username,
                email = avatar.Email,
                name = avatar.Name,
                description = avatar.Description,
                created_by = avatar.CreatedByAvatarId.ToString(),
                created_date = avatar.CreatedDate.ToString(),
                modified_by = avatar.ModifiedByAvatarId.ToString(),
                modified_date = avatar.ModifiedDate.ToString(),
                deleted_by = avatar.DeletedByAvatarId.ToString(),
                deleted_date = avatar.DeletedDate,
                holon_type = avatar.HolonType,
                all_child_id_list_cache = avatar.AllChildIdListCache,
                child_id_list_cache = avatar.ChildIdListCache,
                children = avatar.Children, //JsonSerializer.Serialize(avatar.Children), //TODO: We may need to serialize a few of these props if they are not supported...
                created_oasis_type = avatar.CreatedOASISType,
                created_provider_type = avatar.CreatedProviderType,
                //custom_key = avatar.CustomKey,
                is_active = avatar.IsActive,
                meta_data = avatar.MetaData,
                parent_holon_id = avatar.ParentHolonId,
                previous_version_id = avatar.PreviousVersionId,
                previous_version_provider_unique_storage_key = avatar.PreviousVersionProviderUniqueStorageKey,
                provider_meta_data = avatar.ProviderMetaData,
                provider_unique_storage_key = avatar.ProviderUniqueStorageKey,
                version = avatar.Version,
                version_id = avatar.VersionId,
                achievements = avatar.Achievements,
                address = avatar.Address,
                attributes = avatar.Attributes,
                aura = avatar.Aura,
                chakras = avatar.Chakras,
                country = avatar.Country,
                county = avatar.County,
                dimensionLevelIds = avatar.DimensionLevelIds,
                dimensionLevels = avatar.DimensionLevel,
                dob = avatar.DOB,
                favourite_colour = avatar.FavouriteColour,
                geneKeys = avatar.GeneKeys,
                gifts = avatar.Gifts,
                heart_rate_data = avatar.HeartRateData,
                humanDesign = avatar.HumanDesign,
                inventory = avatar.Inventory,
                karma = avatar.Karma,
                karma_akashic_records = avatar.KarmaAkashicRecords,
                landline = avatar.Landline,
                mobile = avatar.Mobile,
                model3D = avatar.Model3D,
                omniverse = avatar.Omniverse,
                portrait = avatar.Portrait,
                postcode = avatar.Postcode,
                skills = avatar.Skills,
                spells = avatar.Spells,
                star_cli_colour = avatar.STARCLIColour,
                stats = avatar.Stats,
                super_powers = avatar.SuperPowers,
                town = avatar.Town,
                uma_json = avatar.UmaJson,
                xp = avatar.XP
            };
        }

        public static IHcHolon ConvertHolonToHoloOASISHolon(IHolon holon, IHcHolon hcHolon)
        {
            hcHolon.Id = holon.Id;
            hcHolon.Name = holon.Name;
            hcHolon.Description = holon.Description;
            hcHolon.CreatedBy = holon.CreatedByAvatarId.ToString();
            hcHolon.CreatedDate = holon.CreatedDate;
            hcHolon.ModifiedBy = holon.ModifiedByAvatarId.ToString();
            hcHolon.ModifiedDate = holon.ModifiedDate;
            hcHolon.DeletedBy = holon.DeletedByAvatarId.ToString();
            hcHolon.DeletedDate = holon.DeletedDate;
            hcHolon.HolonType = holon.HolonType;
            hcHolon.AllChildIdListCache = holon.AllChildIdListCache;
            //hcHolon.AllChildren = holon.AllChildren;
            hcHolon.HolonType = holon.HolonType;
            hcHolon.ChildIdListCache = holon.ChildIdListCache;
            hcHolon.Children = holon.Children;
            hcHolon.CreatedOASISType = holon.CreatedOASISType;
            hcHolon.CreatedProviderType = holon.CreatedProviderType;
            //hcHolon.CustomKey = holon.CustomKey;
            hcHolon.IsActive = holon.IsActive;
            hcHolon.MetaData = holon.MetaData;
            hcHolon.ParentHolonId = holon.ParentHolonId;
            hcHolon.PreviousVersionId = holon.PreviousVersionId;
            hcHolon.PreviousVersionProviderUniqueStorageKey = holon.PreviousVersionProviderUniqueStorageKey;
            hcHolon.ProviderMetaData = holon.ProviderMetaData;
            hcHolon.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey;
            hcHolon.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = hcHolon.EntryHash;
            hcHolon.Version = holon.Version;
            hcHolon.VersionId = holon.VersionId;
            hcHolon.DimensionLevel = holon.DimensionLevel;
            hcHolon.Nodes = holon.Nodes;
            hcHolon.ParentCelestialBodyId = holon.ParentCelestialBodyId;
            hcHolon.ParentCelestialSpaceId = holon.ParentCelestialSpaceId;
            hcHolon.ParentDimensionId = holon.ParentDimensionId;
            hcHolon.ParentGalaxyClusterId = holon.ParentGalaxyClusterId;
            hcHolon.ParentGalaxyId = holon.ParentGalaxyId;
            hcHolon.ParentGrandSuperStarId = holon.ParentGrandSuperStarId;
            hcHolon.ParentGreatGrandSuperStarId = holon.ParentGreatGrandSuperStarId;
            hcHolon.ParentMoonId = holon.ParentMoonId;  
            hcHolon.ParentMultiverseId = holon.ParentMultiverseId;
            hcHolon.ParentOmniverseId = holon.ParentOmniverseId;
            hcHolon.ParentPlanetId = holon.ParentPlanetId;
            hcHolon.ParentSolarSystemId = holon.ParentSolarSystemId;
            hcHolon.ParentStarId = holon.ParentStarId;
            hcHolon.ParentSuperStarId = holon.ParentSuperStarId;
            hcHolon.ParentUniverseId = holon.ParentUniverseId;
            hcHolon.ParentZomeId = holon.ParentZomeId;
            hcHolon.SubDimensionLevel = holon.SubDimensionLevel;

            return hcHolon;
        }

        public static IHolon ConvertHcHolonToHolon(IHcHolon hcHolon)
        {
            Holon holon = new Holon
            {
                Id = hcHolon.Id,
                Description = hcHolon.Description,
                CreatedByAvatarId = new Guid(hcHolon.CreatedBy),
                CreatedDate = hcHolon.CreatedDate,
                ModifiedByAvatarId = new Guid(hcHolon.ModifiedBy),
                ModifiedDate = hcHolon.ModifiedDate,
                DeletedByAvatarId = new Guid(hcHolon.DeletedBy),
                DeletedDate = hcHolon.DeletedDate,
                HolonType = hcHolon.HolonType,
                AllChildIdListCache = hcHolon.AllChildIdListCache,
                ChildIdListCache = hcHolon.ChildIdListCache,
                Children = hcHolon.Children,
                CreatedOASISType = hcHolon.CreatedOASISType,
                CreatedProviderType = hcHolon.CreatedProviderType,
                CustomKey = hcHolon.CustomKey,
                IsActive = hcHolon.IsActive,
                MetaData = hcHolon.MetaData,
                ParentHolonId = hcHolon.ParentHolonId,
                PreviousVersionId = hcHolon.PreviousVersionId,
                PreviousVersionProviderUniqueStorageKey = hcHolon.PreviousVersionProviderUniqueStorageKey,
                ProviderMetaData = hcHolon.ProviderMetaData,
                ProviderUniqueStorageKey = hcHolon.ProviderUniqueStorageKey,
                Version = hcHolon.Version,
                VersionId = hcHolon.VersionId,
                DimensionLevel = hcHolon.DimensionLevel,
                Nodes = hcHolon.Nodes,
                ParentCelestialBodyId = hcHolon.ParentCelestialBodyId,
                ParentCelestialSpaceId = hcHolon.ParentCelestialSpaceId,
                ParentDimensionId = hcHolon.ParentDimensionId,
                ParentGalaxyClusterId = hcHolon.ParentGalaxyClusterId,
                ParentGalaxyId = hcHolon.ParentGalaxyId,
                ParentGrandSuperStarId = hcHolon.ParentGrandSuperStarId,
                ParentGreatGrandSuperStarId = hcHolon.ParentGreatGrandSuperStarId,
                ParentMoonId = hcHolon.ParentMoonId,
                ParentMultiverseId = hcHolon.ParentMultiverseId,
                ParentOmniverseId = hcHolon.ParentOmniverseId,
                ParentPlanetId = hcHolon.ParentPlanetId,
                ParentSolarSystemId = hcHolon.ParentSolarSystemId,
                ParentStarId = hcHolon.ParentStarId,
                ParentSuperStarId = hcHolon.ParentSuperStarId,
                ParentUniverseId = hcHolon.ParentUniverseId,
                ParentZomeId = hcHolon.ParentZomeId,
                SubDimensionLevel = hcHolon.SubDimensionLevel
            };

            holon.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = hcHolon.EntryHash;
            return holon;
        }

        public static IHolon ConvertKeyValuePairToHolon(Dictionary<string, string> keyValuePair)
        {
            Holon holon = new Holon
            {
                Id = new Guid(keyValuePair["id"]),
                HolonType = (HolonType)Enum.Parse(typeof(HolonType), keyValuePair["holon_type"]),
                Name = keyValuePair["name"],
                Description = keyValuePair["description"],
                AllChildIdListCache = keyValuePair["all_child_id_list_cache"],
                ChildIdListCache = keyValuePair["child_id_list_cache"],
                Children = (IList<IHolon>)JsonSerializer.Deserialize(keyValuePair["children"], typeof(IList<IHolon>)),
                CreatedOASISType = new Utilities.EnumValue<OASISType>((OASISType)Enum.Parse(typeof(OASISType), keyValuePair["created_oasis_type"])),
                CreatedProviderType = new Utilities.EnumValue<ProviderType>((ProviderType)Enum.Parse(typeof(ProviderType), keyValuePair["created_provider_type"])),
                CustomKey = keyValuePair["custom_key"],
                IsActive = Convert.ToBoolean(keyValuePair["is_active"]),
                CreatedByAvatarId = new Guid(keyValuePair["created_by"]),
                CreatedDate = Convert.ToDateTime(keyValuePair["created_date"]),
                ModifiedByAvatarId = new Guid(keyValuePair["modified_by"]),
                ModifiedDate = Convert.ToDateTime(keyValuePair["modified_date"]),
                DeletedByAvatarId = new Guid(keyValuePair["deleted_by"]),
                DeletedDate = Convert.ToDateTime(keyValuePair["deleted_date"]),
                MetaData = (Dictionary<string, object>)JsonSerializer.Deserialize(keyValuePair["meta_data"], typeof(Dictionary<string, object>)),
                ParentHolonId = new Guid(keyValuePair["parent_holon_id"]),
                PreviousVersionId = new Guid(keyValuePair["previous_version_id"]),
                PreviousVersionProviderUniqueStorageKey = (Dictionary<ProviderType, string>)JsonSerializer.Deserialize(keyValuePair["previous_version_provider_unique_storage_key"], typeof(Dictionary<ProviderType, string>)),
                ProviderMetaData = (Dictionary<ProviderType, Dictionary<string, string>>)JsonSerializer.Deserialize(keyValuePair["provider_meta_data"], typeof(Dictionary<ProviderType, Dictionary<string, string>>)),
                ProviderUniqueStorageKey = (Dictionary<ProviderType, string>)JsonSerializer.Deserialize(keyValuePair["provider_unique_storage_key"], typeof(Dictionary<ProviderType, string>)),
                Version = Convert.ToInt32(keyValuePair["version"]),
                VersionId = new Guid(keyValuePair["version_id"]),
                DimensionLevel = (DimensionLevel)JsonSerializer.Deserialize(keyValuePair["dimension_level"], typeof(DimensionLevel)),
                Nodes = (IList<INode>)JsonSerializer.Deserialize(keyValuePair["nodes"], typeof(IList<INode>)),
                ParentCelestialBodyId = new Guid(keyValuePair["parent_celestial_body_id"]),
                ParentCelestialSpaceId = new Guid(keyValuePair["parent_celestial_space_id"]),
                ParentDimensionId = new Guid(keyValuePair["parent_dimension_id"]),
                ParentGalaxyClusterId = new Guid(keyValuePair["parent_galaxy_cluster_id"]),
                ParentGalaxyId = new Guid(keyValuePair["parent_galaxy_id"]),
                ParentGrandSuperStarId = new Guid(keyValuePair["parent_grand_super_star_id"]),
                ParentGreatGrandSuperStarId = new Guid(keyValuePair["parent_great_grand_super_star_id"]),
                ParentMoonId = new Guid(keyValuePair["parent_moon_id"]),
                ParentMultiverseId = new Guid(keyValuePair["parent_multiverse_id"]),
                ParentOmniverseId = new Guid(keyValuePair["parent_omniverse_id"]),
                ParentPlanetId = new Guid(keyValuePair["parent_planet_id"]),
                ParentSolarSystemId = new Guid(keyValuePair["parent_solar_system_id"]),
                ParentStarId = new Guid(keyValuePair["parent_star_id"]),
                ParentSuperStarId = new Guid(keyValuePair["parent_super_star_id"]),
                ParentUniverseId = new Guid(keyValuePair["parent_universe_id"]),
                ParentZomeId = new Guid(keyValuePair["parent_zome_id"]),
                SubDimensionLevel = (SubDimensionLevel)JsonSerializer.Deserialize(keyValuePair["sub_dimension_level"], typeof(SubDimensionLevel))
            };

            holon.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = keyValuePair["entry_hash"];
            return holon;
        }

        public static dynamic ConvertHolonToParamsObject(IHolon holon, Dictionary<string, string> customDataKeyValuePairs = null)
        {
            return new
            {
                id = holon.Id.ToString(),
                name = holon.Name,
                description = holon.Description,
                created_by = holon.CreatedByAvatarId.ToString(),
                created_date = holon.CreatedDate.ToString(),
                modified_by = holon.ModifiedByAvatarId.ToString(),
                modified_date = holon.ModifiedDate.ToString(),
                deleted_by = holon.DeletedByAvatarId.ToString(),
                deleted_date = holon.DeletedDate,
                holon_type = holon.HolonType,
                all_child_id_list_cache = holon.AllChildIdListCache,
                child_id_list_cache = holon.ChildIdListCache,
                children = holon.Children, //JsonSerializer.Serialize(holon.Children), //TODO: We may need to serialize a few of these props if they are not supported...
                created_oasis_type = holon.CreatedOASISType,
                created_provider_type = holon.CreatedProviderType,
                //custom_key = holon.CustomKey,
                is_active = holon.IsActive,
                meta_data = holon.MetaData,
                parent_holon_id = holon.ParentHolonId,
                previous_version_id = holon.PreviousVersionId,
                previous_version_provider_unique_storage_key = holon.PreviousVersionProviderUniqueStorageKey,
                provider_meta_data = holon.ProviderMetaData,
                provider_unique_storage_key = holon.ProviderUniqueStorageKey,
                version = holon.Version,
                version_id = holon.VersionId,
                dimension_level = holon.DimensionLevel,
                nodes = holon.Nodes,
                parentCelestialBodyId = holon.ParentCelestialBodyId,
                parentCelestialSpaceId = holon.ParentCelestialSpaceId,
                parentDimensionId = holon.ParentDimensionId,
                parentGalaxyClusterId = holon.ParentGalaxyClusterId,
                parentGalaxyId = holon.ParentGalaxyId,
                parentGrandSuperStarId = holon.ParentGreatGrandSuperStarId,
                parentGreatGrandSuperStarId = holon.ParentGreatGrandSuperStarId,
                parentMoonId = holon.ParentMoonId,
                parentMultiverseId = holon.ParentMultiverseId,
                parentOmniverseId = holon.ParentOmniverseId,
                parentPlanetId = holon.ParentPlanetId,
                parentSolarSystemId = holon.ParentSolarSystemId,
                parentStarId = holon.ParentStarId,
                parentSuperStarId = holon.ParentSuperStarId,
                parentUniverseId = holon.ParentUniverseId,
                parentZomeId = holon.ParentZomeId,
                sub_dimension_level = holon.SubDimensionLevel,
                save_children = customDataKeyValuePairs["saveChildren"],
                recursive = customDataKeyValuePairs["recursive"],
                maxChildDepth = customDataKeyValuePairs["maxChildDepth"],
                continueOnError = customDataKeyValuePairs["continueOnError"],
                saveChildrenOnProvider = customDataKeyValuePairs["saveChildrenOnProvider"]
            };
        }

        public static OASISResult<T> ConvertHCResponseToOASISResult<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, IHoloNETAuditEntryBase hcObject, OASISResult<T> result) where T : IHolonBase
        {
            //We can also get the result from the EntryDataObject if we wanted.
            //if (response != null && response.Records != null && response.Records.Count > 0 && response.Records[0] != null && response.Records[0].EntryDataObject != null)
            //{
            //    switch (hcObjectType)
            //    {
            //        case HcObjectTypeEnum.Avatar:
            //            result.Result = DataHelper.ConvertHcAvatarToAvatar(response.Records[0].EntryDataObject);
            //            break;

            //        case HcObjectTypeEnum.AvatarDetail:
            //            result.Result = DataHelper.ConvertHcAvatarDetailToAvatarDetail(response.Records[0].EntryDataObject);
            //            break;

            //        case HcObjectTypeEnum.Holon:
            //            result.Result = DataHelper.ConvertHcHolonToHolon(response.Records[0].EntryDataObject);
            //            break;
            //    }
            //}

            if (UseReflection)
            {
                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        result.Result = (T)ConvertHcAvatarToAvatar((IHcAvatar)hcObject);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        result.Result = (T)ConvertHcAvatarDetailToAvatarDetail((IHcAvatarDetail)hcObject);
                        break;

                    case HcObjectTypeEnum.Holon:
                        result.Result = (T)ConvertHcHolonToHolon((IHcHolon)hcObject);
                        break;
                }
            }
            else
            {
                // Not using reflection for the HoloOASIS use case may be more efficient because it uses very slightly less code and has small performance improvement.
                // However, using relection would suit other use cases better (would use a lot less code because HoloNET would manage all the mappings (from the Holochain Conductor KeyValue pair data response) for you) such as where object mapping to external objects (like the OASIS) is not required. Please see HoloNET Test Harness for more examples of this...
                switch (hcObjectType)
                {
                    case HcObjectTypeEnum.Avatar:
                        result.Result = (T)ConvertKeyValuePairToAvatar(response.KeyValuePair);
                        break;

                    case HcObjectTypeEnum.AvatarDetail:
                        result.Result = (T)ConvertKeyValuePairToAvatarDetail(response.KeyValuePair);
                        break;

                    case HcObjectTypeEnum.Holon:
                        result.Result = (T)ConvertKeyValuePairToHolon(response.KeyValuePair);
                        break;
                }
            }

            return result;
        }
    }
}
