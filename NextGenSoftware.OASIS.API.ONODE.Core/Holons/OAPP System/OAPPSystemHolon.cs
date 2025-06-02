using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public abstract class OAPPSystemHolon : Holon, IOAPPSystemHolon //PublishableHolon
    {
        private string _OAPPSystemHolonDNAJSONName = "OAPPSystemHolonDNAJSON";
        private IOAPPSystemHolonDNA _OAPPSystemHolonDNA;

        public OAPPSystemHolon(string OAPPSystemHolonDNAJSONName = "OAPPSystemHolonDNAJSON")
        {
            this.HolonType = HolonType.OAPPSystemHolon;
            _OAPPSystemHolonDNAJSONName = OAPPSystemHolonDNAJSONName;
        }

        public string OAPPSystemHolonDNAJSONName
        {
            get
            {
                return _OAPPSystemHolonDNAJSONName;
            }
            set
            {
                _OAPPSystemHolonDNAJSONName = value;
            }
        }

        public virtual IOAPPSystemHolonDNA OAPPSystemHolonDNA
        {
            get
            {
                if (_OAPPSystemHolonDNA == null && MetaData[_OAPPSystemHolonDNAJSONName] != null && !string.IsNullOrEmpty(MetaData[_OAPPSystemHolonDNAJSONName].ToString()))
                    _OAPPSystemHolonDNA = JsonSerializer.Deserialize<OAPPSystemHolonDNA>(MetaData[_OAPPSystemHolonDNAJSONName].ToString());

                return _OAPPSystemHolonDNA;
            }
            set
            {
                _OAPPSystemHolonDNA = value;
                MetaData[_OAPPSystemHolonDNAJSONName] = JsonSerializer.Serialize(value);
            }
        }

        [CustomOASISProperty()]
        public virtual byte[] PublishedOAPPSystemHolon { get; set; }
    }
}