using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class MissionData : IMissionData
    {
        public List<Mission> Missions { get; set; } = new List<Mission>();
    }
}