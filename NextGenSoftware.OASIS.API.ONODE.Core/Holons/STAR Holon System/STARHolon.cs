using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARHolon : Holon, ISTARHolon //PublishableHolon
    {
        private string _STARHolonDNAJSONName = "STARHolonDNAJSON";
        private ISTARHolonDNA _STARHolonDNA;

        public STARHolon()
        {
            this.HolonType = HolonType.STARHolon;
        }

        public STARHolon(string STARHolonDNAJSONName = "STARHolonDNAJSON")
        {
            this.HolonType = HolonType.STARHolon;
            _STARHolonDNAJSONName = STARHolonDNAJSONName;
        }

        public string STARHolonDNAJSONName
        {
            get
            {
                return _STARHolonDNAJSONName;
            }
            set
            {
                _STARHolonDNAJSONName = value;
            }
        }

        public virtual ISTARHolonDNA STARHolonDNA
        {
            get
            {
                if (_STARHolonDNA == null && MetaData[_STARHolonDNAJSONName] != null && !string.IsNullOrEmpty(MetaData[_STARHolonDNAJSONName].ToString()))
                    _STARHolonDNA = JsonSerializer.Deserialize<STARHolonDNA>(MetaData[_STARHolonDNAJSONName].ToString());

                return _STARHolonDNA;
            }
            set
            {
                _STARHolonDNA = value;
                MetaData[_STARHolonDNAJSONName] = JsonSerializer.Serialize(value);
            }
        }

        [CustomOASISProperty()]
        public virtual byte[] PublishedSTARHolon { get; set; }
    }
}