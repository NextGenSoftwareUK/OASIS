using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public class HolonDNA
    {
        public IHolon FromHolon { get; set; }
        public IHolon ToHolon { get; set; }
    }
}