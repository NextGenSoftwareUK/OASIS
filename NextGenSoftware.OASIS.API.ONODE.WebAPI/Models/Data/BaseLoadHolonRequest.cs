
namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Models.Data
{
    public abstract class BaseLoadHolonRequest : BaseHolonRequest
    {
        public bool LoadChildren { get; set; } = true;
    }
}