using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using System.IO;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISProvider : IOASISProvider
    {
        public OASISDNA OASISDNA { get; set; }
        public string OASISDNAPath { get; set; }

        public string ProviderName { get; set; }
        public string ProviderDescription { get; set; }

        public EnumValue<ProviderCategory> ProviderCategory { get; set; }

        public EnumValue<ProviderType> ProviderType { get; set; }
        //public bool ProviderActivated { get; set; }
        public bool ProviderActivated { get; private set; }

        public OASISProvider()
        {
            if (OASISDNAManager.OASISDNA == null)
            {
                OASISDNAManager.LoadDNA();
                this.OASISDNA = OASISDNAManager.OASISDNA;
                this.OASISDNAPath = OASISDNAManager.OASISDNAPath;
            }
        }

        public OASISProvider(string OASISDNAPath)
        {
            if (OASISDNAManager.OASISDNA == null || File.Exists(OASISDNAPath))
            {
                this.OASISDNAPath = OASISDNAPath;
                OASISDNAManager.LoadDNA(OASISDNAPath);
                this.OASISDNA = OASISDNAManager.OASISDNA;
            }
        }

        public OASISProvider(OASISDNA OASISDNA)
        {
            this.OASISDNA = OASISDNA;
            this.OASISDNAPath = OASISDNAManager.OASISDNAPath;
        }

        public OASISProvider(OASISDNA OASISDNA, string OASISDNAPath)
        {
            this.OASISDNA = OASISDNA;
            this.OASISDNAPath = OASISDNAPath;
        }

        virtual public OASISResult<bool> ActivateProvider()
        {
            ProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        virtual public OASISResult<bool> DeActivateProvider()
        {
            ProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        virtual public async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            ProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        virtual public async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            ProviderActivated = false;
            return new OASISResult<bool>(true);
        }
    }
}
