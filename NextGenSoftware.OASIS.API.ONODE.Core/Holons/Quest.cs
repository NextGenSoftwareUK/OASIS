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
        public Guid MissionId { get; set; }

        [CustomOASISProperty()]
        public QuestType QuestType { get; set; }

        [CustomOASISProperty()]
        public DateTime StartedOn { get; set; }

        [CustomOASISProperty()]
        public Guid StartedBy { get; set; }

        [CustomOASISProperty()]
        public DateTime CompletedOn { get; set; }

        [CustomOASISProperty()]
        public Guid CompletedBy { get; set; }

        [CustomOASISProperty()]
        public IEnumerable<IOASISGeoSpatialNFT> GeoSpatialNFTs { get; set; }

        [CustomOASISProperty()]
        public IEnumerable<string> GeoSpatialNFTIds { get; set; }

        [CustomOASISProperty()]
        public IEnumerable<IGeoHotSpot> GetHotSpots { get; set; }

        [CustomOASISProperty()]
        public IEnumerable<string> GetHotSpotIds { get; set; }

        [CustomOASISProperty()]
        public IEnumerable<IQuest> SubQuests { get; set; }
    }
}