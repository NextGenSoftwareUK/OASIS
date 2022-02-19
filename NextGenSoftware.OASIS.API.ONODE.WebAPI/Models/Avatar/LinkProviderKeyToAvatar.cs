
using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    public class LinkProviderKeyToAvatarParams
    {
        public string AvatarID { get; set; }
        public string ProviderKey { get; set; }
        public string ProviderType { get; set; }
    }
}