using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class Avatar : Holon, IAvatar
    {
        // public Guid UserId { get; set; } //TODO: Remember to add this to the HC Rust code...

        public Guid AvatarId 
        { 
            get
            {
                return this.Id;
            }
        }

        public new string Name
        {
            get
            {
                return FullName;
            }
        }

        //TODO: Think best to encrypt these?
        public Dictionary<ProviderType, string> ProviderPrivateKey { get; set; } = new Dictionary<ProviderType, string>(); //Unique private key used by each provider (part of private/public key pair).
        public string Username { get; set; } //TODO: Might get rid of this and use Avatar.Name instead (from base Holon)? Would that be confusing?
        public string Password { get; set; }
        public string Email { get; set; }
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
        public DateTime DOB { get; set; }
        public string Address { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string Postcode { get; set; }
        public string Mobile { get; set; }
        public string Landline { get; set; }
        public EnumValue<AvatarType> AvatarType { get; set; }
        // public int Karma { get; private set; }
        public int Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
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

        public bool AcceptTerms { get; set; }
        public string VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string ResetToken { get; set; }
        public string JwtToken { get; set; }

        [JsonIgnore] // refresh token is returned in http only cookie
        public string RefreshToken { get; set; }

        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        //public DateTime Created { get; set; }
        //public DateTime? Updated { get; set; }

        public List<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }

        // A record of all the karma the user has earnt/lost along with when and where from.
        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }

        /*
        public KarmaAkashicRecord SetKarmaForDataObject(int karma, bool autoSave = false)
        {
            //TODO: Not sure how to handle this? We dont want to allow people to manually set the karma, because it needs to be logged through 
            //the correct KarmaEarn/Lost methods. But this causes an issue for the OASIS Storage Providers when loading the data objects, because they 
            //cannot set the karma property. So this is an attempt to resolve this but needs more thought, we dont want to add unnecessary Saves for every time we load the object slowing things down for example!
            this.Karma = karma;

            KarmaAkashicRecord record = new KarmaAkashicRecord { KarmaEarntOrLost = KarmaEarntOrLost.DataObject, KarmaTypeNegative = KarmaTypeNegative.None, Date = DateTime.Now, Karma = karma, KarmaSource = KarmaSourceType.DataObject, KarmaSourceTitle = "", KarmaSourceDesc = "", KarmaTypePositive = KarmaTypePositive.None, AvatarId = Id, Provider = ProviderManager.CurrentStorageProviderType };

            if (this.KarmaAkashicRecords == null)
                this.KarmaAkashicRecords = new List<KarmaAkashicRecord>();

            this.KarmaAkashicRecords.Add(record);

            if (autoSave)
                Save();

            return record;
        }*/

        /*
        public async Task<KarmaAkashicRecord> SetKarmaForDataObjectAsync(int karma, bool autoSave = true)
        {
            this.Karma = karma;

            KarmaAkashicRecord record = new KarmaAkashicRecord { KarmaEarntOrLost = KarmaEarntOrLost.DataObject, KarmaTypeNegative = KarmaTypeNegative.None, Date = DateTime.Now, Karma = karma, KarmaSource = KarmaSourceType.DataObject, KarmaSourceTitle = string.Concat("Setting DataObject For ", ProviderManager.CurrentStorageProviderName, " Provider."), KarmaSourceDesc = "", KarmaTypePositive = KarmaTypePositive.None, AvatarId = Id, Provider = ProviderManager.CurrentStorageProviderType, ProviderName = ProviderManager.CurrentStorageProviderName };

            if (this.KarmaAkashicRecords == null)
                this.KarmaAkashicRecords = new List<KarmaAkashicRecord>();

            this.KarmaAkashicRecords.Add(record);

            if (autoSave)
                await SaveAsync();

            return record;
        }*/

        public async Task<KarmaAkashicRecord> KarmaEarntAsync(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = AddKarmaToAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                await SaveAsync();

            return record;
        }

        public KarmaAkashicRecord KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = AddKarmaToAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                Save();

            return record;
        }

        public async Task<KarmaAkashicRecord> KarmaLostAsync(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = RemoveKarmaFromAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                await SaveAsync();

            return record;
        }

        public KarmaAkashicRecord KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string webLink = null, bool autoSave = true, int karmaOverride = 0)
        {
            KarmaAkashicRecord record = RemoveKarmaFromAkashicRecord(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, webLink, karmaOverride);

            if (autoSave)
                Save();

            return record;
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

        public async Task<IAvatar> SaveAsync()
        {
            return await ((IOASISStorage)ProviderManager.CurrentStorageProvider).SaveAvatarAsync(this);
        }
        public IAvatar Save()
        {
            return ((IOASISStorage)ProviderManager.CurrentStorageProvider).SaveAvatar(this);
        }

        public Avatar()
        {
            this.HolonType = HolonType.Avatar;
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
                    return  3;

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
    }
}
