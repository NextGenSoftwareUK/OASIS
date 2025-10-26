using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

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