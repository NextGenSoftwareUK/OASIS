
namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models.Data
{
    public abstract class BaseHolonRequest : OASISRequest
    {
        public bool Recursive { get; set; } = true;
        public int MaxChildDepth { get; set; } = 0;
        public bool ContinueOnError { get; set; } = true;
        public int Version { get; set; } = 0;
    }
}