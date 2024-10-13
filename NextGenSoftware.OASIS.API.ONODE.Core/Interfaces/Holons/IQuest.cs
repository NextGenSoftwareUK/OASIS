using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IQuest
    {
        IEnumerable<string> GeoSpatialNFTIds { get; set; }
        IEnumerable<IOASISGeoSpatialNFT> GeoSpatialNFTs { get; set; }
        IEnumerable<string> GetHotSpotIds { get; set; }
        IEnumerable<IGeoHotSpot> GetHotSpots { get; set; }
        Guid StartedBy { get; set; }
        DateTime StartedOn { get; set; }
        Guid CompletedBy { get; set; }
        DateTime CompletedOn { get; set; }
        IList<IQuest> SubQuests { get; set; }
    }
}