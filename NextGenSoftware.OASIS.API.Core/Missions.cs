using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core
{
    public class MissionData : IMissionData
    {
        public List<Mission> Missions { get; set; } = new List<Mission>();
    }
}