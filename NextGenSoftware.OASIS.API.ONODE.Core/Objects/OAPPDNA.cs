using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class OAPPDNA : IOAPPDNA
    {
        public Guid OAPPId { get; set; }
        public string OAPPName { get; set; }
        public string Description { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public DateTime CreatedOn { get; set; }
        public Guid PublishedByAvatarId { get; set; }
        public DateTime PublishedOn { get; set; }
        public bool PublishedOnSTARNET { get; set; }
        public OAPPType OAPPType { get; set; }
        public GenesisType GenesisType { get; set; }
        public ICelestialBody CelestialBody { get; set; } //optional
        public Guid CelestialBodyId { get; set; }
        public string CelestialBodyName { get; set; }
        public HolonType CelestialBodyType { get; set; }
        public IEnumerable<IZome> Zomes { get; set; }
        public string Version { get; set; }


        //public DateTime CreatedDate { get; set; }
        //public Guid CreatedByAvatarId { get; set; }
        //public DateTime UpdatedDate { get; set; }
        //public Guid UpdatedByAvatarId { get; set; }
        //public OAPPType OAPPType { get; set; }
        //public GenesisType GenesisType { get; set; }
        ////public ICelestialHolon CelestialHolon { get; set; } //The base CelestialHolon that represents this OAPP (planet, moon, star, solar system, galaxy etc).
        //public Guid CelestialBodyId { get; set; }
        //public ICelestialBody CelestialBody { get; set; } //The base CelestialBody that represents this OAPP (planet, moon, star, super star, grand super star, etc).
        //public bool IsPublished { get; set; }

        //TODO:More to come! ;-)
    }
}
