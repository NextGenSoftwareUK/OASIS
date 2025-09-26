//using System.Text.Json;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

//namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
//{
//    public class STARNETHolon : Holon, ISTARNETHolon
//    {
//        private string _STARNETDNAJSONName = "STARNETDNAJSON";
//        private ISTARNETDNA _STARNETDNA;

//        public STARNETHolon()
//        {
//            this.HolonType = HolonType.STARNETHolon;
//        }

//        public STARNETHolon(string STARNETDNAJSONName = "STARNETDNAJSON")
//        {
//            this.HolonType = HolonType.STARNETHolon;
//            _STARNETDNAJSONName = STARNETDNAJSONName;
//        }

//        public string STARNETHolonDNAJSONName
//        {
//            get
//            {
//                return _STARNETDNAJSONName;
//            }
//            set
//            {
//                _STARNETDNAJSONName = value;
//            }
//        }

//        public virtual ISTARNETDNA STARNETDNA
//        {
//            get
//            {
//                if (_STARNETDNA == null && MetaData[_STARNETDNAJSONName] != null && !string.IsNullOrEmpty(MetaData[_STARNETDNAJSONName].ToString()))
//                    _STARNETDNA = JsonSerializer.Deserialize<STARNETDNA>(MetaData[_STARNETDNAJSONName].ToString());

//                return _STARNETDNA;
//            }
//            set
//            {
//                _STARNETDNA = value;
//                MetaData[_STARNETDNAJSONName] = JsonSerializer.Serialize(value);
//            }
//        }

//        [CustomOASISProperty()]
//        public virtual byte[] PublishedSTARNETHolon { get; set; }
//    }
//}