using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IRuntime : IHolon
    {
        byte[] PublishedRuntime { get; set; }
        IRuntimeDNA RuntimeDNA { get; set; }
    }
}