using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public abstract class OASISManager : IOASISManager
    {
        public HolonManager Data { get; set; }
        public OASISDNA OASISDNA {get; set; }

        public OASISManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null)
        {
            Data = new HolonManager(OASISStorageProvider, OASISDNA);
            HandleDNA(OASISDNA);
        }

        public OASISManager(OASISDNA OASISDNA = null)
        {
            HandleDNA(OASISDNA);
            OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProvider();

            if (!result.IsError && result.Result != null)
                Data = new HolonManager(result.Result);
            else
            {
                ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);
            }
        }

        private void HandleDNA(OASISDNA dna)
        {
            if (dna == null)
            {
                if (OASISDNAManager.OASISDNA == null)
                    OASISDNAManager.LoadDNA();

                this.OASISDNA = OASISDNAManager.OASISDNA;
            }
            else
                this.OASISDNA = dna;
        }
    }
}