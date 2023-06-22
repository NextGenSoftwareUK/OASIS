
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class ParkManager : OASISManager
    {
        public ParkManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        public ParkManager(OASISDNA OASISDNA = null) : base(OASISDNA)
        {
            //TODO: Finish implementing ASAP...
        }
    }
}
