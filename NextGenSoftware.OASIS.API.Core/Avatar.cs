using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class Avatar : Holon, IAvatar
    {
       // public Guid UserId { get; set; } //TODO: Remember to add this to the HC Rust code...
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
        public AvatarType AvatarType { get; set; }
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

        //public bool AcceptTerms { get; set; }
        //public string VerificationToken { get; set; }
        //public DateTime? Verified { get; set; }
        //public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        //public string ResetToken { get; set; }
        //public DateTime? ResetTokenExpires { get; set; }
        //public DateTime? PasswordReset { get; set; }
        //public DateTime Created { get; set; }
        //public DateTime? Updated { get; set; }
        //public List<RefreshToken> RefreshTokens { get; set; }

        //public bool OwnsToken(string token)
        //{
        //    return this.RefreshTokens?.Find(x => x.Token == token) != null;
        //}



        public bool AcceptTerms { get; set; }
        public string VerificationToken { get; set; }
        public DateTime? Verified { get; set; }
        public bool IsVerified => Verified.HasValue || PasswordReset.HasValue;
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpires { get; set; }
        public DateTime? PasswordReset { get; set; }
        //public DateTime Created { get; set; }
        //public DateTime? Updated { get; set; }
        public List<RefreshToken> RefreshTokens { get; set; }

        public bool OwnsToken(string token)
        {
            return this.RefreshTokens?.Find(x => x.Token == token) != null;
        }



        // A record of all the karma the user has earnt/lost along with when and where from.
        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }

        public async Task<KarmaAkashicRecord> KarmaEarnt(KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true)
        {
            KarmaAkashicRecord record = new KarmaAkashicRecord { KarmaEarntOrLost = KarmaEarntOrLost.Earnt, Date = DateTime.Now, Karma = GetKarmaForType(karmaType), KarmaSource = karmaSourceType, KarmaSourceTitle = karamSourceTitle, KarmaSourceDesc = karmaSourceDesc, KarmaTypePositive = karmaType, AvatarId = Id, Provider = ProviderManager.CurrentStorageProviderType };
            this.Karma += GetKarmaForType(karmaType);

            if (this.KarmaAkashicRecords == null)
                this.KarmaAkashicRecords = new List<KarmaAkashicRecord>();

            this.KarmaAkashicRecords.Add(record);

            if (autoSave)
                await Save();

            return record;
        }

        public async Task<KarmaAkashicRecord> KarmaLost(KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true)
        {
            KarmaAkashicRecord record = new KarmaAkashicRecord { KarmaEarntOrLost = KarmaEarntOrLost.Lost, Date = DateTime.Now, Karma = GetKarmaForType(karmaType), KarmaSource = karmaSourceType, KarmaSourceTitle = karamSourceTitle, KarmaSourceDesc = karmaSourceDesc, KarmaTypeNegative = karmaType, AvatarId = Id, Provider = ProviderManager.CurrentStorageProviderType };
            this.Karma -= GetKarmaForType(karmaType);

            if (this.KarmaAkashicRecords == null)
                this.KarmaAkashicRecords = new List<KarmaAkashicRecord>();

            this.KarmaAkashicRecords.Add(record);

            if (autoSave)
                await Save();

            return record;
        }

        public async Task<bool> Save()
        {
            await ((IOASISStorage)ProviderManager.CurrentStorageProvider).SaveAvatarAsync(this);
            return true;
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
