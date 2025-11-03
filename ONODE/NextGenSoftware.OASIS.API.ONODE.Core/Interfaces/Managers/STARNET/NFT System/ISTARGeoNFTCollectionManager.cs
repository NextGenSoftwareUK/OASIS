using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISTARGeoNFTCollectionManager : ISTARNETManagerBase<STARGeoNFTCollection, DownloadedGeoNFTCollection, InstalledGeoNFTCollection, STARNETDNA>
    {
        // Geo NFT Collection specific methods can be added here
    }
}
