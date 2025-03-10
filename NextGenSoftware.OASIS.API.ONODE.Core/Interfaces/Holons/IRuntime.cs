using NextGenSoftware.OASIS.API.ONode.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IRuntime
    {
        byte[] PublishedRuntime { get; set; }
        IRuntimeDNA RuntimeDNA { get; set; }
    }
}