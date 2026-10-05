
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class ParkManager : OASISManager
    {
        public ParkManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public ParkManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
            //TODO: Finish implementing ASAP...
        }
    }
}
