using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public class Profile : Holon, IProfile
    {
        public Guid UserId { get; set; } //TODO: Remember to add this to the HC Rust code...
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DOB { get; set; }
        public string PlayerAddress { get; set; }
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

                //TODO: Add all the other levels here, all the way up to 100 for now! ;=)

                return 1; //Default.
            }
        }

        // A record of all the karma the user has earnt/lost along with when and where from.
        public List<KarmaAkashicRecord> KarmaAkashicRecords { get; set; }

        public async Task<KarmaAkashicRecord> KarmaEarnt(KarmaType karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true)
        {
            KarmaAkashicRecord record = new KarmaAkashicRecord { KarmaEarntOrLost = KarmaEarntOrLost.Earnt, Date = DateTime.Now, Karma = GetKarmaForType(karmaType), KarmaSource = karmaSourceType, KarmaSourceTitle = karamSourceTitle, KarmaSourceDesc = karmaSourceDesc, KarmaType = karmaType, UserId = UserId, Provider = ProviderManager.CurrentStorageProviderType };
            this.Karma += GetKarmaForType(karmaType);
            this.KarmaAkashicRecords.Add(record);

            if (autoSave)
                await Save();

            return record;
        }

        public async Task<KarmaAkashicRecord> KarmaLost(KarmaType karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, bool autoSave = true)
        {
            KarmaAkashicRecord record = new KarmaAkashicRecord { KarmaEarntOrLost = KarmaEarntOrLost.Lost, Date = DateTime.Now, Karma = GetKarmaForType(karmaType), KarmaSource = karmaSourceType, KarmaSourceTitle = karamSourceTitle, KarmaSourceDesc = karmaSourceDesc, KarmaType = karmaType, UserId = UserId, Provider = ProviderManager.CurrentStorageProviderType };
            this.Karma -= GetKarmaForType(karmaType);
            this.KarmaAkashicRecords.Add(record);

            if (autoSave)
                await Save();

            return record;
        }

        public async Task<bool> Save()
        {
            await ((IOASISStorage)ProviderManager.CurrentStorageProvider).SaveProfileAsync(this);
            return true;
        }

        public Profile()
        {
            this.HolonType = HolonType.Profile;
        }

        private int GetKarmaForType(KarmaType karmaType)
        {
            switch (karmaType)
            {
                case KarmaType.ContributingTowardsAGoodCauseAdministrator:
                    return  3;

                case KarmaType.ContributingTowardsAGoodCauseSpeaker:
                    return 8;

                case KarmaType.ContributingTowardsAGoodCauseContributor:
                    return 5;

                case KarmaType.ContributingTowardsAGoodCauseCreatorOrganiser:
                    return 10;

                case KarmaType.ContributingTowardsAGoodCauseFunder:
                    return 8;                   

                case KarmaType.ContributingTowardsAGoodCausePeacefulProtesterActivist:
                    return 5;

                case KarmaType.ContributingTowardsAGoodCauseSharer:
                    return 3;

                case KarmaType.HelpingAnimals:
                    return 5;

                case KarmaType.HelpingTheEnvironment:
                    return 5;

                case KarmaType.Other:
                    return 2;

                case KarmaType.OurWorld:
                    return 5;

                case KarmaType.SelfHelpImprovement:
                    return 2;

                default:
                    return 0;
            }
                
        }
    }
}
