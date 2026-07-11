using System;

namespace NextGenSoftware.OASIS.API.Core.Objects.Avatar
{
    public class AvatarPortrait
    {
        public Guid AvatarId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string ImageBase64 { get; set; }
    }
}
