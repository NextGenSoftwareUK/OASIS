using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class HolonMetaDataDNAManager : STARNETManagerBase<HolonMetaDataDNA, DownloadedHolonMetaDataDNA, InstalledHolonMetaDataDNA, STARNETDNA>
    {
        public HolonMetaDataDNAManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(STARHolonType),
            HolonType.HolonMetaDataDNA,
            HolonType.InstalledHolonMetaDataDNA,
            "Holon MetaData DNA",
            //"HolonMetaDataId",
            "STARNETHolonId",
            "HolonMetaDataName",
            "HolonMetaDataType",
            "oholonmetadata",
            "oasis_holonsmetadata",
            "HolonMetaDataDNA.json",
            "HolonMetaDataDNAJSON")
        { }

        public HolonMetaDataDNAManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(STARHolonType),
            HolonType.HolonMetaDataDNA,
            HolonType.InstalledHolonMetaDataDNA,
            "Holon MetaData DNA",
            //"HolonMetaDataId",
            "STARNETHolonId",
            "HolonMetaDataName",
            "HolonMetaDataType",
            "oholonmetadata",
            "oasis_holonsmetadata",
            "HolonMetaDataDNA.json",
            "HolonMetaDataDNAJSON")
        { }
    }
}