//using System;
//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
//using NextGenSoftware.OASIS.API.ONode.Core.Enums;

//namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Objects
//{
//    public interface IOAPPDNA
//    {
//        ICelestialBody CelestialBody { get; set; } //optional
//        Guid CelestialBodyId { get; set; }
//        string CelestialBodyName { get; set; }
//        HolonType CelestialBodyType { get; set; }
//        IEnumerable<IZome> Zomes { get; set; }
//        Guid CreatedByAvatarId { get; set; }
//        DateTime CreatedOn { get; set; }
//        string Description { get; set; }
//        GenesisType GenesisType { get; set; }
//        Guid OAPPId { get; set; }
//        string OAPPName { get; set; }
//        OAPPType OAPPType { get; set; }
//        Guid PublishedByAvatarId { get; set; }
//        DateTime PublishedOn { get; set; }
//        bool PublishedOnSTARNET { get; set; }
//        string Version { get; set; }
//    }
//}