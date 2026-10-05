using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public class PortalCore : CelestialBodyCore<Portal>, IPortalCore
    {
        public IPortal Portal { get; set; }

        public PortalCore(IPortal portal) : base()
        {
            this.Portal = portal;
        }

        //public PortalCore(IPortal portal, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        //{
        //    this.Portal = portal;
        //}

        public PortalCore(IPortal portal, string providerKey, ProviderType providerType) : base(providerKey, providerType)
        {
            this.Portal = portal;
        }

        public PortalCore(IPortal portal, Guid id) : base(id)
        {
            this.Portal = portal;
        }
    }
}