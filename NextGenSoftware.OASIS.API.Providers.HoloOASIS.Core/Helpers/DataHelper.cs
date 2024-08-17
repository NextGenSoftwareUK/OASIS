
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.ORM.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using System;
using System.Collections.Generic;

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
            hcAvatar.FirstName = avatar.FirstName;
            hcAvatar.LastName = avatar.LastName;
            hcAvatar.ProviderUniqueStorageKey = avatar.ProviderUniqueStorageKey == null ? string.Empty : avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS];
            hcAvatar.HolonType = avatar.HolonType;

            //TODO: Finish mapping

            return hcAvatar;
        }

        public static IAvatar ConvertHcAvatarToAvatar(IHcAvatar hcAvatar)
        {
            Avatar avatar = new Avatar
            {
                Email = hcAvatar.Email,
                FirstName = hcAvatar.FirstName,
                HolonType = hcAvatar.HolonType,
                Id = hcAvatar.Id,
                LastName = hcAvatar.LastName,
                Password = hcAvatar.Password,
                Title = hcAvatar.Title,
                Username = hcAvatar.Username

                //TODO: Finish mapping
            };

            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS] = hcAvatar.ProviderUniqueStorageKey;
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
                FirstName = keyValuePair["first_name"],
                LastName = keyValuePair["last_name"],
                HolonType = (HolonType)Enum.Parse(typeof(HolonType), keyValuePair["holon_type"]),

                //TODO: Finish mapping
            };

            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.HoloOASIS] = keyValuePair["provider_unique_storage_key"];
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
                first_name = avatar.FirstName,
                last_name = avatar.LastName,
                provider_unique_storage_key = avatar.ProviderUniqueStorageKey,
                holon_type = avatar.HolonType

                //TODDO: Finish mapping rest of the properties.
            };
        }

        public static IHcAvatarDetail ConvertAvatarDetailToHoloOASISAvatarDetail(IAvatarDetail avatarDetail, IHcAvatarDetail hcAvatarDetail)
        {
            hcAvatarDetail.Id = avatarDetail.Id;
            hcAvatarDetail.Username = avatarDetail.Username;
            hcAvatarDetail.Email = avatarDetail.Email;
            //TODO: Finish implementing...

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

        public static dynamic ConvertHolonToParamsObject(IAvatarDetail avatar)
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
