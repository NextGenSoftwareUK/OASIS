using NextGenSoftware.OASIS.API.Core.Holons;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IMissionData 
    {
        List<Mission> Missions { get; set; }
    }
}