using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public interface IHcAvatar
    {
        Guid Id { get; set; }
        string Username { get; set; }
        string Password { get; set; }
        string Email { get; set; }
        string Title { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string ProviderKey { get; set; }
        HolonType HolonType { get; set; }

        //string id { get; set; }
        //string username { get; set; }
        //string password { get; set; }
        //string email { get; set; }
        //string title { get; set; }
        //string first_name { get; set; }
        //string last_name { get; set; }
        //string provider_key { get; set; }
        //HolonType holon_type { get; set; }

        //string dob { get; set; }
        //string address { get; set; }
        //int karma { get; set; } //TODO: This really needs to have a private setter but in the HoloOASIS provider it needs to copy the object along with each property... would prefer another work around if possible?
        //int level { get; set; }   
    }
}