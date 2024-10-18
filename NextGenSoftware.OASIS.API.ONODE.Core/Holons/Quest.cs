using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class Quest : Holon, IQuest
    {
        public Quest()
        {
            this.HolonType = HolonType.Quest;
        }

        [CustomOASISProperty()]
        public Guid ParentQuestId { get; set; }

        [CustomOASISProperty()]
        public Guid ParentMissionId { get; set; }

        [CustomOASISProperty()]
        public QuestType QuestType { get; set; }

        [CustomOASISProperty()]
        public int Order { get; set; } //The order that the quest's appear and need to be completed in (stages). Each stage/sub-quest can have 1 or more nfts and/or 1 or more hotspots assigned. Once they are all collected/visited/completed then that sub-quest is complete. Once all sub-quests are complete then the parent quest is complete and so on. Once all quests are complete then the mission is complete.

        [CustomOASISProperty()]
        public DateTime StartedOn { get; set; }

        [CustomOASISProperty()]
        public Guid StartedBy { get; set; }

        [CustomOASISProperty()]
        public DateTime CompletedOn { get; set; }

        [CustomOASISProperty()]
        public Guid CompletedBy { get; set; }

        [CustomOASISProperty()]
        public IList<IOASISGeoSpatialNFT> GeoSpatialNFTs { get; set; }

        [CustomOASISProperty()]
        public IList<string> GeoSpatialNFTIds { get; set; }

        [CustomOASISProperty()]
        public IList<IGeoHotSpot> GetHotSpots { get; set; }

        [CustomOASISProperty()]
        public IList<string> GetHotSpotIds { get; set; }

        [CustomOASISProperty()]
        public IList<IQuest> SubQuests { get; set; }
    }
}