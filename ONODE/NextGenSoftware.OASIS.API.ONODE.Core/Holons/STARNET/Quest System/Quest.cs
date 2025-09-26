using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class Quest : QuestBase, IQuest
    {
        public Quest() : base("QuestDNAJSON")
        {
            this.HolonType = HolonType.Quest;
        }

        [CustomOASISProperty()]
        public Guid ParentQuestId { get; set; }

        [CustomOASISProperty()]
        public QuestType QuestType { get; set; }

        //[CustomOASISProperty()]
        //public IList<IOASISGeoSpatialNFT> GeoSpatialNFTs { get; set; }

        //[CustomOASISProperty()]
        //public IList<string> GeoSpatialNFTIds { get; set; }

        //[CustomOASISProperty()]
        //public IList<string> GeoHotSpotIds { get; set; }

        //[CustomOASISProperty()]
        //public IList<IGeoHotSpot> GeoHotSpots { get; set; }
    }
}