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
        string address { get; set; }
        //string town { get; set; } //TODO: FInish adding other fields if we still need this (think new version of HoloNET will make the class redundtant, it will convert automatically... ;-)
        // public int Karma { get; private set; }
        int karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        int level { get; set; }
        string hc_address_hash { get; set; }
    }
}
