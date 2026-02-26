using System;
using System.Collections.Generic;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers
{
    public static class DataHelper
    {
        public static OASISResult<IEnumerable<IAvatar>> ConvertMongoEntitysToOASISAvatars(OASISResult<IEnumerable<Avatar>> avatars, bool mapChildren = true)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatars, result);

            if (avatars != null && !avatars.IsError && avatars.Result != null)
                result.Result = ConvertMongoEntitysToOASISAvatars(avatars.Result, mapChildren);

            return result;
        }

        public static IEnumerable<IAvatar> ConvertMongoEntitysToOASISAvatars(IEnumerable<Avatar> avatars, bool mapChildren = true)
        {
            List<IAvatar> oasisAvatars = new List<IAvatar>();

            if (avatars != null)
            {
                foreach (Avatar avatar in avatars)
                    oasisAvatars.Add(ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(avatar), mapChildren).Result);
            }

            return oasisAvatars;
        }

        //public static IEnumerable<IAvatar> ConvertMongoEntitysToOASISAvatars(List<Avatar> avatars)
        //{
        //    List<IAvatar> oasisAvatars = new List<IAvatar>();

        //    foreach (Avatar avatar in avatars)
        //        oasisAvatars.Add(ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(avatar)).Result);

        //    return oasisAvatars;
        //}

        public static OASISResult<IEnumerable<IAvatarDetail>> ConvertMongoEntitysToOASISAvatarDetails(OASISResult<IEnumerable<AvatarDetail>> avatars, bool mapChildren = true)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            List<IAvatarDetail> oasisAvatars = new List<IAvatarDetail>();
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatars, result);

            if (avatars != null && !avatars.IsError && avatars.Result != null)
            {
                foreach (AvatarDetail avatar in avatars.Result)
                    oasisAvatars.Add(ConvertMongoEntityToOASISAvatarDetail(new OASISResult<AvatarDetail>(avatar), mapChildren).Result);

                result.Result = oasisAvatars;
            }

            return result;
        }

        public static IEnumerable<IHolon> ConvertMongoEntitysToOASISHolons(IEnumerable<Holon> holons, bool mapChildren = true)
        {
            List<IHolon> oasisHolons = new List<IHolon>();

            if (holons != null)
            {
                foreach (Holon holon in holons)
                {
                    OASISResult<IHolon> convertedResult = ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(holon), mapChildren);

                    if (!convertedResult.IsError && convertedResult.Result != null)
                        oasisHolons.Add(convertedResult.Result);
                }
            }

            return oasisHolons;
        }

        public static OASISResult<IAvatar> ConvertMongoEntityToOASISAvatar(OASISResult<Avatar> avatarResult, bool mapChildren = true)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatarResult, result);

            if (avatarResult == null || avatarResult.IsError || avatarResult.Result == null)
                return result;

            result.Result = new Core.Holons.Avatar();

            result.Result.IsNewHolon = false;
            result.Result.Id = avatarResult.Result.HolonId;
            result.Result.ProviderUniqueStorageKey = avatarResult.Result.ProviderUniqueStorageKey;

            //Just in case this has not been set yet or has been lost/corrupted somehow! ;-)
            result.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = avatarResult.Result.Id;
            //if (result.Result.ProviderUniqueStorageKey.ContainsKey(ProviderType.MongoDBOASIS) &&)

            //result.Result.ProviderWallets = avatarResult.Result.ProviderWallets;

            List<IProviderWallet> wallets;
            foreach (ProviderType providerType in avatarResult.Result.ProviderWallets.Keys)
            {
                wallets = new List<IProviderWallet>();

                foreach (IProviderWallet wallet in avatarResult.Result.ProviderWallets[providerType])
                    wallets.Add(wallet);

                result.Result.ProviderWallets[providerType] = wallets;
            }


            // result.Result.ProviderPrivateKey = avatarResult.Result.ProviderPrivateKey;
            // result.Result.ProviderPublicKey = avatarResult.Result.ProviderPublicKey;
            result.Result.ProviderUsername = avatarResult.Result.ProviderUsername;
            // result.Result.ProviderWalletAddress = avatarResult.Result.ProviderWalletAddress;
            result.Result.PreviousVersionId = avatarResult.Result.PreviousVersionId;
            result.Result.PreviousVersionProviderUniqueStorageKey = avatarResult.Result.PreviousVersionProviderUniqueStorageKey;
            result.Result.ProviderMetaData = avatarResult.Result.ProviderMetaData;
            result.Result.Description = avatarResult.Result.Description;
            result.Result.Title = avatarResult.Result.Title;
            result.Result.FirstName = avatarResult.Result.FirstName;
            result.Result.LastName = avatarResult.Result.LastName;
            result.Result.Email = avatarResult.Result.Email;
            result.Result.Password = avatarResult.Result.Password;
            result.Result.Username = avatarResult.Result.Username;
            result.Result.CreatedOASISType = avatarResult.Result.CreatedOASISType;
            //oasisAvatar.CreatedProviderType = new EnumValue<ProviderType>(avatarResult.Result.CreatedProviderType);
            result.Result.CreatedProviderType = avatarResult.Result.CreatedProviderType;
            result.Result.AvatarType = avatarResult.Result.AvatarType;
            result.Result.HolonType = avatarResult.Result.HolonType;
            // oasisAvatar.Image2D = avatarResult.Result.Image2D;
            //oasisAvatar.UmaJson = avatarResult.Result.UmaJson; //TODO: Not sure whether to include UmaJson or not? I think Unity guys said is it pretty large?
            //oasisAvatar.Karma = avatarResult.Result.Karma;
            //oasisAvatar.XP = avatarResult.Result.XP;
            result.Result.IsChanged = avatarResult.Result.IsChanged;
            result.Result.AcceptTerms = avatarResult.Result.AcceptTerms;
            result.Result.JwtToken = avatarResult.Result.JwtToken;
            result.Result.PasswordReset = avatarResult.Result.PasswordReset;
            result.Result.RefreshToken = avatarResult.Result.RefreshToken;
            result.Result.RefreshTokens = avatarResult.Result.RefreshTokens;
            result.Result.ResetToken = avatarResult.Result.ResetToken;
            result.Result.ResetTokenExpires = avatarResult.Result.ResetTokenExpires;
            result.Result.VerificationToken = avatarResult.Result.VerificationToken;
            result.Result.Verified = avatarResult.Result.Verified;
            result.Result.CreatedByAvatarId = Guid.Parse(avatarResult.Result.CreatedByAvatarId);
            result.Result.CreatedDate = avatarResult.Result.CreatedDate;
            result.Result.DeletedByAvatarId = Guid.Parse(avatarResult.Result.DeletedByAvatarId);
            result.Result.DeletedDate = avatarResult.Result.DeletedDate;
            result.Result.ModifiedByAvatarId = Guid.Parse(avatarResult.Result.ModifiedByAvatarId);
            result.Result.ModifiedDate = avatarResult.Result.ModifiedDate;
            result.Result.DeletedDate = avatarResult.Result.DeletedDate;
            result.Result.LastBeamedIn = avatarResult.Result.LastBeamedIn;
            result.Result.LastBeamedOut = avatarResult.Result.LastBeamedOut;
            result.Result.IsBeamedIn = avatarResult.Result.IsBeamedIn;
            result.Result.Version = avatarResult.Result.Version;
            result.Result.IsActive = avatarResult.Result.IsActive;
            //result.Result.CustomKey = avatarResult.Result.CustomKey;

            if (mapChildren)
                result.Result.Children = avatarResult.Result.Children;

            result.Result.AllChildIdListCache = avatarResult.Result.AllChildIdListCache;
            result.Result.ChildIdListCache = avatarResult.Result.ChildIdListCache;

            return result;
        }

        public static OASISResult<IAvatarDetail> ConvertMongoEntityToOASISAvatarDetail(OASISResult<AvatarDetail> avatar, bool mapChildren = true)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(avatar, result);

            if (avatar == null || avatar.IsError || avatar.Result == null)
                return result;

            Core.Holons.AvatarDetail oasisAvatar = new Core.Holons.AvatarDetail();
            oasisAvatar.IsNewHolon = false;
            oasisAvatar.Id = avatar.Result.HolonId;
            oasisAvatar.ProviderUniqueStorageKey = avatar.Result.ProviderUniqueStorageKey;

            //Just in case this has not been set yet or has been lost/corrupted somehow! ;-)
            oasisAvatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = avatar.Result.Id;

            oasisAvatar.ProviderMetaData = avatar.Result.ProviderMetaData;
            oasisAvatar.PreviousVersionId = avatar.Result.PreviousVersionId;
            oasisAvatar.PreviousVersionProviderUniqueStorageKey = avatar.Result.PreviousVersionProviderUniqueStorageKey;
            // oasisAvatar.Title = avatar.Result.Title;
            oasisAvatar.Description = avatar.Result.Description;
            //oasisAvatar.FirstName = avatar.Result.FirstName;
            //oasisAvatar.LastName = avatar.Result.LastName;
            oasisAvatar.Email = avatar.Result.Email;
            oasisAvatar.Username = avatar.Result.Username;
            oasisAvatar.CreatedOASISType = avatar.Result.CreatedOASISType;
            oasisAvatar.CreatedProviderType = avatar.Result.CreatedProviderType;
            // oasisAvatar.AvatarType = avatar.Result.AvatarType;
            oasisAvatar.HolonType = avatar.Result.HolonType;
            oasisAvatar.IsChanged = avatar.Result.IsChanged;
            oasisAvatar.CreatedByAvatarId = Guid.Parse(avatar.Result.CreatedByAvatarId);
            oasisAvatar.CreatedDate = avatar.Result.CreatedDate;
            oasisAvatar.DeletedByAvatarId = Guid.Parse(avatar.Result.DeletedByAvatarId);
            oasisAvatar.DeletedDate = avatar.Result.DeletedDate;
            oasisAvatar.ModifiedByAvatarId = Guid.Parse(avatar.Result.ModifiedByAvatarId);
            oasisAvatar.ModifiedDate = avatar.Result.ModifiedDate;
            oasisAvatar.DeletedDate = avatar.Result.DeletedDate;
            oasisAvatar.Version = avatar.Result.Version;
            oasisAvatar.IsActive = avatar.Result.IsActive;
            oasisAvatar.Portrait = avatar.Result.Portrait;
            oasisAvatar.UmaJson = avatar.Result.UmaJson;
            //oasisAvatar.ProviderPrivateKey = avatar.Result.ProviderPrivateKey;
            //oasisAvatar.ProviderPublicKey = avatar.Result.ProviderPublicKey;
            //oasisAvatar.ProviderUsername = avatar.Result.ProviderUsername;
            //oasisAvatar.ProviderWalletAddress = avatar.Result.ProviderWalletAddress;
            oasisAvatar.XP = avatar.Result.XP;
            oasisAvatar.FavouriteColour = avatar.Result.FavouriteColour;
            oasisAvatar.STARCLIColour = avatar.Result.STARCLIColour;
            oasisAvatar.Skills = avatar.Result.Skills;
            //oasisAvatar.Spells = avatar.Result.Spells;

            if (avatar.Result.Spells != null)
            {
                foreach (var item in avatar.Result.Spells)
                    oasisAvatar.Spells.Add((Spell)item);
            }

            oasisAvatar.Stats = avatar.Result.Stats;
            oasisAvatar.SuperPowers = avatar.Result.SuperPowers;
            //oasisAvatar.GeneKeys = avatar.Result.GeneKeys;

            if (avatar.Result.GeneKeys != null)
            {
                foreach (var item in avatar.Result.GeneKeys)
                    oasisAvatar.GeneKeys.Add((GeneKey)item);
            }
  
            oasisAvatar.HumanDesign = avatar.Result.HumanDesign;
            //oasisAvatar.Gifts = avatar.Result.Gifts;

            if (avatar.Result.Gifts != null)
            {
                foreach (var item in avatar.Result.Gifts)
                    oasisAvatar.Gifts.Add((AvatarGift)item);
            }

            oasisAvatar.Chakras = avatar.Result.Chakras;
            oasisAvatar.Aura = avatar.Result.Aura;
            //oasisAvatar.Achievements = avatar.Result.Achievements;

            if (avatar.Result.Achievements != null)
            {
                foreach (var item in avatar.Result.Achievements)
                    oasisAvatar.Achievements.Add((Achievement)item);
            }

            //oasisAvatar.Inventory = avatar.Result.Inventory;
            // Prefer InventoryJson for reliable Quantity/Stack persistence (avoids BSON nested serialization issues)
            if (!string.IsNullOrWhiteSpace(avatar.Result.InventoryJson))
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<InventoryItem>>(avatar.Result.InventoryJson);
                    if (list != null)
                    {
                        foreach (var inv in list)
                            oasisAvatar.Inventory.Add(new InventoryItem
                            {
                                Name = inv.Name,
                                Description = inv.Description,
                                Quantity = inv.Quantity,
                                Stack = inv.Stack,
                                Id = inv.Id,
                                Image2D = inv.Image2D,
                                Image2DURI = inv.Image2DURI,
                                Object3D = inv.Object3D,
                                Object3DURI = inv.Object3DURI
                            });
                    }
                }
                catch { /* fall back to Inventory list below */ }
            }
            if (oasisAvatar.Inventory.Count == 0 && avatar.Result.Inventory != null)
            {
                foreach (var item in avatar.Result.Inventory)
                {
                    var inv = item as InventoryItem ?? new InventoryItem();
                    oasisAvatar.Inventory.Add(new InventoryItem
                    {
                        Name = inv.Name,
                        Description = inv.Description,
                        Quantity = inv.Quantity,
                        Stack = inv.Stack,
                        Id = inv.Id,
                        Image2D = inv.Image2D,
                        Image2DURI = inv.Image2DURI,
                        Object3D = inv.Object3D,
                        Object3DURI = inv.Object3DURI
                    });
                }
            }

            oasisAvatar.Address = avatar.Result.Address;
            // oasisAvatar.AvatarType = avatar.Result.AvatarType;
            oasisAvatar.Country = avatar.Result.Country;
            oasisAvatar.County = avatar.Result.County;
            oasisAvatar.Address = avatar.Result.Address;
            oasisAvatar.Country = avatar.Result.Country;
            oasisAvatar.County = avatar.Result.County;
            oasisAvatar.DOB = avatar.Result.DOB;
            oasisAvatar.Landline = avatar.Result.Landline;
            oasisAvatar.Mobile = avatar.Result.Mobile;
            oasisAvatar.Postcode = avatar.Result.Postcode;
            oasisAvatar.Town = avatar.Result.Town;
            oasisAvatar.Karma = avatar.Result.Karma;
            //oasisAvatar.KarmaAkashicRecords = avatar.Result.KarmaAkashicRecords;

            if (avatar.Result.KarmaAkashicRecords != null)
            {
                foreach (var item in avatar.Result.KarmaAkashicRecords)
                    oasisAvatar.KarmaAkashicRecords.Add((KarmaAkashicRecord)item);
            }

            oasisAvatar.ParentHolonId = avatar.Result.ParentHolonId;
            oasisAvatar.ParentHolon = avatar.Result.ParentHolon;
            oasisAvatar.ParentZomeId = avatar.Result.ParentZomeId;
            oasisAvatar.ParentZome = avatar.Result.ParentZome;
            oasisAvatar.ParentCelestialBody = avatar.Result.ParentCelestialBody;
            oasisAvatar.ParentCelestialBodyId = avatar.Result.ParentCelestialBodyId;
            oasisAvatar.ParentCelestialSpace = avatar.Result.ParentCelestialSpace;
            oasisAvatar.ParentCelestialSpaceId = avatar.Result.ParentCelestialSpaceId;
            oasisAvatar.ParentOmniverse = avatar.Result.ParentOmniverse;
            oasisAvatar.ParentOmniverseId = avatar.Result.ParentOmniverseId;
            oasisAvatar.ParentDimension = avatar.Result.ParentDimension;
            oasisAvatar.ParentDimensionId = avatar.Result.ParentDimensionId;
            oasisAvatar.ParentMultiverse = avatar.Result.ParentMultiverse;
            oasisAvatar.ParentMultiverseId = avatar.Result.ParentMultiverseId;
            oasisAvatar.ParentUniverse = avatar.Result.ParentUniverse;
            oasisAvatar.ParentUniverseId = avatar.Result.ParentUniverseId;
            oasisAvatar.ParentGalaxyCluster = avatar.Result.ParentGalaxyCluster;
            oasisAvatar.ParentGalaxyClusterId = avatar.Result.ParentGalaxyClusterId;
            oasisAvatar.ParentGalaxy = avatar.Result.ParentGalaxy;
            oasisAvatar.ParentGalaxyId = avatar.Result.ParentGalaxyId;
            oasisAvatar.ParentSolarSystem = avatar.Result.ParentSolarSystem;
            oasisAvatar.ParentSolarSystemId = avatar.Result.ParentSolarSystemId;
            oasisAvatar.ParentGreatGrandSuperStar = avatar.Result.ParentGreatGrandSuperStar;
            oasisAvatar.ParentGreatGrandSuperStarId = avatar.Result.ParentGreatGrandSuperStarId;
            oasisAvatar.ParentGreatGrandSuperStar = avatar.Result.ParentGreatGrandSuperStar;
            oasisAvatar.ParentGrandSuperStarId = avatar.Result.ParentGrandSuperStarId;
            oasisAvatar.ParentGrandSuperStar = avatar.Result.ParentGrandSuperStar;
            oasisAvatar.ParentSuperStarId = avatar.Result.ParentSuperStarId;
            oasisAvatar.ParentSuperStar = avatar.Result.ParentSuperStar;
            oasisAvatar.ParentStarId = avatar.Result.ParentStarId;
            oasisAvatar.ParentStar = avatar.Result.ParentStar;
            oasisAvatar.ParentPlanetId = avatar.Result.ParentPlanetId;
            oasisAvatar.ParentPlanet = avatar.Result.ParentPlanet;
            oasisAvatar.ParentMoonId = avatar.Result.ParentMoonId;
            oasisAvatar.ParentMoon = avatar.Result.ParentMoon;

            if (mapChildren)
                oasisAvatar.Children = avatar.Result.Children;

            oasisAvatar.AllChildIdListCache = avatar.Result.AllChildIdListCache;
            oasisAvatar.ChildIdListCache = avatar.Result.ChildIdListCache;

            //oasisAvatar.Children = avatar.Result.Children;
            //oasisAvatar.CustomKey = avatar.Result.CustomKey;
            //oasisAvatar.Nodes = avatar.Result.Nodes;

            if (avatar.Result.Nodes != null)
            {
                foreach (INode node in avatar.Result.Nodes)
                    oasisAvatar.Nodes.Add(node);
            }

            result.Result = oasisAvatar;
            return result;
        }

        public static Avatar ConvertOASISAvatarToMongoEntity(IAvatar avatar, bool mapChildren = true)
        {
            if (avatar == null)
                return null;

            Avatar mongoAvatar = new Avatar();

            if (avatar.ProviderUniqueStorageKey != null && avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                mongoAvatar.Id = avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS];

            //if (avatar.CreatedProviderType != null)
            //    mongoAvatar.CreatedProviderType = avatar.CreatedProviderType.Value;

            mongoAvatar.HolonId = avatar.Id;
            // mongoAvatar.AvatarId = avatar.Id;
            mongoAvatar.ProviderUniqueStorageKey = avatar.ProviderUniqueStorageKey;

            foreach (ProviderType providerType in avatar.ProviderWallets.Keys)
            {
                foreach (IProviderWallet wallet in avatar.ProviderWallets[providerType])
                {
                    if (!mongoAvatar.ProviderWallets.ContainsKey(providerType))
                        mongoAvatar.ProviderWallets[providerType] = new List<ProviderWallet>();

                    mongoAvatar.ProviderWallets[providerType].Add((ProviderWallet)wallet);
                }
            }

            //mongoAvatar.ProviderWallets = avatar.ProviderWallets;
            // mongoAvatar.ProviderPrivateKey = avatar.ProviderPrivateKey;
            //mongoAvatar.ProviderPublicKey = avatar.ProviderPublicKey;
            mongoAvatar.ProviderUsername = avatar.ProviderUsername;
            //mongoAvatar.ProviderWalletAddress = avatar.ProviderWalletAddress;
            mongoAvatar.ProviderMetaData = avatar.ProviderMetaData;
            mongoAvatar.PreviousVersionProviderUniqueStorageKey = avatar.PreviousVersionProviderUniqueStorageKey;
            mongoAvatar.Name = avatar.Name;
            mongoAvatar.Description = avatar.Description;
            mongoAvatar.FirstName = avatar.FirstName;
            mongoAvatar.LastName = avatar.LastName;
            mongoAvatar.Email = avatar.Email;
            mongoAvatar.Password = avatar.Password;
            mongoAvatar.Title = avatar.Title;
            mongoAvatar.Username = avatar.Username;
            mongoAvatar.HolonType = avatar.HolonType;
            mongoAvatar.AvatarType = avatar.AvatarType;
            mongoAvatar.CreatedProviderType = avatar.CreatedProviderType;
            mongoAvatar.CreatedOASISType = avatar.CreatedOASISType;
            mongoAvatar.MetaData = avatar.MetaData;
            // mongoAvatar.Image2D = avatar.Image2D;
            mongoAvatar.AcceptTerms = avatar.AcceptTerms;
            mongoAvatar.JwtToken = avatar.JwtToken;
            mongoAvatar.PasswordReset = avatar.PasswordReset;
            mongoAvatar.RefreshToken = avatar.RefreshToken;
            mongoAvatar.RefreshTokens = avatar.RefreshTokens;
            mongoAvatar.ResetToken = avatar.ResetToken;
            mongoAvatar.ResetTokenExpires = avatar.ResetTokenExpires;
            mongoAvatar.VerificationToken = avatar.VerificationToken;
            mongoAvatar.Verified = avatar.Verified;
            //mongoAvatar.Karma = avatar.Karma;
            // mongoAvatar.XP = avatar.XP;
            // mongoAvatar.Image2D = avatar.Image2D;
            mongoAvatar.IsChanged = avatar.IsChanged;
            mongoAvatar.CreatedByAvatarId = avatar.CreatedByAvatarId.ToString();
            mongoAvatar.CreatedDate = avatar.CreatedDate;
            mongoAvatar.DeletedByAvatarId = avatar.DeletedByAvatarId.ToString();
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.ModifiedByAvatarId = avatar.ModifiedByAvatarId.ToString();
            mongoAvatar.ModifiedDate = avatar.ModifiedDate;
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.LastBeamedIn = avatar.LastBeamedIn;
            mongoAvatar.LastBeamedOut = avatar.LastBeamedOut;
            mongoAvatar.IsBeamedIn = avatar.IsBeamedIn;
            mongoAvatar.Version = avatar.Version;
            mongoAvatar.PreviousVersionId = avatar.PreviousVersionId;
            mongoAvatar.VersionId = avatar.VersionId;
            mongoAvatar.IsActive = avatar.IsActive;
            //mongoAvatar.CustomKey = avatar.CustomKey;

            if (mapChildren)
                mongoAvatar.Children = avatar.Children;

            mongoAvatar.AllChildIdListCache = avatar.AllChildIdListCache;
            mongoAvatar.ChildIdListCache = avatar.ChildIdListCache;

            return mongoAvatar;
        }

        /*
        private AvatarDetail ConvertOASISAvatarToMongoEntity(IAvatarDetail avatarDetail)
        {
            if (avatarDetail == null)
                return null;

            AvatarDetail mongoAvatar = new AvatarDetail();

            if (avatarDetail.ProviderUniqueStorageKey != null && avatarDetail.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                mongoAvatar.Id = avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS];

            //if (avatar.CreatedProviderType != null)
            //    mongoAvatar.CreatedProviderType = avatar.CreatedProviderType.Value;

            mongoAvatar.HolonId = avatarDetail.Id;
            // mongoAvatar.AvatarId = avatarDetail.Id;
            mongoAvatar.ProviderUniqueStorageKey = avatarDetail.ProviderUniqueStorageKey;
            mongoAvatar.ProviderMetaData = avatarDetail.ProviderMetaData;
            mongoAvatar.PreviousVersionId = avatarDetail.PreviousVersionId;
            mongoAvatar.PreviousVersionProviderKey = avatarDetail.PreviousVersionProviderKey;
            mongoAvatar.Name = avatarDetail.Name;
            mongoAvatar.Description = avatarDetail.Description;
            mongoAvatar.FirstName = avatarDetail.FirstName;
            mongoAvatar.LastName = avatarDetail.LastName;
            mongoAvatar.Email = avatarDetail.Email;
            mongoAvatar.Title = avatarDetail.Title;
            mongoAvatar.Username = avatarDetail.Username;
            mongoAvatar.HolonType = avatarDetail.HolonType;
            mongoAvatar.AvatarType = avatarDetail.AvatarType;
            mongoAvatar.CreatedProviderType = avatarDetail.CreatedProviderType;
            mongoAvatar.CreatedOASISType = avatarDetail.CreatedOASISType;
            mongoAvatar.MetaData = avatarDetail.MetaData;
            mongoAvatar.Image2D = avatarDetail.Image2D;
            mongoAvatar.Karma = avatarDetail.Karma;
            mongoAvatar.XP = avatarDetail.XP;
            mongoAvatar.Image2D = avatarDetail.Image2D;
            mongoAvatar.IsChanged = avatarDetail.IsChanged;
            mongoAvatar.CreatedByAvatarId = avatarDetail.CreatedByAvatarId.ToString();
            mongoAvatar.CreatedDate = avatarDetail.CreatedDate;
            mongoAvatar.DeletedByAvatarId = avatarDetail.DeletedByAvatarId.ToString();
            mongoAvatar.DeletedDate = avatarDetail.DeletedDate;
            mongoAvatar.ModifiedByAvatarId = avatarDetail.ModifiedByAvatarId.ToString();
            mongoAvatar.ModifiedDate = avatarDetail.ModifiedDate;
            mongoAvatar.DeletedDate = avatarDetail.DeletedDate;
            mongoAvatar.Version = avatarDetail.Version;
            mongoAvatar.IsActive = avatarDetail.IsActive;


            return mongoAvatar;
        }*/

        public static AvatarDetail ConvertOASISAvatarDetailToMongoEntity(IAvatarDetail avatar, bool mapChildren = true)
        {
            if (avatar == null)
                return null;

            AvatarDetail mongoAvatar = new AvatarDetail();

            if (avatar.ProviderUniqueStorageKey != null && avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                mongoAvatar.Id = avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS];

            // if (avatar.CreatedProviderType != null)
            //     mongoAvatar.CreatedProviderType = avatar.CreatedProviderType.Value;

            //Avatar Properties
            mongoAvatar.HolonId = avatar.Id;
            mongoAvatar.ProviderUniqueStorageKey = avatar.ProviderUniqueStorageKey;
            mongoAvatar.ProviderMetaData = avatar.ProviderMetaData;
            mongoAvatar.PreviousVersionId = avatar.PreviousVersionId;
            mongoAvatar.PreviousVersionProviderUniqueStorageKey = avatar.PreviousVersionProviderUniqueStorageKey;
            mongoAvatar.Name = avatar.Name;
            mongoAvatar.Description = avatar.Description;
            //mongoAvatar.FirstName = avatar.FirstName;
            //mongoAvatar.LastName = avatar.LastName;
            mongoAvatar.Email = avatar.Email;
            //mongoAvatar.Title = avatar.Title;
            mongoAvatar.Username = avatar.Username;
            mongoAvatar.HolonType = avatar.HolonType;
            // mongoAvatar.AvatarType = avatar.AvatarType;
            mongoAvatar.CreatedProviderType = avatar.CreatedProviderType;
            mongoAvatar.CreatedOASISType = avatar.CreatedOASISType;
            mongoAvatar.MetaData = avatar.MetaData;
            mongoAvatar.Karma = avatar.Karma;
            mongoAvatar.XP = avatar.XP;
            mongoAvatar.Portrait = avatar.Portrait;
            mongoAvatar.IsChanged = avatar.IsChanged;
            mongoAvatar.CreatedByAvatarId = avatar.CreatedByAvatarId.ToString();
            mongoAvatar.CreatedDate = avatar.CreatedDate;
            mongoAvatar.DeletedByAvatarId = avatar.DeletedByAvatarId.ToString();
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.ModifiedByAvatarId = avatar.ModifiedByAvatarId.ToString();
            mongoAvatar.ModifiedDate = avatar.ModifiedDate;
            mongoAvatar.DeletedDate = avatar.DeletedDate;
            mongoAvatar.Version = avatar.Version;
            mongoAvatar.PreviousVersionId = avatar.PreviousVersionId;
            mongoAvatar.VersionId = avatar.VersionId;
            mongoAvatar.IsActive = avatar.IsActive;

            //AvatarDetail Properties
            mongoAvatar.UmaJson = avatar.UmaJson;
            //mongoAvatar.ProviderPrivateKey = avatar.ProviderPrivateKey;
            //mongoAvatar.ProviderPublicKey = avatar.ProviderPublicKey;
            //mongoAvatar.ProviderUsername = avatar.ProviderUsername;
            //mongoAvatar.ProviderWalletAddress = avatar.ProviderWalletAddress;
            mongoAvatar.FavouriteColour = avatar.FavouriteColour;
            mongoAvatar.STARCLIColour = avatar.STARCLIColour;
            mongoAvatar.Skills = (AvatarSkills)avatar.Skills;
            //mongoAvatar.Spells = avatar.Spells;

            if (avatar.Spells != null)
            {
                foreach (var item in avatar.Spells)
                    mongoAvatar.Spells.Add((Spell)item);
            }

            mongoAvatar.Stats = (AvatarStats)avatar.Stats;
            mongoAvatar.SuperPowers = (AvatarSuperPowers)avatar.SuperPowers;
            //mongoAvatar.GeneKeys = avatar.GeneKeys;

            if (avatar.GeneKeys != null)
            {
                foreach (var item in avatar.GeneKeys)
                    mongoAvatar.GeneKeys.Add((GeneKey)item);
            }

            mongoAvatar.HumanDesign = (HumanDesign)avatar.HumanDesign;
            //mongoAvatar.Gifts = avatar.Gifts;

            if (avatar.Gifts != null)
            {
                foreach (var item in avatar.Gifts)
                    mongoAvatar.Gifts.Add((AvatarGift)item);
            }

            mongoAvatar.Chakras = (AvatarChakras)avatar.Chakras;
            mongoAvatar.Aura = (AvatarAura)avatar.Aura;
            //mongoAvatar.Achievements = avatar.Achievements;

            if (avatar.Achievements != null)
            {
                foreach (var item in avatar.Achievements)
                    mongoAvatar.Achievements.Add((Achievement)item);
            }

            //mongoAvatar.Inventory = avatar.Inventory;
            // Persist inventory as JSON for reliable Quantity/Stack (avoids BSON nested serialization issues)
            if (avatar.Inventory != null && avatar.Inventory.Count > 0)
            {
                mongoAvatar.InventoryJson = JsonSerializer.Serialize(avatar.Inventory);
                foreach (var item in avatar.Inventory)
                {
                    var inv = item as InventoryItem ?? new InventoryItem();
                    mongoAvatar.Inventory.Add(new InventoryItem
                    {
                        Name = inv.Name,
                        Description = inv.Description,
                        Quantity = inv.Quantity,
                        Stack = inv.Stack,
                        Id = inv.Id,
                        Image2D = inv.Image2D,
                        Image2DURI = inv.Image2DURI,
                        Object3D = inv.Object3D,
                        Object3DURI = inv.Object3DURI
                    });
                }
            }

            mongoAvatar.Address = avatar.Address;
            mongoAvatar.Country = avatar.Country;
            mongoAvatar.County = avatar.County;
            mongoAvatar.Address = avatar.Address;
            mongoAvatar.Country = avatar.Country;
            mongoAvatar.County = avatar.County;
            mongoAvatar.DOB = avatar.DOB;
            mongoAvatar.Landline = avatar.Landline;
            mongoAvatar.Mobile = avatar.Mobile;
            mongoAvatar.Postcode = avatar.Postcode;
            mongoAvatar.Town = avatar.Town;
            //mongoAvatar.KarmaAkashicRecords = avatar.KarmaAkashicRecords;

            if (avatar.KarmaAkashicRecords != null)
            {
                foreach (var item in avatar.KarmaAkashicRecords)
                    mongoAvatar.KarmaAkashicRecords.Add((KarmaAkashicRecord)item);
            }

            mongoAvatar.MetaData = avatar.MetaData;
            mongoAvatar.ParentHolonId = avatar.ParentHolonId;
            mongoAvatar.ParentHolon = avatar.ParentHolon;
            mongoAvatar.ParentZomeId = avatar.ParentZomeId;
            mongoAvatar.ParentZome = avatar.ParentZome;
            mongoAvatar.ParentCelestialBody = avatar.ParentCelestialBody;
            mongoAvatar.ParentCelestialBodyId = avatar.ParentCelestialBodyId;
            mongoAvatar.ParentCelestialSpace = avatar.ParentCelestialSpace;
            mongoAvatar.ParentCelestialSpaceId = avatar.ParentCelestialSpaceId;
            mongoAvatar.ParentOmniverse = avatar.ParentOmniverse;
            mongoAvatar.ParentOmniverseId = avatar.ParentOmniverseId;
            mongoAvatar.ParentDimension = avatar.ParentDimension;
            mongoAvatar.ParentDimensionId = avatar.ParentDimensionId;
            mongoAvatar.ParentMultiverse = avatar.ParentMultiverse;
            mongoAvatar.ParentMultiverseId = avatar.ParentMultiverseId;
            mongoAvatar.ParentUniverse = avatar.ParentUniverse;
            mongoAvatar.ParentUniverseId = avatar.ParentUniverseId;
            mongoAvatar.ParentGalaxyCluster = avatar.ParentGalaxyCluster;
            mongoAvatar.ParentGalaxyClusterId = avatar.ParentGalaxyClusterId;
            mongoAvatar.ParentGalaxy = avatar.ParentGalaxy;
            mongoAvatar.ParentGalaxyId = avatar.ParentGalaxyId;
            mongoAvatar.ParentSolarSystem = avatar.ParentSolarSystem;
            mongoAvatar.ParentSolarSystemId = avatar.ParentSolarSystemId;
            mongoAvatar.ParentGreatGrandSuperStar = avatar.ParentGreatGrandSuperStar;
            mongoAvatar.ParentGreatGrandSuperStarId = avatar.ParentGreatGrandSuperStarId;
            mongoAvatar.ParentGreatGrandSuperStar = avatar.ParentGreatGrandSuperStar;
            mongoAvatar.ParentGrandSuperStarId = avatar.ParentGrandSuperStarId;
            mongoAvatar.ParentGrandSuperStar = avatar.ParentGrandSuperStar;
            mongoAvatar.ParentSuperStarId = avatar.ParentSuperStarId;
            mongoAvatar.ParentSuperStar = avatar.ParentSuperStar;
            mongoAvatar.ParentStarId = avatar.ParentStarId;
            mongoAvatar.ParentStar = avatar.ParentStar;
            mongoAvatar.ParentPlanetId = avatar.ParentPlanetId;
            mongoAvatar.ParentPlanet = avatar.ParentPlanet;
            mongoAvatar.ParentMoonId = avatar.ParentMoonId;
            mongoAvatar.ParentMoon = avatar.ParentMoon;
            //mongoAvatar.Children = avatar.Children;
            //mongoAvatar.CustomKey = avatar.CustomKey;
            // mongoAvatar.Nodes = avatar.Nodes;

            if (mapChildren)
                mongoAvatar.Children = avatar.Children;

            mongoAvatar.AllChildIdListCache = avatar.AllChildIdListCache;
            mongoAvatar.ChildIdListCache = avatar.ChildIdListCache;

            if (avatar.Nodes != null)
            {
                List<Node> nodes = new List<Node>();
                foreach (INode node in avatar.Nodes)
                    nodes.Add((Node)node);

                mongoAvatar.Nodes = nodes;
            }

            return mongoAvatar;
        }

        public static IHolon ConvertMongoEntityToOASISHolon(Holon holon, bool mapChildren = true)
        {
            Core.Holons.Holon oasisHolon = new Core.Holons.Holon();
            oasisHolon.IsNewHolon = false; //TODO: Not sure if best to default all new Holons to have this set to true or not?
            oasisHolon.Id = holon.HolonId;
            oasisHolon.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey;

            //Just in case this has not been set yet or has been lost/corrupted somehow! ;-)
            oasisHolon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = holon.Id;

            oasisHolon.PreviousVersionId = holon.PreviousVersionId;
            oasisHolon.PreviousVersionProviderUniqueStorageKey = holon.PreviousVersionProviderUniqueStorageKey;
            oasisHolon.MetaData = holon.MetaData;
            oasisHolon.ProviderMetaData = holon.ProviderMetaData;
            oasisHolon.Name = holon.Name;
            oasisHolon.Description = holon.Description;
            oasisHolon.HolonType = holon.HolonType;
            oasisHolon.CreatedOASISType = holon.CreatedOASISType;
            // oasisHolon.CreatedProviderType = new EnumValue<ProviderType>(holon.CreatedProviderType);
            oasisHolon.CreatedProviderType = holon.CreatedProviderType;
            //oasisHolon.CreatedProviderType.Value = Core.Enums.ProviderType.MongoDBOASIS;
            oasisHolon.CreatedProviderType = holon.CreatedProviderType;
            oasisHolon.IsChanged = holon.IsChanged;
            oasisHolon.ParentHolonId = holon.ParentHolonId;
            oasisHolon.ParentHolon = holon.ParentHolon;
            oasisHolon.ParentZomeId = holon.ParentZomeId;
            oasisHolon.ParentZome = holon.ParentZome;
            oasisHolon.ParentCelestialBody = holon.ParentCelestialBody;
            oasisHolon.ParentCelestialBodyId = holon.ParentCelestialBodyId;
            oasisHolon.ParentCelestialSpace = holon.ParentCelestialSpace;
            oasisHolon.ParentCelestialSpaceId = holon.ParentCelestialSpaceId;
            oasisHolon.ParentOmniverse = holon.ParentOmniverse;
            oasisHolon.ParentOmniverseId = holon.ParentOmniverseId;
            oasisHolon.ParentDimension = holon.ParentDimension;
            oasisHolon.ParentDimensionId = holon.ParentDimensionId;
            oasisHolon.ParentMultiverse = holon.ParentMultiverse;
            oasisHolon.ParentMultiverseId = holon.ParentMultiverseId;
            oasisHolon.ParentUniverse = holon.ParentUniverse;
            oasisHolon.ParentUniverseId = holon.ParentUniverseId;
            oasisHolon.ParentGalaxyCluster = holon.ParentGalaxyCluster;
            oasisHolon.ParentGalaxyClusterId = holon.ParentGalaxyClusterId;
            oasisHolon.ParentGalaxy = holon.ParentGalaxy;
            oasisHolon.ParentGalaxyId = holon.ParentGalaxyId;
            oasisHolon.ParentSolarSystem = holon.ParentSolarSystem;
            oasisHolon.ParentSolarSystemId = holon.ParentSolarSystemId;
            oasisHolon.ParentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            oasisHolon.ParentGreatGrandSuperStarId = holon.ParentGreatGrandSuperStarId;
            oasisHolon.ParentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            oasisHolon.ParentGrandSuperStarId = holon.ParentGrandSuperStarId;
            oasisHolon.ParentGrandSuperStar = holon.ParentGrandSuperStar;
            oasisHolon.ParentSuperStarId = holon.ParentSuperStarId;
            oasisHolon.ParentSuperStar = holon.ParentSuperStar;
            oasisHolon.ParentStarId = holon.ParentStarId;
            oasisHolon.ParentStar = holon.ParentStar;
            oasisHolon.ParentPlanetId = holon.ParentPlanetId;
            oasisHolon.ParentPlanet = holon.ParentPlanet;
            oasisHolon.ParentMoonId = holon.ParentMoonId;
            oasisHolon.ParentMoon = holon.ParentMoon;
            //oasisHolon.CustomKey = holon.CustomKey;

            if (mapChildren)
                oasisHolon.Children = holon.Children;

            oasisHolon.AllChildIdListCache = holon.AllChildIdListCache;
            oasisHolon.ChildIdListCache = holon.ChildIdListCache;

            //oasisHolon.Nodes = holon.Nodes;

            if (holon.Nodes != null)
            {
                oasisHolon.Nodes = new System.Collections.ObjectModel.ObservableCollection<INode>();
                foreach (INode node in holon.Nodes)
                    oasisHolon.Nodes.Add(node);
            }

            oasisHolon.CreatedByAvatarId = Guid.Parse(holon.CreatedByAvatarId);
            oasisHolon.CreatedDate = holon.CreatedDate;
            oasisHolon.DeletedByAvatarId = Guid.Parse(holon.DeletedByAvatarId);
            oasisHolon.DeletedDate = holon.DeletedDate;
            oasisHolon.ModifiedByAvatarId = Guid.Parse(holon.ModifiedByAvatarId);
            oasisHolon.ModifiedDate = holon.ModifiedDate;
            oasisHolon.DeletedDate = holon.DeletedDate;
            oasisHolon.Version = holon.Version;
            oasisHolon.VersionId = holon.VersionId;
            oasisHolon.PreviousVersionId = holon.PreviousVersionId;
            oasisHolon.IsActive = holon.IsActive;

            return oasisHolon;
        }

        public static OASISResult<IHolon> ConvertMongoEntityToOASISHolon(OASISResult<Holon> holon, bool mapChildren = true)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(holon, result);

            if (holon.IsError || holon.Result == null)
            {
                holon.IsError = true;

                //if (string.IsNullOrEmpty(holon.Message))
                //    holon.Message = $"The holon with id {} was not found.";

                return result;
            }

            result.Result = ConvertMongoEntityToOASISHolon(holon.Result, mapChildren);
            return result;
        }

        public static Holon ConvertOASISHolonToMongoEntity(IHolon holon, bool mapChildren = true)
        {
            if (holon == null)
                return null;

            Holon mongoHolon = new Holon();

            // if (holon.CreatedProviderType != null)
            //     mongoHolon.CreatedProviderType = holon.CreatedProviderType.Value;

            if (holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MongoDBOASIS))
                mongoHolon.Id = holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS];

            mongoHolon.HolonId = holon.Id;
            mongoHolon.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey;
            mongoHolon.PreviousVersionId = holon.PreviousVersionId;
            mongoHolon.PreviousVersionProviderUniqueStorageKey = holon.PreviousVersionProviderUniqueStorageKey;
            mongoHolon.ProviderMetaData = holon.ProviderMetaData;
            mongoHolon.MetaData = holon.MetaData;
            mongoHolon.CreatedOASISType = holon.CreatedOASISType;
            mongoHolon.CreatedProviderType = holon.CreatedProviderType;
            mongoHolon.HolonType = holon.HolonType;
            mongoHolon.Name = holon.Name;
            mongoHolon.Description = holon.Description;
            mongoHolon.IsChanged = holon.IsChanged;
            mongoHolon.ParentHolonId = holon.ParentHolonId;
            mongoHolon.ParentHolon = holon.ParentHolon;
            mongoHolon.ParentZomeId = holon.ParentZomeId;
            mongoHolon.ParentZome = holon.ParentZome;
            mongoHolon.ParentCelestialBody = holon.ParentCelestialBody;
            mongoHolon.ParentCelestialBodyId = holon.ParentCelestialBodyId;
            mongoHolon.ParentCelestialSpace = holon.ParentCelestialSpace;
            mongoHolon.ParentCelestialSpaceId = holon.ParentCelestialSpaceId;
            mongoHolon.ParentOmniverse = holon.ParentOmniverse;
            mongoHolon.ParentOmniverseId = holon.ParentOmniverseId;
            mongoHolon.ParentDimension = holon.ParentDimension;
            mongoHolon.ParentDimensionId = holon.ParentDimensionId;
            mongoHolon.ParentMultiverse = holon.ParentMultiverse;
            mongoHolon.ParentMultiverseId = holon.ParentMultiverseId;
            mongoHolon.ParentUniverse = holon.ParentUniverse;
            mongoHolon.ParentUniverseId = holon.ParentUniverseId;
            mongoHolon.ParentGalaxyCluster = holon.ParentGalaxyCluster;
            mongoHolon.ParentGalaxyClusterId = holon.ParentGalaxyClusterId;
            mongoHolon.ParentGalaxy = holon.ParentGalaxy;
            mongoHolon.ParentGalaxyId = holon.ParentGalaxyId;
            mongoHolon.ParentSolarSystem = holon.ParentSolarSystem;
            mongoHolon.ParentSolarSystemId = holon.ParentSolarSystemId;
            mongoHolon.ParentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            mongoHolon.ParentGreatGrandSuperStarId = holon.ParentGreatGrandSuperStarId;
            mongoHolon.ParentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            mongoHolon.ParentGrandSuperStarId = holon.ParentGrandSuperStarId;
            mongoHolon.ParentGrandSuperStar = holon.ParentGrandSuperStar;
            mongoHolon.ParentSuperStarId = holon.ParentSuperStarId;
            mongoHolon.ParentSuperStar = holon.ParentSuperStar;
            mongoHolon.ParentStarId = holon.ParentStarId;
            mongoHolon.ParentStar = holon.ParentStar;
            mongoHolon.ParentPlanetId = holon.ParentPlanetId;
            mongoHolon.ParentPlanet = holon.ParentPlanet;
            mongoHolon.ParentMoonId = holon.ParentMoonId;
            mongoHolon.ParentMoon = holon.ParentMoon;

            if (mapChildren)
                mongoHolon.Children = holon.Children;

            mongoHolon.AllChildIdListCache = holon.AllChildIdListCache;
            mongoHolon.ChildIdListCache = holon.ChildIdListCache;

            //mongoHolon.CustomKey = holon.CustomKey;
            //mongoHolon.Nodes = holon.Nodes;

            if (holon.Nodes != null)
            {
                List<Node> nodes = new List<Node>();
                foreach (INode node in holon.Nodes)
                    nodes.Add((Node)node);

                mongoHolon.Nodes = nodes;
            }

            mongoHolon.CreatedByAvatarId = holon.CreatedByAvatarId.ToString();
            mongoHolon.CreatedDate = holon.CreatedDate;
            mongoHolon.DeletedByAvatarId = holon.DeletedByAvatarId.ToString();
            mongoHolon.DeletedDate = holon.DeletedDate;
            mongoHolon.ModifiedByAvatarId = holon.ModifiedByAvatarId.ToString();
            mongoHolon.ModifiedDate = holon.ModifiedDate;
            mongoHolon.DeletedDate = holon.DeletedDate;
            mongoHolon.Version = holon.Version;
            mongoHolon.VersionId = holon.VersionId;
            mongoHolon.PreviousVersionId = holon.PreviousVersionId;
            mongoHolon.IsActive = holon.IsActive;

            return mongoHolon;
        }
    }
}
