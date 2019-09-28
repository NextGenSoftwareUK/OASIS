using System;

namespace NextGenSoftware.OASIS.API.Core
{
    public class Profile : Holon, IProfile
   // public class Profile :  IProfile
    {
        //  public Guid Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DOB { get; set; }
        public string PlayerAddress { get; set; }
       // public int Karma { get; private set; }
        public int Karma { get; set; }
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

        public bool AddKarma(int karmaToAdd, KarmaType karmaType)
        {
            this.Karma += karmaToAdd + GetKarmaForType(karmaType);

            /*
            switch (karmaType)
            {
                case KarmaType.ContributingTowardsAGoodCauseAdministrator:
                    this.Karma += karmaToAdd + 3;
                    break;

                case KarmaType.ContributingTowardsAGoodCauseSpeaker:
                    this.Karma += karmaToAdd + 8;
                    break;

                case KarmaType.ContributingTowardsAGoodCauseContributor:
                    this.Karma += karmaToAdd + 5;
                    break;

                case KarmaType.ContributingTowardsAGoodCauseCreatorOrganiser:
                    this.Karma += karmaToAdd + 10;
                    break;

                case KarmaType.ContributingTowardsAGoodCauseFunder:
                    this.Karma += karmaToAdd + 8;
                    break;

                case KarmaType.ContributingTowardsAGoodCausePeacefulProtesterActivist:
                    this.Karma += karmaToAdd + 5;
                    break;

                case KarmaType.ContributingTowardsAGoodCauseSharer:
                    this.Karma += karmaToAdd + 3;
                    break;

                case KarmaType.HelpingAnimals:
                    this.Karma += karmaToAdd + 5;
                    break;

                case KarmaType.HelpingTheEnvironment:
                    this.Karma += karmaToAdd + 5;
                    break;

                case KarmaType.Other:
                    this.Karma += karmaToAdd + 2;
                    break;

                case KarmaType.OurWorld:
                    this.Karma += karmaToAdd + 5;
                    break;

                case KarmaType.SelfHelpImprovement:
                    this.Karma += karmaToAdd + 2;
                    break;
            }
            */
            return true;
        }

        public bool SubstractKarma(int karmaToRemove, KarmaType karmaType)
        {
            this.Karma -= karmaToRemove - GetKarmaForType(karmaType);
            return true;
        }

        public bool Save()
        {
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
