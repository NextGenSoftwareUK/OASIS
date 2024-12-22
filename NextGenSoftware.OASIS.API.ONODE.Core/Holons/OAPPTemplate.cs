using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class OAPPTemplate : Holon, IOAPPTemplate //PublishableHolon, IOAPPTemplate
    {
        private IOAPPTemplateDNA _OAPPTemplateDNA;

        public OAPPTemplate()
        {
            this.HolonType = HolonType.OAPPTemplate; 
        }

        public IOAPPTemplateDNA OAPPTemplateDNA
        {
            get
            {
                if (_OAPPTemplateDNA == null && MetaData["OAPPTemplateDNAJSON"] != null && !string.IsNullOrEmpty(MetaData["OAPPTemplateDNAJSON"].ToString()))
                    _OAPPTemplateDNA = JsonSerializer.Deserialize<OAPPTemplateDNA>(MetaData["OAPPTemplateDNAJSON"].ToString());

                return _OAPPTemplateDNA;
            }
            set
            {
                _OAPPTemplateDNA = value;
                MetaData["OAPPTemplateDNAJSON"] = JsonSerializer.Serialize(value);
            }
        }

        [CustomOASISProperty()]
        public byte[] PublishedOAPPTemplate { get; set; }

        //[CustomOASISProperty]
        //public OAPPTemplateType OAPPTemplateType { get; set; }

        //[CustomOASISProperty]
        //public string OAPPTemplatePath { get; set; }

        //[CustomOASISProperty]
        //public string OAPPTemplatePublishedPath { get; set; }

        //[CustomOASISProperty]
        //public bool OAPPTemplatePublishedOnSTARNET { get; set; }

        //[CustomOASISProperty]
        //public bool OAPPTemplatePublishedToCloud { get; set; }

        //[CustomOASISProperty]
        //public ProviderType OAPPTemplatePublishedProviderType { get; set; }

        //[CustomOASISProperty]
        //public long OAPPTemplateFileSize { get; set; }

        //[CustomOASISProperty]
        //public int Versions { get; set; }

        //[CustomOASISProperty]
        //public int Downloads { get; set; }
    }
}