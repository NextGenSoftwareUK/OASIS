using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class STARGeoNFTCollectionManager : STARNETManagerBase<STARGeoNFTCollection, DownloadedGeoNFTCollection, InstalledGeoNFTCollection, STARNETDNA>, ISTARGeoNFTCollectionManager
    {
        public STARGeoNFTCollectionManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(NFTType),
            HolonType.STARGeoNFTCollection,
            HolonType.InstalledGeoNFTCollection,
            "GeoNFTCollection",
            //"NFTCollectionId",
            "STARNETHolonId",
            "GeoNFTCollectionName",
            "GeoNFTCollectionType",
            "ogeonftcollection",
            "oasis_geonftcollections",
            "GeoNFTCollectionDNA.json",
            "GeoNFTCollectionDNAJSON")
        { }

        public STARGeoNFTCollectionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(STARGeoNFTCollection),
            HolonType.STARGeoNFTCollection,
            HolonType.InstalledGeoNFTCollection,
            "GeoNFTCollection",
            //"NFTCollectionId",
            "STARNETHolonId",
            "GeoNFTCollectionName",
            "GeoNFTCollectionType",
            "ogeonftcollection",
            "oasis_geonftcollections",
            "GeoNFTCollectionDNA.json",
            "GeoNFTCollectionDNAJSON")
        { }
    }
}