using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Objects;

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
            hcAvatar.AllChildren = avatar.AllChildren;
            hcAvatar.AvatarType = avatar.AvatarType;
            hcAvatar.ChildIdListCache = avatar.ChildIdListCache;
            hcAvatar.Children = avatar.Children;
            hcAvatar.CreatedOASISType = avatar.CreatedOASISType;
            hcAvatar.CreatedProviderType = avatar.CreatedProviderType;
            hcAvatar.CustomKey = avatar.CustomKey;
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
                title = avatar.Title,
                description = avatar.Description,
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
                custom_key = avatar.CustomKey,
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
                versionId = avatar.VersionId
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
            hcAvatarDetail.AllChildren = avatarDetail.AllChildren;
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
            hcAvatarDetail.CustomKey = avatarDetail.CustomKey;
            hcAvatarDetail.DeletedBy = avatarDetail.DeletedByAvatarId.ToString();
            hcAvatarDetail.DeletedDate = avatarDetail.DeletedDate;
            hcAvatarDetail.Description = avatarDetail.Description;
            hcAvatarDetail.DimensionLevelIds = avatarDetail.DimensionLevelIds;
            hcAvatarDetail.DimensionLevels = avatarDetail.DimensionLevels;
            hcAvatarDetail.DOB = avatarDetail.DOB;
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
            hcAvatarDetail.Level = avatarDetail.Level;
            hcAvatarDetail.MetaData = avatarDetail.MetaData;
            hcAvatarDetail.Mobile = avatarDetail.Mobile;
            hcAvatarDetail.Model3D = avatarDetail.Model3D;
            hcAvatarDetail.ModifiedBy = avatarDetail.ModifiedByAvatarId.ToString();
            hcAvatarDetail.ModifiedDate = avatarDetail.ModifiedDate;
            hcAvatarDetail.Name = avatarDetail.Name;
            hcAvatarDetail.Omniverse = avatarDetail.Omniverse;
            hcAvatarDetail.Original = avatarDetail.Original;
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
                HolonType = hcAvatarDetail.HolonType,

                //TODO: Finish mapping
            };

            avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS] = hcAvatarDetail.ProviderUniqueStorageKey;
            return avatarDetail;
        }

        public IAvatarDetail ConvertKeyValuePairToAvatarDetail(Dictionary<string, string> keyValuePair)
        {
            AvatarDetail avatarDetail = new AvatarDetail
            {
                Id = new Guid(keyValuePair["id"]),
                Email = keyValuePair["email"],
                Username = keyValuePair["username"],
                HolonType = (HolonType)Enum.Parse(typeof(HolonType), keyValuePair["holon_type"])

                //TODO: Finish mapping
            };

            avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS] = keyValuePair["provider_unique_storage_key"];
            return avatarDetail;
        }

        public static dynamic ConvertAvatarDetailToParamsObject(IAvatarDetail avatar)
        {
            return new
            {
                id = avatar.Id.ToString(),
                username = avatar.Username,
                email = avatar.Email,
                provider_unique_storage_key = avatar.ProviderUniqueStorageKey,
                holon_type = avatar.HolonType

                //TODDO: Finish mapping rest of the properties.
            };
        }

        public static IHcAvatar ConvertHolonToHoloOASISHolon(IHolon holon, IHcHolon hcHolon)
        {
            hcHolon.Id = holon.Id;
            hcHolon.
            hcHolon.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey == null ? string.Empty : holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS];
            hcHolon.HolonType = holon.HolonType;

            //TODO: Finish mapping

            return hcAvatar;
        }

        public static IHolon ConvertHcHolonToHolon(IHcHolon hcHolon)
        {
            Holon holon = new Holon
            {
                Id = hcHolon.Id,
                AllChildIdListCache = hcHolon.AllChildIdListCache,
                AllChildren = hcHolon.AllChildren,
                ChildIdListCache = hcHolon.ChildIdListCache,
                Children = hcHolon.Children,
                CreatedByAvatarId = new Guid(hcHolon.CreatedBy),
                CreatedDate = hcHolon.CreatedDate,
                CreatedOASISType = hcHolon.CreatedOASISType,
                CreatedProviderType = hcHolon.CreatedProviderType,
                CustomKey = hcHolon.CustomKey,
                DeletedByAvatarId = new Guid(hcHolon.DeletedBy),
                DeletedDate = hcHolon.DeletedDate,
                Description = hcHolon.Description,
                DimensionLevel = hcHolon.DimensionLevel,
                HolonType = hcHolon.HolonType,
                InstanceSavedOnProviderType = hcHolon.InstanceSavedOnProviderType,
                IsActive = hcHolon.IsActive,
                MetaData = hcHolon.MetaData,
                ModifiedByAvatarId = new Guid(hcHolon.ModifiedBy),
                ModifiedDate = hcHolon.ModifiedDate,
                Name = hcHolon.Name,
                Nodes = hcHolon.Nodes,
                ParentCelestialBody = hcHolon.ParentCelestialBody,


                //TODO: Finish mapping
            };

            avatar.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = hcAvatar.ProviderUniqueStorageKey;
            return avatar;
        }

        public IHolon ConvertKeyValuePairToHolon(Dictionary<string, string> keyValuePair)
        {
            Holon holon = new Holon
            {
                Id = new Guid(keyValuePair["id"]),
                Email = keyValuePair["email"],
                Username = keyValuePair["username"],
                HolonType = (HolonType)Enum.Parse(typeof(HolonType), keyValuePair["holon_type"]),
                //ProviderUniqueStorageKey[ProviderType.HoloOASIS] = keyValuePair["provider_unique_storage_key"]

            //TODO: Finish mapping
        };

            holon.ProviderUniqueStorageKey[ProviderType.HoloOASIS] = keyValuePair["provider_unique_storage_key"];
            return avatarDetail;
        }

        public static dynamic ConvertHolonToParamsObject(IHolon holon, Dictionary<string, string> customDataKeyValuePairs = null)
        {
            return new
            {
                id = holon.Id.ToString(),
                provider_unique_storage_key = holon.ProviderUniqueStorageKey,
                holon_type = holon.HolonType,
                saveChildren = customDataKeyValuePairs["saveChildren"],
                recursive = customDataKeyValuePairs["recursive"],
                maxChildDepth = customDataKeyValuePairs["maxChildDepth"],
                continueOnError = customDataKeyValuePairs["continueOnError"],
                saveChildrenOnProvider = customDataKeyValuePairs["saveChildrenOnProvider"]
            //TODDO: Finish mapping rest of the properties.
            };
        }

        public static OASISResult<T> ConvertHCResponseToOASISResult<T>(ZomeFunctionCallBackEventArgs response, HcObjectTypeEnum hcObjectType, IHoloNETAuditEntryBase hcObject, OASISResult<T> result) where T : IHolonBase
        {
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
