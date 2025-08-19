﻿using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class CelestialBodyMetaDataDNAManager : STARNETManagerBase<CelestialBodyMetaDataDNA, DownloadedCelestialBodyMetaDataDNA, InstalledCelestialBodyMetaDataDNA, STARNETDNA>
    {
        public CelestialBodyMetaDataDNAManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(CelestialBodyType),
            HolonType.CelestialBodyMetaDataDNA,
            HolonType.InstalledCelestialBodyMetaDataDNA,
            "CelestialBody MetaData DNA",
            //"CelestialBodyMetaDataId",
            "STARNETHolonId",
            "CelestialBodyMetaDataName",
            "CelestialBodyMetaDataType",
            "ocelestialbodymetadata",
            "oasis_celestialbodiesmetadata",
            "CelestialBodyMetaDataDNA.json",
            "CelestialBodyMetaDataDNAJSON")
        { }

        public CelestialBodyMetaDataDNAManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(CelestialBodyType),
            HolonType.CelestialBodyMetaDataDNA,
            HolonType.InstalledCelestialBodyMetaDataDNA,
            "CelestialBody MetaData DNA",
            //"CelestialBodyMetaDataId",
            "STARNETHolonId",
            "CelestialBodyMetaDataName",
            "CelestialBodyMetaDataType",
            "ocelestialbodymetadata",
            "oasis_celestialbodiesmetadata",
            "CelestialBodyMetaDataDNA.json",
            "CelestialBodyMetaDataDNAJSON")
        { }
    }
}