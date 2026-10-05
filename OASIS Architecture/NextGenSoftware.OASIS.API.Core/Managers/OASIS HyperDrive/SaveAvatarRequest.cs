using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class SaveAvatarRequest : IRequest
    {
        public Guid AvatarId { get; set; }
        public object Avatar { get; set; }
        public ProviderType PreferredProvider { get; set; } = ProviderType.Default;
        
        // IRequest interface implementation
        public string RequestType { get; set; } = "SaveAvatar";
        public int Priority { get; set; } = 5;
        public string ProviderTypeString { get; set; } = "Default";
        public System.Collections.Generic.Dictionary<string, object> Parameters { get; set; } = new System.Collections.Generic.Dictionary<string, object>();
    }
}
