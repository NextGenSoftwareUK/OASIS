
using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    public class LinkProviderKeyToAvatar
    {
        public Guid AvatarID { get; set; }

        public string ProviderKey { get; set; }
        public ProviderType ProviderType { get; set; }
    }
}