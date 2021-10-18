using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces
{
    public interface IMissionData 
    {
        List<Mission> Missions { get; set; }
    }
}