using System;

namespace NextGenSoftware.OASIS.API.Core
{
    //public class Profile : Holon, IProfile
    public class Profile :  IProfile
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DOB { get; set; }
        public string PlayerAddress { get; set; }
        //public string Karma { get; private set; }
        public string Karma { get; set; }
        public string Level
        {
            get
            {
                //if (this.Karma > 0 && this.Karma < 100)
                //    return 1;

                //if (this.Karma >= 100 && this.Karma < 200)
                //    return 2;

                //if (this.Karma >= 200 && this.Karma < 300)
                //    return 3;

                //TODO: Add all the other levels here, all the way up to 100 for now! ;=)

                return "1"; //Default.
            }
        }

        public bool AddKarma(int karmaToAdd)
        {
            //this.Karma += karmaToAdd;
            return true;
        }

        public bool SubstractKarma(int karmaToRemove)
        {
            //this.Karma -= karmaToRemove;
            return true;
        }

        public bool Save()
        {
            return true;
        }

        public Profile()
        {
           // this.HolonType = HolonType.Profile;
        }
    }
}
