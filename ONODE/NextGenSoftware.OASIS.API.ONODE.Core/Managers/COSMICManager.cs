using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    /// <summary>
    /// COSMICManager exposes the full COSMIC ORM / Omniverse object model to the WEB4 OASIS API.
    /// It provides strongly-typed Create/Read/Update/Delete and rich Get X For Y operations
    /// for all CelestialBodies and CelestialSpaces defined in the STAR ontology.
    /// </summary>
    public partial class COSMICManager : COSMICManagerBase
    {
        private IOmiverse _omiverse = null;

        public COSMICManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
        }

        public COSMICManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {
        }

        public IOmiverse Omiverse
        {
            get
            {
                if (_omiverse == null)
                    _omiverse = GetOmniverseAsync().ConfigureAwait(false).GetAwaiter().GetResult().Result;

                return _omiverse;
            }
        }

    }
}
