using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    public class AvatarImage
    {
        public Guid AvatarId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string ImageBase64 { get; set; }
    }
}