using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class AvatarDetail : Holon, IAvatarDetail
    {
        public AvatarDetail()
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


        //TODO: Think best to encrypt these?
        public Dictionary<ProviderType, string> ProviderPrivateKey { get; set; } = new Dictionary<ProviderType, string>();  //Unique private key used by each provider (part of private/public key pair).
        public Dictionary<ProviderType, string> ProviderPublicKey { get; set; } = new Dictionary<ProviderType, string>();
        public Dictionary<ProviderType, string> ProviderUsername { get; set; } = new Dictionary<ProviderType, string>();  // This is only really needed when we need to store BOTH a id and username for a provider (ProviderKey on Holon already stores either id/username etc).
                                                                                                                          // public Dictionary<ProviderType, string> ProviderId { get; set; } = new Dictionary<ProviderType, string>(); // The ProviderKey property on the base Holon object can store ids, usernames, etc that uniqueliy identity that holon in the provider (although the Guid is globally unique we still need to map the Holons the unique id/username/etc for each provider).
        public Dictionary<ProviderType, string> ProviderWalletAddress { get; set; } = new Dictionary<ProviderType, string>();

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
        public string Image2D { get; set; }
        public string Model3D { get; set; }
        public List<HeartRateEntry> HeartRateData { get; set; }
       
      //  public EnumValue<OASISType> CreatedOASISType { get; set; }
        // public int Karma { get; private set; }
        public int Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        public int XP { get; set; }
        public IOmiverse Omiverse { get; set; } //We have all of creation inside of us... ;-)
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
        public int Level
        {
            get
            {
                if (this.Karma > 0 && this.Karma < 100)
                    return 1;

                if (this.Karma >= 100 && this.Karma < 200)
                    return 2;

                if (this.Karma >= 200 && this.Karma < 300)
                    return 3;

                if (this.Karma >= 777)
                    return 99;

                //TODO: Add all the other levels here, all the way up to 100 for now! ;=)

                return 1; //Default.
            }
        }

        // A record of all the karma the user has earnt/lost along with when and where from.
        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }

        public async Task<OASISResult<KarmaAkashicRecord>> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = AddKarmaToAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                await SaveAsync();

            //TODO: Handle OASISResult properly with Save above, etc.
            return new OASISResult<KarmaAkashicRecord>(record);
        }

        public OASISResult<KarmaAkashicRecord> KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = AddKarmaToAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                Save();

            //TODO: Handle OASISResult properly with Save above, etc.
            return new OASISResult<KarmaAkashicRecord>(record);
        }

        public async Task<OASISResult<KarmaAkashicRecord>> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = RemoveKarmaFromAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                await SaveAsync();

            //TODO: Handle OASISResult properly with Save above, etc.
            return new OASISResult<KarmaAkashicRecord>(record);
        }

        public OASISResult<KarmaAkashicRecord> KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = RemoveKarmaFromAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                Save();

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



        public override bool HasHolonChanged(bool checkChildren = true)
        {
            if (Original != null)
            {
                if (((IAvatarDetail)Original).DOB != DOB)
                    return true;

                //if (((IAvatarDetail)Original).Email != Email)
                //    return true;

                //TODO: Finish this ASAP!
            }

            return base.HasHolonChanged(checkChildren);
        }

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

        public new async Task<IAvatarDetail> SaveAsync()
        {
            return await (ProviderManager.CurrentStorageProvider).SaveAvatarDetailAsync(this);
        }
        public new IAvatarDetail Save()
        {
            return (ProviderManager.CurrentStorageProvider).SaveAvatarDetail(this);
        }
    }
}