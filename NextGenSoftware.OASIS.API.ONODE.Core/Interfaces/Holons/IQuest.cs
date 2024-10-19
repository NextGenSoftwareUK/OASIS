using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IQuest : ITaskBase
    {
        Guid ParentMissionId { get; set; }
        Guid ParentQuestId { get; set; }
        QuestType QuestType { get; set; }
        int Order { get; set; } //The order that the quest's appear and need to be completed in (stages). Each stage/sub-quest can have 1 or more nfts and/or 1 or more hotspots assigned. Once they are all collected/visited/completed then that sub-quest is complete. Once all sub-quests are complete then the parent quest is complete and so on. Once all quests are complete then the mission is complete.
        IList<IQuest> SubQuests { get; set; }
        IList<string> GeoSpatialNFTIds { get; set; }
        IList<IOASISGeoSpatialNFT> GeoSpatialNFTs { get; set; }
        IList<string> GeoHotSpotIds { get; set; }
        IList<IGeoHotSpot> GeoHotSpots { get; set; }
    }
}