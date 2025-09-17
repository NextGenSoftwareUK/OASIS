
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public abstract class BaseLoadHolonRequest : BaseHolonRequest
    {
        public bool LoadChildren { get; set; } = true;
    }
}