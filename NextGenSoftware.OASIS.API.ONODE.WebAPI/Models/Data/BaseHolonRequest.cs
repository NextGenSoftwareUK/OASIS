
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public abstract class BaseHolonRequest : OASISRequest
    {
        public bool Recursive { get; set; } = true;
        public int MaxChildDepth { get; set; } = 0;
        public bool ContinueOnError { get; set; } = true;
        public bool LoadChildrenFromProvider { get; set; } = false;
        public string ChildHolonType { get; set; } = "All";
        public int Version { get; set; } = 0;
    }
}