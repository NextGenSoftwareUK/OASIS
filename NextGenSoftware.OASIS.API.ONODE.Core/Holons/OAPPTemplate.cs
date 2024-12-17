using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class OAPPTemplate : PublishableHolon, IOAPPTemplate, IPublishableHolon
    {
        public OAPPTemplate()
        {
            this.HolonType = HolonType.OAPPTemplate; 
        }

        [CustomOASISProperty]
        public OAPPTemplateType OAPPTemplateType { get; set; }

        [CustomOASISProperty]
        public string OAPPTemplatePath { get; set; }

        [CustomOASISProperty]
        public string OAPPTemplatePublishedPath { get; set; }

        [CustomOASISProperty]
        public bool OAPPTemplatePublishedOnSTARNET { get; set; }

        [CustomOASISProperty]
        public bool OAPPTemplatePublishedToCloud { get; set; }

        [CustomOASISProperty]
        public ProviderType OAPPTemplatePublishedProviderType { get; set; }

        [CustomOASISProperty]
        public long OAPPTemplateFileSize { get; set; }

        [CustomOASISProperty]
        public int Versions { get; set; }

        [CustomOASISProperty]
        public int Downloads { get; set; }
    }
}