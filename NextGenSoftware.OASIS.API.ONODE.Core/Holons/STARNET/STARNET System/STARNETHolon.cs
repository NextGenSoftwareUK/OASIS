using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARNETHolon : Holon, ISTARNETHolon //PublishableHolon
    {
        private string _STARNETHolonDNAJSONName = "STARNETHolonDNAJSON";
        private ISTARNETHolonDNA _STARNETHolonDNA;

        public STARNETHolon()
        {
            this.HolonType = HolonType.STARNETHolon;
        }

        public STARNETHolon(string STARNETHolonDNAJSONName = "STARNETHolonDNAJSON")
        {
            this.HolonType = HolonType.STARNETHolon;
            _STARNETHolonDNAJSONName = STARNETHolonDNAJSONName;
        }

        public string STARNETHolonDNAJSONName
        {
            get
            {
                return _STARNETHolonDNAJSONName;
            }
            set
            {
                _STARNETHolonDNAJSONName = value;
            }
        }

        public virtual ISTARNETHolonDNA STARNETHolonDNA
        {
            get
            {
                if (_STARNETHolonDNA == null && MetaData[_STARNETHolonDNAJSONName] != null && !string.IsNullOrEmpty(MetaData[_STARNETHolonDNAJSONName].ToString()))
                    _STARNETHolonDNA = JsonSerializer.Deserialize<STARNETHolonDNA>(MetaData[_STARNETHolonDNAJSONName].ToString());

                return _STARNETHolonDNA;
            }
            set
            {
                _STARNETHolonDNA = value;
                MetaData[_STARNETHolonDNAJSONName] = JsonSerializer.Serialize(value);
            }
        }

        [CustomOASISProperty()]
        public virtual byte[] PublishedSTARNETHolon { get; set; }
    }
}