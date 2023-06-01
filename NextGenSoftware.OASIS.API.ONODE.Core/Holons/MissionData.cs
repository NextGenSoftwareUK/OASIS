using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class MissionData : Holon, IMissionData
    {
        public List<IMission> Missions { get; set; } = new List<IMission>();
    }
}