using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Google.Protobuf.WellKnownTypes;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class OAPP : Holon, IOAPP
    //public class OAPP : HolonBase, IOAPP
    {
        public OAPP()
        {
            this.HolonType = HolonType.OAPP;
        }

        //[CustomOASISProperty]
        //public OAPPType OAPPType { get; set; }

        //[CustomOASISProperty]
        //public GenesisType GenesisType { get; set; }
        ////public ICelestialHolon CelestialHolon { get; set; } //The base CelestialHolon that represents this OAPP (planet, moon, star, solar system, galaxy etc).

        //[CustomOASISProperty]
        //public Guid CelestialBodyId { get; set; }

        //[CustomOASISProperty]
        //public ICelestialBody CelestialBody { get; set; } //The base CelestialBody that represents this OAPP (planet, moon, star, super star, grand super star, etc).

        //[CustomOASISProperty]
        //public DateTime PublishedOn { get; set; }

        //[CustomOASISProperty]
        //public Guid PublishedByAvatarId { get; set; }

        [CustomOASISProperty()]
        public string OAPPDNAJSON { get; set; }

        private IOAPPDNA _OAPPDNA;

        [CustomOASISProperty(StoreAsJsonString = true)]
        public IOAPPDNA OAPPDNA
        {
            get
            {
                if (_OAPPDNA == null && MetaData["OAPPDNAJSON"] != null && !string.IsNullOrEmpty(MetaData["OAPPDNAJSON"].ToString()))
                    _OAPPDNA = JsonSerializer.Deserialize<OAPPDNA>(MetaData["OAPPDNAJSON"].ToString());

                return _OAPPDNA;
            }
            set
            {
                _OAPPDNA = value;
            }
        }

        //private IOAPPDNA _Name;

        //[CustomOASISProperty(StoreAsJsonString = true)]
        //public IOAPPDNA OAPPDNA { get; set; }
        

        [CustomOASISProperty()]
        public byte[] PublishedOAPP { get; set; }

        //[CustomOASISProperty]
        //public string CreatedByAvatarUsername { get; set; }

        //[CustomOASISProperty]
        //public string PublishedByAvatarUsername { get; set; }

        //TODO:More to come! ;-)
    }
}
