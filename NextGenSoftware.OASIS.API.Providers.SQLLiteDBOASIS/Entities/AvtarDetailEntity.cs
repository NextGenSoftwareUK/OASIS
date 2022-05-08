using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities
{
    public class AvatarDetailEntity : HolonEntity, IAvatarDetail
    {

        public AvatarDetailEntity()
        {
            this.HolonType = HolonType.AvatarDetail;
        }

        //FORCE TO DUPLICATE THESE PROPERTIES FROM AVATAR BECAUSE MULTIPLE INHERIETANCE NOT SUPPORTED IN C#! :(
        //TODO: Be good if we can find a better work around?! ;-)

        public new Guid Id
        {
            get
            {
                return base.Id;
            }
            set
            {
                base.Id = value;
            }
        }

        public string Username { get; set; }
        public string Email { get; set; }

        /*
        public new string Name
        {
            get
            {
                return FullName;
            }
        }

        public EnumValue<AvatarType> AvatarType { get; set; }
        
      //  public string Password { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName
        {
            get
            {
                return string.Concat(Title, " ", FirstName, " ", LastName);
            }
        }
        //END DUPLICATION
        */

        public ConsoleColor FavouriteColour { get; set; }
        public ConsoleColor STARCLIColour { get; set; }
        public DateTime DOB { get; set; }
        public string Address { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string Postcode { get; set; }
        public string Mobile { get; set; }
        public string Landline { get; set; }
        public string UmaJson { get; set; }
        public string Portrait { get; set; }
        public string Model3D { get; set; }
        public List<HeartRateEntry> HeartRateData { get; set; }

        //  public EnumValue<OASISType> CreatedOASISType { get; set; }
        // public int Karma { get; private set; }
        public int Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        public int XP { get; set; }
        public IOmiverse Omniverse { get; set; } //We have all of creation inside of us... ;-)
        public List<AvatarGift> Gifts { get; set; } = new List<AvatarGift>();
        //public List<Chakra> Chakras { get; set; }
        public Dictionary<DimensionLevel, Guid> DimensionLevelIds { get; set; }
        public Dictionary<DimensionLevel, IHolon> DimensionLevels { get; set; }
        public AvatarChakras Chakras { get; set; } = new AvatarChakras();
        public AvatarAura Aura { get; set; } = new AvatarAura();
        public AvatarStats Stats { get; set; } = new AvatarStats();
        public List<GeneKey> GeneKeys { get; set; } = new List<GeneKey>();
        public HumanDesign HumanDesign { get; set; } = new HumanDesign();
        public AvatarSkills Skills { get; set; } = new AvatarSkills();
        public AvatarAttributes Attributes { get; set; } = new AvatarAttributes();
        public AvatarSuperPowers SuperPowers { get; set; } = new AvatarSuperPowers();
        public List<Spell> Spells { get; set; } = new List<Spell>();
        public List<Achievement> Achievements { get; set; } = new List<Achievement>();
        public List<InventoryItem> Inventory { get; set; } = new List<InventoryItem>();
        //public int Level
        //{
        //    get
        //    {
        //        if (this.Karma > 0 && this.Karma < 100)
        //            return 1;

        //        if (this.Karma >= 100 && this.Karma < 200)
        //            return 2;

        //        if (this.Karma >= 200 && this.Karma < 300)
        //            return 3;

        //        if (this.Karma >= 777)
        //            return 99;

        //        //TODO: Add all the other levels here, all the way up to 100 for now! ;=)

        //        return 1; //Default.
        //    }
        //}
        public int Level { get; set; }

        // A record of all the karma the user has earnt/lost along with when and where from.
        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }
        public Guid ParentOmniverseId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IOmiverse ParentOmniverse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentMultiverseId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMultiverse ParentMultiverse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentUniverseId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IUniverse ParentUniverse { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentDimensionId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IDimension ParentDimension { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DimensionLevel DimensionLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public SubDimensionLevel SubDimensionLevel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentGalaxyClusterId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGalaxyCluster ParentGalaxyCluster { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentGalaxyId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGalaxy ParentGalaxy { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentSolarSystemId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISolarSystem ParentSolarSystem { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentGreatGrandSuperStarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGreatGrandSuperStar ParentGreatGrandSuperStar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentGrandSuperStarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IGrandSuperStar ParentGrandSuperStar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentSuperStarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ISuperStar ParentSuperStar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentStarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IStar ParentStar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentPlanetId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IPlanet ParentPlanet { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentMoonId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IMoon ParentMoon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentCelestialSpaceId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ICelestialSpace ParentCelestialSpace { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentCelestialBodyId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ICelestialBody ParentCelestialBody { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentZomeId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IZome ParentZome { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ParentHolonId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IHolon ParentHolon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IEnumerable<IHolon> Children { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ObservableCollection<IHolon> ChildrenTest { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public ObservableCollection<INode> Nodes { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Description { get; set; }
        //public HolonType HolonType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<ProviderType, string> ProviderUniqueStorageKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<ProviderType, Dictionary<string, string>> ProviderMetaData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<string, string> MetaData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Version { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid VersionId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid PreviousVersionId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Dictionary<ProviderType, string> PreviousVersionProviderUniqueStorageKey { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsActive { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsChanged { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsNewHolon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public bool IsSaving { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IHolon Original { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Avatar CreatedByAvatar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid CreatedByAvatarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime CreatedDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Avatar ModifiedByAvatar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid ModifiedByAvatarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime ModifiedDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Avatar DeletedByAvatar { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public Guid DeletedByAvatarId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public DateTime DeletedDate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EnumValue<ProviderType> CreatedProviderType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EnumValue<OASISType> CreatedOASISType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public event PropertyChangedEventHandler PropertyChanged;

        public async Task<OASISResult<KarmaAkashicRecord>> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = AddKarmaToAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
            {
                this.IsNewHolon = false;
                await SaveAsync();
            }

            //TODO: Handle OASISResult properly with Save above, etc.
            return new OASISResult<KarmaAkashicRecord>(record);
        }

        public OASISResult<KarmaAkashicRecord> KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = AddKarmaToAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
            {
                this.IsNewHolon = false;
                Save();
            }

            //TODO: Handle OASISResult properly with Save above, etc.
            return new OASISResult<KarmaAkashicRecord>(record);
        }

        public async Task<OASISResult<KarmaAkashicRecord>> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = RemoveKarmaFromAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
            {
                this.IsNewHolon = false;
                await SaveAsync();
            }

            //TODO: Handle OASISResult properly with Save above, etc.
            return new OASISResult<KarmaAkashicRecord>(record);
        }

        public OASISResult<KarmaAkashicRecord> KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = RemoveKarmaFromAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
            {
                this.IsNewHolon = false;
                Save();
            }

            //TODO: Handle OASISResult properly with Save above, etc.
            return new OASISResult<KarmaAkashicRecord>(record);
        }

        private KarmaAkashicRecord AddKarmaToAkashicRecord(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, int karmaOverride = 0)
        {
            int karma = GetKarmaForType(karmaType);

            if (karmaType == KarmaTypePositive.Other)
                karma = karmaOverride;

            this.Karma += karma;

            KarmaAkashicRecord record = new KarmaAkashicRecord
            {
                AvatarId = Id,
                Date = DateTime.Now,
                Karma = karma,
                TotalKarma = this.Karma,
                Provider = ProviderManager.CurrentStorageProviderType,
                KarmaSourceTitle = karamSourceTitle,
                KarmaSourceDesc = karmaSourceDesc,
                WebLink = webLink,
                KarmaSource = new EnumValue<KarmaSourceType>(karmaSourceType),
                KarmaEarntOrLost = new EnumValue<KarmaEarntOrLost>(KarmaEarntOrLost.Earnt),
                KarmaTypeNegative = new EnumValue<KarmaTypeNegative>(KarmaTypeNegative.None),
                KarmaTypePositive = new EnumValue<KarmaTypePositive>(karmaType),
            };

            if (this.KarmaAkashicRecords == null)
                this.KarmaAkashicRecords = new List<KarmaAkashicRecord>();

            this.KarmaAkashicRecords.Add(record);
            return record;
        }

        private KarmaAkashicRecord RemoveKarmaFromAkashicRecord(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, int karmaOverride = 0)
        {
            int karma = GetKarmaForType(karmaType);

            if (karmaType == KarmaTypeNegative.Other)
                karma = karmaOverride;

            this.Karma -= karma;

            KarmaAkashicRecord record = new KarmaAkashicRecord
            {
                AvatarId = Id,
                Date = DateTime.Now,
                Karma = karma,
                TotalKarma = this.Karma,
                Provider = ProviderManager.CurrentStorageProviderType,
                KarmaSourceTitle = karamSourceTitle,
                KarmaSourceDesc = karmaSourceDesc,
                WebLink = webLink,
                KarmaSource = new EnumValue<KarmaSourceType>(karmaSourceType),
                KarmaEarntOrLost = new EnumValue<KarmaEarntOrLost>(KarmaEarntOrLost.Lost),
                KarmaTypeNegative = new EnumValue<KarmaTypeNegative>(karmaType),
                KarmaTypePositive = new EnumValue<KarmaTypePositive>(KarmaTypePositive.None),
            };

            if (this.KarmaAkashicRecords == null)
                this.KarmaAkashicRecords = new List<KarmaAkashicRecord>();

            this.KarmaAkashicRecords.Add(record);
            return record;
        }



        //public override bool HasHolonChanged(bool checkChildren = true)
        //{
        //    if (Original != null)
        //    {
        //        if (((IAvatarDetail)Original).DOB != DOB)
        //            return true;

        //        //if (((IAvatarDetail)Original).Email != Email)
        //        //    return true;

        //        //TODO: Finish this ASAP!
        //    }

        //    return base.HasHolonChanged(checkChildren);
        //}

        private int GetKarmaForType(KarmaTypePositive karmaType)
        {
            switch (karmaType)
            {
                case KarmaTypePositive.BeAHero:
                    return 7;

                case KarmaTypePositive.BeASuperHero:
                    return 8;

                case KarmaTypePositive.BeATeamPlayer:
                    return 5;

                case KarmaTypePositive.BeingDetermined:
                    return 5;

                case KarmaTypePositive.BeingFast:
                    return 5;

                case KarmaTypePositive.ContributingTowardsAGoodCauseAdministrator:
                    return 3;

                case KarmaTypePositive.ContributingTowardsAGoodCauseSpeaker:
                    return 8;

                case KarmaTypePositive.ContributingTowardsAGoodCauseContributor:
                    return 5;

                case KarmaTypePositive.ContributingTowardsAGoodCauseCreatorOrganiser:
                    return 10;

                case KarmaTypePositive.ContributingTowardsAGoodCauseFunder:
                    return 8;

                case KarmaTypePositive.ContributingTowardsAGoodCausePeacefulProtesterActivist:
                    return 5;

                case KarmaTypePositive.ContributingTowardsAGoodCauseSharer:
                    return 3;

                case KarmaTypePositive.HelpingAnimals:
                    return 5;

                case KarmaTypePositive.HelpingTheEnvironment:
                    return 5;

                case KarmaTypePositive.Other:
                    return 2;

                case KarmaTypePositive.OurWorld:
                    return 5;

                case KarmaTypePositive.SelfHelpImprovement:
                    return 2;

                //TODO: Finish...

                default:
                    return 0;
            }

        }

        private int GetKarmaForType(KarmaTypeNegative karmaType)
        {
            switch (karmaType)
            {
                case KarmaTypeNegative.AttackPhysciallyOtherPersonOrPeople:
                    return 10;

                case KarmaTypeNegative.AttackVerballyOtherPersonOrPeople:
                    return 5;

                case KarmaTypeNegative.BeingSelfish:
                    return 3;

                case KarmaTypeNegative.DisrespectPersonOrPeople:
                    return 4;

                case KarmaTypeNegative.DropLitter:
                    return 9;

                case KarmaTypeNegative.HarmingAnimals:
                    return 10;

                case KarmaTypeNegative.HarmingChildren:
                    return 9;

                case KarmaTypeNegative.HarmingNature:
                    return 10;

                case KarmaTypeNegative.NotTeamPlayer:
                    return 3;

                case KarmaTypeNegative.NutritionEatDiary:
                    return 6;

                case KarmaTypeNegative.NutritionEatDrinkUnhealthy:
                    return 3;

                case KarmaTypeNegative.NutritionEatMeat:
                    return 7;

                case KarmaTypeNegative.Other:
                    return 1;

                case KarmaTypeNegative.OurWorldAttackOtherPlayer:
                    return 7;

                case KarmaTypeNegative.OurWorldBeSelfish:
                    return 4;

                case KarmaTypeNegative.OurWorldDisrespectOtherPlayer:
                    return 5;

                case KarmaTypeNegative.OurWorldDropLitter:
                    return 7;

                case KarmaTypeNegative.OurWorldNotTeamPlayer:
                    return 3;

                default:
                    return 0;
            }
        }

        //public new async Task<IAvatarDetail> SaveAsync()
        //{
        //    OASISResult<IAvatarDetail> result = await (ProviderManager.CurrentStorageProvider).SaveAvatarDetailAsync(this);
        //    return result.Result;
        //}
        //public new IAvatarDetail Save()
        //{
        //    return (ProviderManager.CurrentStorageProvider).SaveAvatarDetail(this).Result;
        //}

        public bool LoadChildHolons()
        {
            throw new NotImplementedException();
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            throw new NotImplementedException();
        }

        public OASISResult<IAvatarDetail> Save()
        {
            throw new NotImplementedException();
        }

        public Task<OASISResult<IAvatarDetail>> SaveAsync()
        {
            throw new NotImplementedException();
        }
    }
}
