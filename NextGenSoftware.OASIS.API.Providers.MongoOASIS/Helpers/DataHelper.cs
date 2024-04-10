using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers
{
    public static class DataHelper
    {
        public static OASISResult<IEnumerable<IAvatar>> ConvertMongoEntitysToOASISAvatars(OASISResult<IEnumerable<Avatar>> avatars)
        {
            OASISResult<IEnumerable<IAvatar>> result = new OASISResult<IEnumerable<IAvatar>>();
            OASISResultHelper<IEnumerable<Avatar>, IEnumerable<IAvatar>>.CopyResult(avatars, result);

            if (!avatars.IsError && avatars.Result != null)
                result.Result = ConvertMongoEntitysToOASISAvatars(avatars.Result);

            return result;
        }

        public static IEnumerable<IAvatar> ConvertMongoEntitysToOASISAvatars(IEnumerable<Avatar> avatars)
        {
            List<IAvatar> oasisAvatars = new List<IAvatar>();

            foreach (Avatar avatar in avatars)
                oasisAvatars.Add(ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(avatar)).Result);

            return oasisAvatars;
        }

        //public static IEnumerable<IAvatar> ConvertMongoEntitysToOASISAvatars(List<Avatar> avatars)
        //{
        //    List<IAvatar> oasisAvatars = new List<IAvatar>();

        //    foreach (Avatar avatar in avatars)
        //        oasisAvatars.Add(ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(avatar)).Result);

        //    return oasisAvatars;
        //}

        public static OASISResult<IEnumerable<IAvatarDetail>> ConvertMongoEntitysToOASISAvatarDetails(OASISResult<IEnumerable<AvatarDetail>> avatars)
        {
            OASISResult<IEnumerable<IAvatarDetail>> result = new OASISResult<IEnumerable<IAvatarDetail>>();
            List<IAvatarDetail> oasisAvatars = new List<IAvatarDetail>();
            OASISResultHelper<IEnumerable<AvatarDetail>, IEnumerable<IAvatarDetail>>.CopyResult(avatars, result);

            if (!avatars.IsError && avatars.Result != null)
            {
                foreach (AvatarDetail avatar in avatars.Result)
                    oasisAvatars.Add(ConvertMongoEntityToOASISAvatarDetail(new OASISResult<AvatarDetail>(avatar)).Result);

                result.Result = oasisAvatars;
            }

            return result;
        }

        public static IEnumerable<IHolon> ConvertMongoEntitysToOASISHolons(IEnumerable<Holon> holons)
        {
            List<IHolon> oasisHolons = new List<IHolon>();

            foreach (Holon holon in holons)
            {
                OASISResult<IHolon> convertedResult = ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(holon));

                if (!convertedResult.IsError && convertedResult.Result != null)
                    oasisHolons.Add(convertedResult.Result);
            }

            return oasisHolons;
        }

        public static OASISResult<IAvatar> ConvertMongoEntityToOASISAvatar(OASISResult<Avatar> avatarResult)
        {
            OASISResult<IAvatar> result = new OASISResult<IAvatar>();
            OASISResultHelper<Avatar, IAvatar>.CopyResult(avatarResult, result);

            if (avatarResult.IsError || avatarResult.Result == null)
                return result;

            result.Result = new Core.Holons.Avatar();

            result.Result.IsNewHolon = false;
            result.Result.Id = avatarResult.Result.HolonId;
            result.Result.ProviderUniqueStorageKey = avatarResult.Result.ProviderUniqueStorageKey;
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
            result.Result.CustomKey = avatarResult.Result.CustomKey;

            return result;
        }

        public static OASISResult<IAvatarDetail> ConvertMongoEntityToOASISAvatarDetail(OASISResult<AvatarDetail> avatar)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            OASISResultHelper<AvatarDetail, IAvatarDetail>.CopyResult(avatar, result);

            if (avatar.IsError || avatar.Result == null)
                return result;

            Core.Holons.AvatarDetail oasisAvatar = new Core.Holons.AvatarDetail();
            oasisAvatar.IsNewHolon = false;
            oasisAvatar.Id = avatar.Result.HolonId;
            oasisAvatar.ProviderUniqueStorageKey = avatar.Result.ProviderUniqueStorageKey;
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
            oasisAvatar.Spells = avatar.Result.Spells;
            oasisAvatar.Stats = avatar.Result.Stats;
            oasisAvatar.SuperPowers = avatar.Result.SuperPowers;
            oasisAvatar.GeneKeys = avatar.Result.GeneKeys;
            oasisAvatar.HumanDesign = avatar.Result.HumanDesign;
            oasisAvatar.Gifts = avatar.Result.Gifts;
            oasisAvatar.Chakras = avatar.Result.Chakras;
            oasisAvatar.Aura = avatar.Result.Aura;
            oasisAvatar.Achievements = avatar.Result.Achievements;
            oasisAvatar.Inventory = avatar.Result.Inventory;
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
            oasisAvatar.KarmaAkashicRecords = avatar.Result.KarmaAkashicRecords;
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
            oasisAvatar.Children = avatar.Result.Children;
            oasisAvatar.CustomKey = avatar.Result.CustomKey;
            //oasisAvatar.Nodes = avatar.Result.Nodes;

            if (avatar.Result.Nodes != null)
            {
                foreach (INode node in avatar.Result.Nodes)
                    oasisAvatar.Nodes.Add(node);
            }

            result.Result = oasisAvatar;
            return result;
        }

        public static Avatar ConvertOASISAvatarToMongoEntity(IAvatar avatar)
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
            mongoAvatar.PreviousVersionId = avatar.PreviousVersionId;
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
            mongoAvatar.IsActive = avatar.IsActive;
            mongoAvatar.CustomKey = avatar.CustomKey;

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

        public static AvatarDetail ConvertOASISAvatarDetailToMongoEntity(IAvatarDetail avatar)
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
            mongoAvatar.IsActive = avatar.IsActive;

            //AvatarDetail Properties
            mongoAvatar.UmaJson = avatar.UmaJson;
            //mongoAvatar.ProviderPrivateKey = avatar.ProviderPrivateKey;
            //mongoAvatar.ProviderPublicKey = avatar.ProviderPublicKey;
            //mongoAvatar.ProviderUsername = avatar.ProviderUsername;
            //mongoAvatar.ProviderWalletAddress = avatar.ProviderWalletAddress;
            mongoAvatar.FavouriteColour = avatar.FavouriteColour;
            mongoAvatar.STARCLIColour = avatar.STARCLIColour;
            mongoAvatar.Skills = avatar.Skills;
            mongoAvatar.Spells = avatar.Spells;
            mongoAvatar.Stats = avatar.Stats;
            mongoAvatar.SuperPowers = avatar.SuperPowers;
            mongoAvatar.GeneKeys = avatar.GeneKeys;
            mongoAvatar.HumanDesign = avatar.HumanDesign;
            mongoAvatar.Gifts = avatar.Gifts;
            mongoAvatar.Chakras = avatar.Chakras;
            mongoAvatar.Aura = avatar.Aura;
            mongoAvatar.Achievements = avatar.Achievements;
            mongoAvatar.Inventory = avatar.Inventory;
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
            mongoAvatar.KarmaAkashicRecords = avatar.KarmaAkashicRecords;
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
            mongoAvatar.Children = avatar.Children;
            mongoAvatar.CustomKey = avatar.CustomKey;
            // mongoAvatar.Nodes = avatar.Nodes;

            if (avatar.Nodes != null)
            {
                List<Node> nodes = new List<Node>();
                foreach (INode node in avatar.Nodes)
                    nodes.Add((Node)node);

                mongoAvatar.Nodes = nodes;
            }

            return mongoAvatar;
        }

        public static OASISResult<IHolon> ConvertMongoEntityToOASISHolon(OASISResult<Holon> holon)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            OASISResultHelper<Holon, IHolon>.CopyResult(holon, result);

            if (holon.IsError || holon.Result == null)
            {
                holon.IsError = true;

                //if (string.IsNullOrEmpty(holon.Message))
                //    holon.Message = $"The holon with id {} was not found.";

                return result;
            }

            result.Result = new Core.Holons.Holon();
            result.Result.IsNewHolon = false; //TODO: Not sure if best to default all new Holons to have this set to true or not?
            result.Result.Id = holon.Result.HolonId;
            result.Result.ProviderUniqueStorageKey = holon.Result.ProviderUniqueStorageKey;
            result.Result.PreviousVersionId = holon.Result.PreviousVersionId;
            result.Result.PreviousVersionProviderUniqueStorageKey = holon.Result.PreviousVersionProviderUniqueStorageKey;
            result.Result.MetaData = holon.Result.MetaData;
            result.Result.ProviderMetaData = holon.Result.ProviderMetaData;
            result.Result.Name = holon.Result.Name;
            result.Result.Description = holon.Result.Description;
            result.Result.HolonType = holon.Result.HolonType;
            result.Result.CreatedOASISType = holon.Result.CreatedOASISType;
            // oasisHolon.CreatedProviderType = new EnumValue<ProviderType>(holon.CreatedProviderType);
            result.Result.CreatedProviderType = holon.Result.CreatedProviderType;
            //oasisHolon.CreatedProviderType.Value = Core.Enums.ProviderType.MongoDBOASIS;
            result.Result.CreatedProviderType = holon.Result.CreatedProviderType;
            result.Result.IsChanged = holon.Result.IsChanged;
            result.Result.ParentHolonId = holon.Result.ParentHolonId;
            result.Result.ParentHolon = holon.Result.ParentHolon;
            result.Result.ParentZomeId = holon.Result.ParentZomeId;
            result.Result.ParentZome = holon.Result.ParentZome;
            result.Result.ParentCelestialBody = holon.Result.ParentCelestialBody;
            result.Result.ParentCelestialBodyId = holon.Result.ParentCelestialBodyId;
            result.Result.ParentCelestialSpace = holon.Result.ParentCelestialSpace;
            result.Result.ParentCelestialSpaceId = holon.Result.ParentCelestialSpaceId;
            result.Result.ParentOmniverse = holon.Result.ParentOmniverse;
            result.Result.ParentOmniverseId = holon.Result.ParentOmniverseId;
            result.Result.ParentDimension = holon.Result.ParentDimension;
            result.Result.ParentDimensionId = holon.Result.ParentDimensionId;
            result.Result.ParentMultiverse = holon.Result.ParentMultiverse;
            result.Result.ParentMultiverseId = holon.Result.ParentMultiverseId;
            result.Result.ParentUniverse = holon.Result.ParentUniverse;
            result.Result.ParentUniverseId = holon.Result.ParentUniverseId;
            result.Result.ParentGalaxyCluster = holon.Result.ParentGalaxyCluster;
            result.Result.ParentGalaxyClusterId = holon.Result.ParentGalaxyClusterId;
            result.Result.ParentGalaxy = holon.Result.ParentGalaxy;
            result.Result.ParentGalaxyId = holon.Result.ParentGalaxyId;
            result.Result.ParentSolarSystem = holon.Result.ParentSolarSystem;
            result.Result.ParentSolarSystemId = holon.Result.ParentSolarSystemId;
            result.Result.ParentGreatGrandSuperStar = holon.Result.ParentGreatGrandSuperStar;
            result.Result.ParentGreatGrandSuperStarId = holon.Result.ParentGreatGrandSuperStarId;
            result.Result.ParentGreatGrandSuperStar = holon.Result.ParentGreatGrandSuperStar;
            result.Result.ParentGrandSuperStarId = holon.Result.ParentGrandSuperStarId;
            result.Result.ParentGrandSuperStar = holon.Result.ParentGrandSuperStar;
            result.Result.ParentSuperStarId = holon.Result.ParentSuperStarId;
            result.Result.ParentSuperStar = holon.Result.ParentSuperStar;
            result.Result.ParentStarId = holon.Result.ParentStarId;
            result.Result.ParentStar = holon.Result.ParentStar;
            result.Result.ParentPlanetId = holon.Result.ParentPlanetId;
            result.Result.ParentPlanet = holon.Result.ParentPlanet;
            result.Result.ParentMoonId = holon.Result.ParentMoonId;
            result.Result.ParentMoon = holon.Result.ParentMoon;
            result.Result.Children = holon.Result.Children;
            result.Result.CustomKey = holon.Result.CustomKey;

            //result.Result.Nodes = holon.Result.Nodes;

            if (holon.Result.Nodes != null)
            {
                result.Result.Nodes = new System.Collections.ObjectModel.ObservableCollection<INode>();
                foreach (INode node in holon.Result.Nodes)
                    result.Result.Nodes.Add(node);
            }

            result.Result.CreatedByAvatarId = Guid.Parse(holon.Result.CreatedByAvatarId);
            result.Result.CreatedDate = holon.Result.CreatedDate;
            result.Result.DeletedByAvatarId = Guid.Parse(holon.Result.DeletedByAvatarId);
            result.Result.DeletedDate = holon.Result.DeletedDate;
            result.Result.ModifiedByAvatarId = Guid.Parse(holon.Result.ModifiedByAvatarId);
            result.Result.ModifiedDate = holon.Result.ModifiedDate;
            result.Result.DeletedDate = holon.Result.DeletedDate;
            result.Result.Version = holon.Result.Version;
            result.Result.IsActive = holon.Result.IsActive;
            return result;
        }

        public static Holon ConvertOASISHolonToMongoEntity(IHolon holon)
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
            mongoHolon.Children = holon.Children;
            mongoHolon.CustomKey = holon.CustomKey;
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
            mongoHolon.IsActive = holon.IsActive;

            return mongoHolon;
        }
    }
}
