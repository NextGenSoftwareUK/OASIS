using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IQuest : IQuestBase
    {
        Guid ParentMissionId { get; set; }
        Guid ParentQuestId { get; set; }
        QuestType QuestType { get; set; }
        //IList<string> GeoSpatialNFTIds { get; set; }
        //IList<IOASISGeoSpatialNFT> GeoSpatialNFTs { get; set; }
        //IList<string> GeoHotSpotIds { get; set; }
        //IList<IGeoHotSpot> GeoHotSpots { get; set; }
        IQuest CurrentSubQuest { get; }
        int CurrentSubQuestNumber { get; }
        string Status { get; }
    }
}