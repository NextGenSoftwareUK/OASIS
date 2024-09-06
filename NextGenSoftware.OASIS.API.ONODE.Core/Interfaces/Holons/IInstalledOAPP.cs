using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Objects;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons
{
    public interface IInstalledOAPP : IHolon
    {
        IOAPPDNA OAPPDNA { get; set; }
        //IOAPP OAPP { get; set; }
        //Guid OAPPId { get; set; }
    }
}