using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class LoadAvatarRequest : IRequest
    {
        public Guid AvatarId { get; set; }
        public ProviderType PreferredProvider { get; set; } = ProviderType.Default;
    }
}
