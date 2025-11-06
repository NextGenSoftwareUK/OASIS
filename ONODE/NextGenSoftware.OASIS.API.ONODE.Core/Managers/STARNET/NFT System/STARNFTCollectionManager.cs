using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class STARNFTCollectionManager : STARNETManagerBase<STARNFTCollection, DownloadedNFTCollection, InstalledNFTCollection, STARNETDNA>, ISTARNFTCollectionManager
    {
        public STARNFTCollectionManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(NFTType),
            HolonType.STARNFTCollection,
            HolonType.InstalledNFTCollection,
            "NFT Collection",
            //"NFTCollectionId",
            "STARNETHolonId",
            "NFTCollectionName",
            "NFTCollectionType",
            "onftcollection",
            "oasis_nftcollections",
            "NFTCollectionDNA.json",
            "NFTCollectionDNAJSON")
        { }

        public STARNFTCollectionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(STARNFTCollection),
            HolonType.STARNFTCollection,
            HolonType.InstalledNFTCollection,
            "NFTCollection",
            //"NFTCollectionId",
            "STARNETHolonId",
            "NFTCollectionName",
            "NFTCollectionType",
            "onftcollection",
            "oasis_nftcollections",
            "NFTCollectionDNA.json",
            "NFTCollectionDNAJSON")
        { }
    }
}