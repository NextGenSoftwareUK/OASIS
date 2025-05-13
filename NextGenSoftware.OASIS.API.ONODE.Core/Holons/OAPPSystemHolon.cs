using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    //public class OAPPTemplate : Holon, IOAPPTemplate //PublishableHolon, IOAPPTemplate
    public class OAPPSystemHolon : Holon, IOAPPSystemHolon //PublishableHolon, IOAPPTemplate
    {
        private IOAPPSystemHolonDNA _OAPPSystemHolonDNA;

        public OAPPSystemHolon()
        {
            this.HolonType = HolonType.OAPPTemplate; 
        }

        public IOAPPSystemHolonDNA OAPPSystemHolonDNA
        {
            get
            {
                if (_OAPPSystemHolonDNA == null && MetaData["OAPPSystemHolonDNAJSON"] != null && !string.IsNullOrEmpty(MetaData["OAPPSystemHolonDNAJSON"].ToString()))
                    _OAPPSystemHolonDNA = JsonSerializer.Deserialize<OAPPSystemHolonDNA>(MetaData["OAPPSystemHolonDNAJSON"].ToString());

                return _OAPPSystemHolonDNA;
            }
            set
            {
                _OAPPSystemHolonDNA = value;
                MetaData["OAPPSystemHolonDNAJSON"] = JsonSerializer.Serialize(value);
            }
        }

        [CustomOASISProperty()]
        public byte[] PublishedOAPSystemItem { get; set; }

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