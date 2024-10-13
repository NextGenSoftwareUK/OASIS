using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class Quest : Holon, IQuest
    {
        public Quest()
        {
            this.HolonType = HolonType.Quest;
        }

        public IEnumerable<IOASISGeoSpatialNFT> GeoSpatialNFTs { get; set; }
        public IEnumerable<string> GeoSpatialNFTIds { get; set; }
        public IEnumerable<IGeoHotSpot> GetHotSpots { get; set; }
        public IEnumerable<string> GetHotSpotIds { get; set; }

        public DateTime StartedOn { get; set; }
        public Guid StartedBy { get; set; }
        public DateTime CompletedOn { get; set; }
        public Guid CompletedBy { get; set; }
        public IList<IQuest> SubQuests { get; set; }
    }
}