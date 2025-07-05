using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class ZomeMetaDataDNAManager : STARNETManagerBase<ZomeMetaDataDNA, DownloadedZomeMetaDataDNA, InstalledZomeMetaDataDNA>
    {
        public ZomeMetaDataDNAManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(ZomeType),
            HolonType.ZomeMetaDataDNA,
            HolonType.InstalledSTARNETHolon,
            "Zome MetaData DNA",
            "ZomeMetaDataId",
            "ZomeMetaDataName",
            "ZomeMetaDataType",
            "ozomemetadata",
            "oasis_zomessmetadata",
            "ZomeMetaDataDNA.json",
            "ZomeMetaDataDNAJSON")
        { }

        public ZomeMetaDataDNAManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(ZomeType),
            HolonType.ZomeMetaDataDNA,
            HolonType.InstalledSTARNETHolon,
            "Zome MetaData DNA",
            "ZomeMetaDataId",
            "ZomeMetaDataName",
            "ZomeMetaDataType",
            "ozomemetadata",
            "oasis_zomessmetadata",
            "ZomeMetaDataDNA.json",
            "ZomeMetaDataDNAJSON")
        { }
    }
}