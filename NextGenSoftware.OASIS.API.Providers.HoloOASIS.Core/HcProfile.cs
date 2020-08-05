
using System;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core
{
    //public class Profile : API.Core.Profile, IProfile    
    //{
    //    public string HcAddressHash { get; set; }
    //}

    public class HcProfile : IHcProfile
    {
        public Guid id { get; set; }
        public Guid user_id { get; set; } //TODO: Remember to add this to the HC Rust code...
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string title { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string dob { get; set; }
        public string player_address { get; set; }
        // public int Karma { get; private set; }
        public int karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        public int level { get; set; }
        public string hc_address_hash { get; set; }
    }
}
