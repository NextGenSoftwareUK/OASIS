using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons.STARNET;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET
{
    public class ExternalDependency : IExternalDependency
    {
        public ExternalDependencyType ExternalDependencyType { get; set; }
        public string Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string URI { get; set; }
    }
}