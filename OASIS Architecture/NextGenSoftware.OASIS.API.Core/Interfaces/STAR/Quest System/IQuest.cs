using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
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
    }
}