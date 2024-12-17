using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
{
    public interface IOAPPTemplate : IPublishableHolon
    {
       public OAPPTemplateType OAPPTemplateType { get; set; }
    }
}