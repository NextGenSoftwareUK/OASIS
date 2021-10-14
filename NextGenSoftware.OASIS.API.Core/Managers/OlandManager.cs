using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class OlandManager : OASISManager
    {
        public OlandManager(IOASISStorage OASISStorageProvider, OASISDNA OASISDNA = null) 
            : base(OASISStorageProvider, OASISDNA)
        {
        }
    }
}