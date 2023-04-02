
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.TestHarness;
using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    //public class Avatar : API.Core.Avatar, IAvatar
    //{
    //    public string HcAddressHash { get; set; }
    //}

    
    public class HcAvatar : HoloNETAuditEntryBaseClass, IHcAvatar
    {
        public HcAvatar() : base("oasis", "get_entry_avatar", "create_entry_avatar", "update_entry_avatar", "delete_entry_avatar") { }
        public HcAvatar(HoloNETClient holoNETClient) : base("oasis", "get_entry_avatar", "create_entry_avatar", "update_entry_avatar", "delete_entry_avatar", holoNETClient) { }

        [HolochainFieldName("id")]
        public Guid Id { get; set; }
        //public Guid user_id { get; set; } //TODO: Remember to add this to the HC Rust code...

        [HolochainFieldName("username")]
        public string Username { get; set; }

        [HolochainFieldName("password")]
        public string Password { get; set; }

        [HolochainFieldName("email")]
        public string Email { get; set; }

        [HolochainFieldName("title")]
        public string Title { get; set; }

        [HolochainFieldName("first_name")]
        public string FirstName { get; set; }

        [HolochainFieldName("last_name")]
        public string LastName { get; set; }

        //[HolochainFieldName("dob")]
        //public string DOB { get; set; }

        //[HolochainFieldName("address")]
        //public string Address { get; set; }

        //[HolochainFieldName("karma")]
        //public int Karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?

        //[HolochainFieldName("level")]
        //public int Level { get; set; }

        [HolochainFieldName("provider_key")]
        public string ProviderKey { get; set; }

        [HolochainFieldName("holon_type")]
        public HolonType HolonType { get; set; }


        /*
        public string id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string title { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string dob { get; set; }
        public string address { get; set; }
        public int karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        public int level { get; set; }
        public string provider_key { get; set; }
        public HolonType holon_type { get; set; }
        */
    }
}
