using System;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core
{
    //public interface IAvatar : API.Core.IAvatar
    //{
    //    string HcAddressHash { get; set; }
    //}

    
    public interface IHcAvatar
    {
        Guid id { get; set; }
        Guid user_id { get; set; } //TODO: Remember to add this to the HC Rust code...
        string username { get; set; }
        string password { get; set; }
        string email { get; set; }
        string title { get; set; }
        string first_name { get; set; }
        string last_name { get; set; }
        string dob { get; set; }
        string player_address { get; set; }
        // public int Karma { get; private set; }
        int karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        int level { get; set; }
        string hc_address_hash { get; set; }
    }
}
