using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IQuest : ITaskBase
    {
        Guid MissionId { get; set; }
        QuestType QuestType { get; set; }
        IEnumerable<IQuest> SubQuests { get; set; }
        IEnumerable<string> GeoSpatialNFTIds { get; set; }
        IEnumerable<IOASISGeoSpatialNFT> GeoSpatialNFTs { get; set; }
        IEnumerable<string> GetHotSpotIds { get; set; }
        IEnumerable<IGeoHotSpot> GetHotSpots { get; set; }
    }
}