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
    }
}
