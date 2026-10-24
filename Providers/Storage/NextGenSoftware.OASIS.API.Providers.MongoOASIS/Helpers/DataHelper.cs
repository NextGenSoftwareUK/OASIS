using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
using Holon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers
{
    public static partial class DataHelper
    {
        private static Dictionary<ProviderType,Dictionary<string,string>> CopyProviderMetaData(Dictionary<ProviderType,Dictionary<string,string>> source)
            => source == null ? new Dictionary<ProviderType,Dictionary<string,string>>()
                : source.ToDictionary(pair => pair.Key, pair => pair.Value == null ? null : new Dictionary<string,string>(pair.Value));

        internal static InventoryItem CopyInventoryItem(IInventoryItem value)
            => NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Project<InventoryItem>(value);
        private static EnumValue<T> CopyEnum<T>(EnumValue<T> value)=>value==null?null:new EnumValue<T>(value.Value);
        private static Crystal CopyCrystal(Crystal value)=>value==null?null:new Crystal{
            Name=CopyEnum(value.Name),Type=CopyEnum(value.Type),Description=value.Description,
            ProtectionLevel=value.ProtectionLevel,EnergisingLevel=value.EnergisingLevel,GroundingLevel=value.GroundingLevel,
            CleansingLevel=value.CleansingLevel,AmplifyicationLevel=value.AmplifyicationLevel};
        private static T CopyChakra<T>(T value) where T:Chakra,new()
        {
            if(value==null)return null;
            return new T{Name=value.Name,SanskritName=value.SanskritName,Description=value.Description,
                WhatItControls=value.WhatItControls,WhenItDevelops=value.WhenItDevelops,
                YogaPose=CopyEnum(value.YogaPose),Element=CopyEnum(value.Element),Type=CopyEnum(value.Type),
                Crystal=CopyCrystal(value.Crystal),Level=value.Level,Progress=value.Progress,XP=value.XP,
                GiftsUnlocked=value.GiftsUnlocked==null?null:value.GiftsUnlocked.Select(CopyGift).ToList()};
        }
        private static AvatarChakras CopyChakras(IAvatarChakras value)=>value==null?null:new AvatarChakras{
            Crown=CopyChakra(value.Crown),Heart=CopyChakra(value.Heart),Root=CopyChakra(value.Root),
            Sacral=CopyChakra(value.Sacral),SolarPlexus=CopyChakra(value.SolarPlexus),
            ThirdEye=CopyChakra(value.ThirdEye),Throat=CopyChakra(value.Throat)};
        private static Spell CopySpell(ISpell value)=>value==null?null:new Spell{
            Name=value.Name,Description=value.Description,FireDamage=value.FireDamage,WaterDamage=value.WaterDamage,
            IceDamage=value.IceDamage,WindDamage=value.WindDamage,LightningDamage=value.LightningDamage,
            PoisonDamage=value.PoisonDamage,HealingPower=value.HealingPower};
        private static GeneKey CopyGeneKey(IGeneKey value)=>value==null?null:new GeneKey{
            Name=value.Name,Description=value.Description,Shadow=value.Shadow,Gift=value.Gift,Sidhi=value.Sidhi};
        private static AvatarGift CopyGift(IAvatarGift value)=>value==null?null:new AvatarGift{
            AvatarId=value.AvatarId,GiftEarnt=value.GiftEarnt,GiftType=value.GiftType,KarmaSource=CopyEnum(value.KarmaSource),
            KarmaSourceDesc=value.KarmaSourceDesc,KarmaSourceTitle=value.KarmaSourceTitle,Provider=CopyEnum(value.Provider),WebLink=value.WebLink};
        private static Achievement CopyAchievement(IAchievement value)=>value==null?null:new Achievement{
            AvatarId=value.AvatarId,AchievementEarnt=value.AchievementEarnt,AchievementType=value.AchievementType,
            Name=value.Name,Description=value.Description,KarmaSource=CopyEnum(value.KarmaSource),
            KarmaSourceDesc=value.KarmaSourceDesc,KarmaSourceTitle=value.KarmaSourceTitle,Provider=CopyEnum(value.Provider),WebLink=value.WebLink};
        private static KarmaAkashicRecord CopyKarmaRecord(IKarmaAkashicRecord value)=>value==null?null:new KarmaAkashicRecord{
            AvatarId=value.AvatarId,Date=value.Date,Karma=value.Karma,TotalKarma=value.TotalKarma,
            KarmaEarntOrLost=CopyEnum(value.KarmaEarntOrLost),KarmaSource=CopyEnum(value.KarmaSource),
            KarmaTypeNegative=CopyEnum(value.KarmaTypeNegative),KarmaTypePositive=CopyEnum(value.KarmaTypePositive),
            KarmaSourceDesc=value.KarmaSourceDesc,KarmaSourceTitle=value.KarmaSourceTitle,Provider=CopyEnum(value.Provider),WebLink=value.WebLink};
        private static AvatarStat CopyAvatarStat(AvatarStat value)=>value==null?null:new AvatarStat{Current=value.Current,Max=value.Max};
        private static AvatarStats CopyAvatarStats(IAvatarStats value)=>value==null?null:new AvatarStats{
            Energy=CopyAvatarStat(value.Energy),HP=CopyAvatarStat(value.HP),Mana=CopyAvatarStat(value.Mana),Stamina=CopyAvatarStat(value.Stamina)};
        private static AvatarSuperPowers CopyAvatarPowers(IAvatarSuperPowers value)=>value==null?null:new AvatarSuperPowers{
            AstralProjection=value.AstralProjection,BioLocatation=value.BioLocatation,Flight=value.Flight,
            FreezeBreath=value.FreezeBreath,HeatVision=value.HeatVision,Invulnerability=value.Invulnerability,
            RemoteViewing=value.RemoteViewing,SuperSpeed=value.SuperSpeed,SuperStrength=value.SuperStrength,
            Telekinesis=value.Telekinesis,Telepathy=value.Telepathy,Teleportation=value.Teleportation,XRayVision=value.XRayVision};
        private static AvatarAura CopyAvatarAura(IAvatarAura value)=>value==null?null:new AvatarAura{
            Progress=value.Progress,Brightness=value.Brightness,Size=value.Size,ColourBlue=value.ColourBlue,
            ColourGreen=value.ColourGreen,ColourRed=value.ColourRed,Level=value.Level,Value=value.Value};
        private static HumanDesign CopyHumanDesign(IHumanDesign value)=>value==null?null:new HumanDesign{Type=value.Type};

        private static AvatarSkills CopyAvatarSkills(IAvatarSkills value) => value == null ? null : new AvatarSkills
        {
            Computers=value.Computers,Engineering=value.Engineering,Farming=value.Farming,
            FireStarting=value.FireStarting,Fishing=value.Fishing,Languages=value.Languages,
            Meditation=value.Meditation,MeleeCombat=value.MeleeCombat,Mindfulness=value.Mindfulness,
            Negotiating=value.Negotiating,RangedCombat=value.RangedCombat,Research=value.Research,
            Science=value.Science,SpellCasting=value.SpellCasting,Translating=value.Translating,Yoga=value.Yoga
        };

        private static AvatarAttributes CopyAvatarAttributes(IAvatarAttributes value) => value == null ? null : new AvatarAttributes
        {
            Strength = value.Strength, Speed = value.Speed, Dexterity = value.Dexterity,
            Toughness = value.Toughness, Wisdom = value.Wisdom, Intelligence = value.Intelligence,
            Magic = value.Magic, Vitality = value.Vitality, Endurance = value.Endurance
        };

        public static OASISResult<IEnumerable<IAvatar>> ConvertMongoEntitysToOASISAvatars(OASISResult<IEnumerable<Avatar>> avatars, bool mapChildren = true)
            => ConvertNativeCollection<Avatar,IAvatar>(avatars,row=>ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(row),mapChildren));

        public static IEnumerable<IAvatar> ConvertMongoEntitysToOASISAvatars(IEnumerable<Avatar> avatars, bool mapChildren = true)
            => RequireConvertedCollection(ConvertNativeCollection<Avatar,IAvatar>(new OASISResult<IEnumerable<Avatar>>(avatars),row=>ConvertMongoEntityToOASISAvatar(new OASISResult<Avatar>(row),mapChildren)));

        public static OASISResult<IEnumerable<IAvatarDetail>> ConvertMongoEntitysToOASISAvatarDetails(OASISResult<IEnumerable<AvatarDetail>> avatars, bool mapChildren = true)
            => ConvertNativeCollection<AvatarDetail,IAvatarDetail>(avatars,row=>ConvertMongoEntityToOASISAvatarDetail(new OASISResult<AvatarDetail>(row),mapChildren));

        public static IEnumerable<IHolon> ConvertMongoEntitysToOASISHolons(IEnumerable<Holon> holons, bool mapChildren = true)
            => RequireConvertedCollection(ConvertNativeCollection<Holon,IHolon>(new OASISResult<IEnumerable<Holon>>(holons),row=>ConvertMongoEntityToOASISHolon(new OASISResult<Holon>(row),mapChildren)));

        public static OASISResult<IAvatar> ConvertMongoEntityToOASISAvatar(OASISResult<Avatar> avatarResult, bool mapChildren = true)
        {
            return ConvertNativeRecord<Avatar,IAvatar>(avatarResult,(native,result)=>{
            result.Result = new Core.Holons.Avatar();

            result.Result.IsNewHolon = false;
            result.Result.Id = native.HolonId;
            result.Result.ProviderUniqueStorageKey = native.ProviderUniqueStorageKey==null?new Dictionary<ProviderType,string>():new Dictionary<ProviderType,string>(native.ProviderUniqueStorageKey);

            //Just in case this has not been set yet or has been lost/corrupted somehow! ;-)
            result.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = native.Id;
            //if (result.Result.ProviderUniqueStorageKey.ContainsKey(ProviderType.MongoDBOASIS) &&)

            //result.Result.ProviderWallets = native.ProviderWallets;

            List<IProviderWallet> wallets;
            foreach (ProviderType providerType in native.ProviderWallets.Keys)
            {
                wallets = new List<IProviderWallet>();

                foreach (IProviderWallet wallet in native.ProviderWallets[providerType])
                    wallets.Add(wallet);

                result.Result.ProviderWallets[providerType] = wallets;
            }


            // result.Result.ProviderPrivateKey = native.ProviderPrivateKey;
            // result.Result.ProviderPublicKey = native.ProviderPublicKey;
            result.Result.ProviderUsername = native.ProviderUsername;
            // result.Result.ProviderWalletAddress = native.ProviderWalletAddress;
            result.Result.PreviousVersionId = native.PreviousVersionId;
            result.Result.PreviousVersionProviderUniqueStorageKey = native.PreviousVersionProviderUniqueStorageKey;
            result.Result.ProviderMetaData = native.ProviderMetaData;
            result.Result.MetaData = native.MetaData != null
                ? new Dictionary<string, object>(native.MetaData) : new Dictionary<string, object>();
            result.Result.Description = native.Description;
            result.Result.Title = native.Title;
            result.Result.ParentHolonId = native.ParentHolonId;
            result.Result.FirstName = native.FirstName;
            result.Result.LastName = native.LastName;
            result.Result.Email = native.Email;
            result.Result.Password = native.Password;
            result.Result.Username = native.Username;
            result.Result.CreatedOASISType = native.CreatedOASISType;
            //oasisAvatar.CreatedProviderType = new EnumValue<ProviderType>(native.CreatedProviderType);
            result.Result.CreatedProviderType = native.CreatedProviderType;
            result.Result.AvatarType = native.AvatarType;
            result.Result.HolonType = native.HolonType;
            // oasisAvatar.Image2D = native.Image2D;
            //oasisAvatar.UmaJson = native.UmaJson; //TODO: Not sure whether to include UmaJson or not? I think Unity guys said is it pretty large?
            //oasisAvatar.Karma = native.Karma;
            //oasisAvatar.XP = native.XP;
            result.Result.IsChanged = native.IsChanged;
            result.Result.AcceptTerms = native.AcceptTerms;
            result.Result.JwtToken = native.JwtToken;
            result.Result.PasswordReset = native.PasswordReset;
            result.Result.RefreshToken = native.RefreshToken;
            result.Result.RefreshTokens = native.RefreshTokens;
            result.Result.ResetToken = native.ResetToken;
            result.Result.ResetTokenExpires = native.ResetTokenExpires;
            result.Result.VerificationToken = native.VerificationToken;
            result.Result.Verified = native.Verified;
            result.Result.CreatedByAvatarId = ((IAuditBase)native).CreatedByAvatarId;
            result.Result.CreatedDate = native.CreatedDate;
            result.Result.DeletedByAvatarId = ((IAuditBase)native).DeletedByAvatarId;
            result.Result.DeletedDate = native.DeletedDate;
            result.Result.ModifiedByAvatarId = ((IAuditBase)native).ModifiedByAvatarId;
            result.Result.ModifiedDate = native.ModifiedDate;
            result.Result.DeletedDate = native.DeletedDate;
            result.Result.LastBeamedIn = native.LastBeamedIn;
            result.Result.LastBeamedOut = native.LastBeamedOut;
            result.Result.IsBeamedIn = native.IsBeamedIn;
            result.Result.Version = native.Version;
            result.Result.VersionId = native.VersionId;
            result.Result.IsActive = native.IsActive;
            result.Result.IsPublic = native.IsPublic;
            result.Result.CustomHolonType = native.CustomHolonType;
            //result.Result.CustomKey = native.CustomKey;

            if (mapChildren)
                result.Result.Children = native.Children;

            result.Result.AllChildIdListCache = native.AllChildIdListCache;
            result.Result.ChildIdListCache = native.ChildIdListCache;

            });
        }

        public static OASISResult<IAvatarDetail> ConvertMongoEntityToOASISAvatarDetail(OASISResult<AvatarDetail> avatar, bool mapChildren = true)
        {
            return ConvertNativeRecord<AvatarDetail,IAvatarDetail>(avatar,(native,result)=>{
            Core.Holons.AvatarDetail oasisAvatar = new Core.Holons.AvatarDetail();
            oasisAvatar.IsNewHolon = false;
            oasisAvatar.Id = native.HolonId;
            oasisAvatar.ProviderUniqueStorageKey = native.ProviderUniqueStorageKey==null?new Dictionary<ProviderType,string>():new Dictionary<ProviderType,string>(native.ProviderUniqueStorageKey);

            //Just in case this has not been set yet or has been lost/corrupted somehow! ;-)
            oasisAvatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = native.Id;

            oasisAvatar.ProviderMetaData = native.ProviderMetaData;
            oasisAvatar.PreviousVersionId = native.PreviousVersionId;
            oasisAvatar.PreviousVersionProviderUniqueStorageKey = native.PreviousVersionProviderUniqueStorageKey;
            // oasisAvatar.Title = native.Title;
            oasisAvatar.Description = native.Description;
            //oasisAvatar.FirstName = native.FirstName;
            //oasisAvatar.LastName = native.LastName;
            oasisAvatar.Email = native.Email;
            oasisAvatar.Username = native.Username;
            oasisAvatar.CreatedOASISType = native.CreatedOASISType;
            oasisAvatar.CreatedProviderType = native.CreatedProviderType;
            // oasisAvatar.AvatarType = native.AvatarType;
            oasisAvatar.HolonType = native.HolonType;
            oasisAvatar.IsChanged = native.IsChanged;
            oasisAvatar.CreatedByAvatarId = ((IAuditBase)native).CreatedByAvatarId;
            oasisAvatar.CreatedDate = native.CreatedDate;
            oasisAvatar.DeletedByAvatarId = ((IAuditBase)native).DeletedByAvatarId;
            oasisAvatar.DeletedDate = native.DeletedDate;
            oasisAvatar.ModifiedByAvatarId = ((IAuditBase)native).ModifiedByAvatarId;
            oasisAvatar.ModifiedDate = native.ModifiedDate;
            oasisAvatar.DeletedDate = native.DeletedDate;
            oasisAvatar.Version = native.Version;
            oasisAvatar.VersionId = native.VersionId;
            oasisAvatar.IsActive = native.IsActive;
            oasisAvatar.IsPublic = native.IsPublic;
            oasisAvatar.CustomHolonType = native.CustomHolonType;
            oasisAvatar.Portrait = native.Portrait;
            oasisAvatar.UmaJson = native.UmaJson;
            oasisAvatar.Model3D = native.Model3D;
            oasisAvatar.Attributes = CopyAvatarAttributes(native.Attributes);
            oasisAvatar.DimensionLevelIds = native.DimensionLevelIds == null ? null : new Dictionary<DimensionLevel, Guid>(native.DimensionLevelIds);
            oasisAvatar.HeartRateData = native.HeartRateData?.Select(entry => (IHeartRateEntry)new Core.Objects.Avatar.HeartRateEntry
                { HeartRateValue = entry.HeartRateValue, TimeStamp = entry.TimeStamp }).ToList();
            oasisAvatar.ActiveQuestId = native.ActiveQuestId;
            oasisAvatar.ActiveObjectiveId = native.ActiveObjectiveId;
            //oasisAvatar.ProviderPrivateKey = native.ProviderPrivateKey;
            //oasisAvatar.ProviderPublicKey = native.ProviderPublicKey;
            //oasisAvatar.ProviderUsername = native.ProviderUsername;
            //oasisAvatar.ProviderWalletAddress = native.ProviderWalletAddress;
            oasisAvatar.XP = native.XP;
            oasisAvatar.FavouriteColour = native.FavouriteColour;
            oasisAvatar.STARCLIColour = native.STARCLIColour;
            oasisAvatar.Skills = CopyAvatarSkills(native.Skills);
            //oasisAvatar.Spells = native.Spells;

            if (native.Spells != null)
            {
                foreach (var item in native.Spells)
                    oasisAvatar.Spells.Add(CopySpell(item));
            }

            oasisAvatar.Stats = CopyAvatarStats(native.Stats);
            oasisAvatar.SuperPowers = CopyAvatarPowers(native.SuperPowers);
            //oasisAvatar.GeneKeys = native.GeneKeys;

            if (native.GeneKeys != null)
            {
                foreach (var item in native.GeneKeys)
                    oasisAvatar.GeneKeys.Add(CopyGeneKey(item));
            }
  
            oasisAvatar.HumanDesign = CopyHumanDesign(native.HumanDesign);
            //oasisAvatar.Gifts = native.Gifts;

            if (native.Gifts != null)
            {
                foreach (var item in native.Gifts)
                    oasisAvatar.Gifts.Add(CopyGift(item));
            }

            oasisAvatar.Chakras = CopyChakras(native.Chakras);
            oasisAvatar.Aura = CopyAvatarAura(native.Aura);
            //oasisAvatar.Achievements = native.Achievements;

            if (native.Achievements != null)
            {
                foreach (var item in native.Achievements)
                    oasisAvatar.Achievements.Add(CopyAchievement(item));
            }

            if (native.Inventory != null)
            {
                foreach (var item in native.Inventory)
                {
                    oasisAvatar.Inventory.Add(CopyInventoryItem(item));
                }
            }

            oasisAvatar.Address = native.Address;
            // oasisAvatar.AvatarType = native.AvatarType;
            oasisAvatar.Country = native.Country;
            oasisAvatar.County = native.County;
            oasisAvatar.Address = native.Address;
            oasisAvatar.Country = native.Country;
            oasisAvatar.County = native.County;
            oasisAvatar.DOB = native.DOB;
            oasisAvatar.Landline = native.Landline;
            oasisAvatar.Mobile = native.Mobile;
            oasisAvatar.Postcode = native.Postcode;
            oasisAvatar.Town = native.Town;
            oasisAvatar.Karma = native.Karma;
            //oasisAvatar.KarmaAkashicRecords = native.KarmaAkashicRecords;

            if (native.KarmaAkashicRecords != null)
            {
                oasisAvatar.KarmaAkashicRecords ??= new List<IKarmaAkashicRecord>();
                foreach (var item in native.KarmaAkashicRecords)
                    oasisAvatar.KarmaAkashicRecords.Add(CopyKarmaRecord(item));
            }

            oasisAvatar.ParentHolonId = native.ParentHolonId;
            oasisAvatar.ParentHolon = native.ParentHolon;
            oasisAvatar.ParentZomeId = native.ParentZomeId;
            oasisAvatar.ParentZome = native.ParentZome;
            oasisAvatar.ParentCelestialBody = native.ParentCelestialBody;
            oasisAvatar.ParentCelestialBodyId = native.ParentCelestialBodyId;
            oasisAvatar.ParentCelestialSpace = native.ParentCelestialSpace;
            oasisAvatar.ParentCelestialSpaceId = native.ParentCelestialSpaceId;
            oasisAvatar.ParentOmniverse = native.ParentOmniverse;
            oasisAvatar.ParentOmniverseId = native.ParentOmniverseId;
            oasisAvatar.ParentDimension = native.ParentDimension;
            oasisAvatar.ParentDimensionId = native.ParentDimensionId;
            oasisAvatar.DimensionLevel = native.DimensionLevel;
            oasisAvatar.SubDimensionLevel = native.SubDimensionLevel;
            oasisAvatar.ParentMultiverse = native.ParentMultiverse;
            oasisAvatar.ParentMultiverseId = native.ParentMultiverseId;
            oasisAvatar.ParentUniverse = native.ParentUniverse;
            oasisAvatar.ParentUniverseId = native.ParentUniverseId;
            oasisAvatar.ParentGalaxyCluster = native.ParentGalaxyCluster;
            oasisAvatar.ParentGalaxyClusterId = native.ParentGalaxyClusterId;
            oasisAvatar.ParentGalaxy = native.ParentGalaxy;
            oasisAvatar.ParentGalaxyId = native.ParentGalaxyId;
            oasisAvatar.ParentSolarSystem = native.ParentSolarSystem;
            oasisAvatar.ParentSolarSystemId = native.ParentSolarSystemId;
            oasisAvatar.ParentGreatGrandSuperStar = native.ParentGreatGrandSuperStar;
            oasisAvatar.ParentGreatGrandSuperStarId = native.ParentGreatGrandSuperStarId;
            oasisAvatar.ParentGreatGrandSuperStar = native.ParentGreatGrandSuperStar;
            oasisAvatar.ParentGrandSuperStarId = native.ParentGrandSuperStarId;
            oasisAvatar.ParentGrandSuperStar = native.ParentGrandSuperStar;
            oasisAvatar.ParentSuperStarId = native.ParentSuperStarId;
            oasisAvatar.ParentSuperStar = native.ParentSuperStar;
            oasisAvatar.ParentStarId = native.ParentStarId;
            oasisAvatar.ParentStar = native.ParentStar;
            oasisAvatar.ParentPlanetId = native.ParentPlanetId;
            oasisAvatar.ParentPlanet = native.ParentPlanet;
            oasisAvatar.ParentMoonId = native.ParentMoonId;
            oasisAvatar.ParentMoon = native.ParentMoon;

            if (mapChildren)
                oasisAvatar.Children = native.Children;

            oasisAvatar.AllChildIdListCache = native.AllChildIdListCache;
            oasisAvatar.ChildIdListCache = native.ChildIdListCache;

            if (native.MetaData != null && native.MetaData.Count > 0)
                oasisAvatar.MetaData = new Dictionary<string, object>(native.MetaData);

            //oasisAvatar.Children = native.Children;
            //oasisAvatar.CustomKey = native.CustomKey;
            //oasisAvatar.Nodes = native.Nodes;

            if (native.Nodes != null)
            {
                foreach (INode node in native.Nodes)
                    oasisAvatar.Nodes.Add(node);
            }

            result.Result = oasisAvatar;
            });
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
            mongoAvatar.ParentHolonId = avatar.ParentHolonId;
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
            mongoAvatar.IsPublic = avatar.IsPublic;
            mongoAvatar.CustomHolonType = avatar.CustomHolonType;
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
            mongoAvatar.IsPublic = avatar.IsPublic;
            mongoAvatar.CustomHolonType = avatar.CustomHolonType;

            //AvatarDetail Properties
            mongoAvatar.ActiveQuestId = avatar.ActiveQuestId;
            mongoAvatar.ActiveObjectiveId = avatar.ActiveObjectiveId;
            mongoAvatar.UmaJson = avatar.UmaJson;
            mongoAvatar.Model3D = avatar.Model3D;
            mongoAvatar.Attributes = CopyAvatarAttributes(avatar.Attributes);
            mongoAvatar.DimensionLevelIds = avatar.DimensionLevelIds == null ? null : new Dictionary<DimensionLevel, Guid>(avatar.DimensionLevelIds);
            mongoAvatar.HeartRateData = avatar.HeartRateData?.Select(entry => new Core.Objects.Avatar.HeartRateEntry
                { HeartRateValue = entry.HeartRateValue, TimeStamp = entry.TimeStamp }).ToList();
            //mongoAvatar.ProviderPrivateKey = avatar.ProviderPrivateKey;
            //mongoAvatar.ProviderPublicKey = avatar.ProviderPublicKey;
            //mongoAvatar.ProviderUsername = avatar.ProviderUsername;
            //mongoAvatar.ProviderWalletAddress = avatar.ProviderWalletAddress;
            mongoAvatar.FavouriteColour = avatar.FavouriteColour;
            mongoAvatar.STARCLIColour = avatar.STARCLIColour;
            mongoAvatar.Skills = CopyAvatarSkills(avatar.Skills);
            //mongoAvatar.Spells = avatar.Spells;

            if (avatar.Spells != null)
            {
                foreach (var item in avatar.Spells)
                    mongoAvatar.Spells.Add(CopySpell(item));
            }

            mongoAvatar.Stats = CopyAvatarStats(avatar.Stats);
            mongoAvatar.SuperPowers = CopyAvatarPowers(avatar.SuperPowers);
            //mongoAvatar.GeneKeys = avatar.GeneKeys;

            if (avatar.GeneKeys != null)
            {
                foreach (var item in avatar.GeneKeys)
                    mongoAvatar.GeneKeys.Add(CopyGeneKey(item));
            }

            mongoAvatar.HumanDesign = CopyHumanDesign(avatar.HumanDesign);
            //mongoAvatar.Gifts = avatar.Gifts;

            if (avatar.Gifts != null)
            {
                foreach (var item in avatar.Gifts)
                    mongoAvatar.Gifts.Add(CopyGift(item));
            }

            mongoAvatar.Chakras = CopyChakras(avatar.Chakras);
            mongoAvatar.Aura = CopyAvatarAura(avatar.Aura);
            //mongoAvatar.Achievements = avatar.Achievements;

            if (avatar.Achievements != null)
            {
                foreach (var item in avatar.Achievements)
                    mongoAvatar.Achievements.Add(CopyAchievement(item));
            }

            if (avatar.Inventory != null && avatar.Inventory.Count > 0)
            {
                foreach (var item in avatar.Inventory)
                {
                    mongoAvatar.Inventory.Add(CopyInventoryItem(item));
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
                    mongoAvatar.KarmaAkashicRecords.Add(CopyKarmaRecord(item));
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
            mongoAvatar.DimensionLevel = avatar.DimensionLevel;
            mongoAvatar.SubDimensionLevel = avatar.SubDimensionLevel;
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
            oasisHolon.ChildRoleManifestJson = holon.ChildRoleManifestJson;
            oasisHolon.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey == null ? new Dictionary<ProviderType,string>()
                : new Dictionary<ProviderType,string>(holon.ProviderUniqueStorageKey);

            //Just in case this has not been set yet or has been lost/corrupted somehow! ;-)
            oasisHolon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = holon.Id;

            oasisHolon.PreviousVersionId = holon.PreviousVersionId;
            oasisHolon.PreviousVersionProviderUniqueStorageKey = holon.PreviousVersionProviderUniqueStorageKey == null ? null
                : new Dictionary<ProviderType,string>(holon.PreviousVersionProviderUniqueStorageKey);
            oasisHolon.MetaData = holon.MetaData != null ? new Dictionary<string, object>(holon.MetaData) : new Dictionary<string, object>();
            oasisHolon.ProviderMetaData = CopyProviderMetaData(holon.ProviderMetaData);
            Core.Holons.CosmicConcurrency.SetProviderRevisionId(oasisHolon, ProviderType.MongoDBOASIS, holon.ProviderRevisionId);
            oasisHolon.Name = holon.Name;
            oasisHolon.Description = holon.Description;
            oasisHolon.HolonType = holon.HolonType;
            oasisHolon.CustomHolonType = holon.CustomHolonType;
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
            oasisHolon.DimensionLevel = holon.DimensionLevel;
            oasisHolon.SubDimensionLevel = holon.SubDimensionLevel;
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
            oasisHolon.CustomKey = holon.CustomKey;
            oasisHolon.ParentHolonId2 = holon.ParentHolonId2;
            oasisHolon.ParentHolonId3 = holon.ParentHolonId3;
            oasisHolon.ParentHolonIds = holon.ParentHolonIds == null ? null : new List<Guid>(holon.ParentHolonIds);
            oasisHolon.ChildrenIds = holon.ChildrenIds == null ? null : new List<Guid>(holon.ChildrenIds);

            if (mapChildren)
                oasisHolon.Children = holon.Children;

            oasisHolon.AllChildIdListCache = holon.AllChildIdListCache;
            oasisHolon.ChildIdListCache = holon.ChildIdListCache;

            //oasisHolon.Nodes = holon.Nodes;

            if (holon.Nodes != null)
            {
                oasisHolon.Nodes = new List<INode>();
                foreach (INode node in holon.Nodes)
                    oasisHolon.Nodes.Add(node);
            }

            oasisHolon.CreatedByAvatarId = ((IAuditBase)holon).CreatedByAvatarId;
            oasisHolon.CreatedDate = holon.CreatedDate;
            oasisHolon.DeletedByAvatarId = ((IAuditBase)holon).DeletedByAvatarId;
            oasisHolon.DeletedDate = holon.DeletedDate;
            oasisHolon.ModifiedByAvatarId = ((IAuditBase)holon).ModifiedByAvatarId;
            oasisHolon.ModifiedDate = holon.ModifiedDate;
            oasisHolon.DeletedDate = holon.DeletedDate;
            oasisHolon.Version = holon.Version;
            oasisHolon.VersionId = holon.VersionId;
            oasisHolon.PreviousVersionId = holon.PreviousVersionId;
            oasisHolon.IsActive = holon.IsActive;
            oasisHolon.IsPublic = holon.IsPublic;

            return oasisHolon;
        }

        public static OASISResult<IHolon> ConvertMongoEntityToOASISHolon(OASISResult<Holon> holon, bool mapChildren = true)
            => ConvertNativeRecord<Holon,IHolon>(holon,(native,result)=>result.Result=ConvertMongoEntityToOASISHolon(native,mapChildren));

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
            mongoHolon.ChildRoleManifestJson = (holon as Core.Holons.Holon)?.ChildRoleManifestJson;
            mongoHolon.ProviderUniqueStorageKey = holon.ProviderUniqueStorageKey == null ? new Dictionary<ProviderType,string>()
                : new Dictionary<ProviderType,string>(holon.ProviderUniqueStorageKey);
            mongoHolon.PreviousVersionId = holon.PreviousVersionId;
            mongoHolon.PreviousVersionProviderUniqueStorageKey = holon.PreviousVersionProviderUniqueStorageKey == null ? null
                : new Dictionary<ProviderType,string>(holon.PreviousVersionProviderUniqueStorageKey);
            mongoHolon.ProviderMetaData = CopyProviderMetaData(holon.ProviderMetaData);
            mongoHolon.MetaData = holon.MetaData != null ? new Dictionary<string, object>(holon.MetaData) : new Dictionary<string, object>();
            mongoHolon.CreatedOASISType = holon.CreatedOASISType;
            mongoHolon.CreatedProviderType = holon.CreatedProviderType;
            mongoHolon.HolonType = holon.HolonType;
            mongoHolon.CustomHolonType = holon.CustomHolonType;
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
            mongoHolon.DimensionLevel = holon.DimensionLevel;
            mongoHolon.SubDimensionLevel = holon.SubDimensionLevel;
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

            mongoHolon.CustomKey = (holon as Core.Holons.HolonBase)?.CustomKey;
            mongoHolon.ParentHolonId2 = holon.ParentHolonId2;
            mongoHolon.ParentHolonId3 = holon.ParentHolonId3;
            mongoHolon.ParentHolonIds = holon.ParentHolonIds == null ? null : new List<Guid>(holon.ParentHolonIds);
            mongoHolon.ChildrenIds = holon is Core.Holons.SemanticHolon semantic
                ? semantic.ChildrenIds == null ? null : new List<Guid>(semantic.ChildrenIds) : new List<Guid>();
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
            mongoHolon.IsPublic = holon.IsPublic;

            return mongoHolon;
        }
    }
}
