using NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IMissionData : IHolon
    {
        public List<IMission> Missions { get; set; }
    }
}