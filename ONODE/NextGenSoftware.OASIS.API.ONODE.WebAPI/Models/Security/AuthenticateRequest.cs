using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    //[FromBody]
    public class AuthenticateRequest : OASISRequest
    {
        //[FromQuery]
        [Required]
        //[EmailAddress] //The username will default to their email address but they can later change it to something else so this could be any string.
        public string Username { get; set; }

        //[FromQuery]
        [Required]
        public string Password { get; set; }
    }
}