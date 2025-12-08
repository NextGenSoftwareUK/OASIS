using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISProvider : IOASISProvider
    {
        public OASISDNA OASISDNA { get; set; }
        public string OASISDNAPath { get; set; }

        public string ProviderName { get; set; }
        public string ProviderDescription { get; set; }

        //TODO: Phase this out
        public EnumValue<ProviderCategory> ProviderCategory { get; set; }

        public List<EnumValue<ProviderCategory>> ProviderCategories { get; set; } = new List<EnumValue<ProviderCategory>>();

        public EnumValue<ProviderType> ProviderType { get; set; }
        //public bool ProviderActivated { get; set; }
        public bool IsProviderActivated { get; set; }

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
            //IsProviderActivated = true;
            return new OASISResult<bool>(false) { IsError = true, Message = "Error Occured In OASISProvider.ActivateProvider. Reason: The Provider Has Not Implemented ActivateProvider And Overriden This Base Abstract PlaceHolder!" };
        }

        virtual public OASISResult<bool> DeActivateProvider()
        {
            //IsProviderActivated = false;
            return new OASISResult<bool>(false) { IsError = true, Message = "Error Occured In OASISProvider.DeActivateProvider. Reason: The Provider Has Not Implemented DeActivateProvider And Overriden This Base Abstract PlaceHolder!" };
        }

        virtual public async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            //if (!IsProviderActivated.HasValue)
            //    IsProviderActivated = true;

            IsProviderActivated = false;
            return new OASISResult<bool>(false) { IsError = true, Message = "Error Occured In OASISProvider.ActivateProviderAsync. Reason: The Provider Has Not Implemented ActivateProviderAsync And Overriden This Base Abstract PlaceHolder!" };
        }

        virtual public async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            //if (!IsProviderActivated.HasValue)
            //    IsProviderActivated = true;

            IsProviderActivated = false;
            return new OASISResult<bool>(false) { IsError = true, Message = "Error Occured In OASISProvider.DeActivateProviderAsync. Reason: The Provider Has Not Implemented DeActivateProviderAsync And Overriden This Base Abstract PlaceHolder!" };
        }

        //virtual public OASISResult<bool> ActivateProvider()
        //{
        //    IsProviderActivated = true;
        //    return new OASISResult<bool>(true);
        //}

        //virtual public OASISResult<bool> DeActivateProvider()
        //{
        //    IsProviderActivated = false;
        //    return new OASISResult<bool>(true);
        //}

        //virtual public async Task<OASISResult<bool>> ActivateProviderAsync()
        //{
        //    if (!IsProviderActivated.HasValue) 
        //        IsProviderActivated = true;

        //    return new OASISResult<bool>(true);
        //}

        //virtual public async Task<OASISResult<bool>> DeActivateProviderAsync()
        //{
        //    if (!IsProviderActivated.HasValue)
        //        IsProviderActivated = true;

        //    return new OASISResult<bool>(true);
        //}
    }
}
