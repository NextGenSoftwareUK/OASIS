
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IExternalDependency
    {
        string Description { get; set; }
        ExternalDependencyType ExternalDependencyType { get; set; }
        string Id { get; set; }
        string Key { get; set; }
        string Name { get; set; }
        string URI { get; set; }
    }
}