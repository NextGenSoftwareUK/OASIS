using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces
{
    public interface IMissionData : IHolon
    {
        List<Mission> Missions { get; set; }
    }
}