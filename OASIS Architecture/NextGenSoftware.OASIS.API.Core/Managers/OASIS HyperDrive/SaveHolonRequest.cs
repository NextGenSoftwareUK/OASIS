using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class SaveHolonRequest : IRequest
    {
        public Guid HolonId { get; set; }
        public object Holon { get; set; }
        public ProviderType PreferredProvider { get; set; } = ProviderType.Default;
    }
}
