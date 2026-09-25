
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class LoadAllHolonsRequest : BaseLoadHolonRequest
    {
        public string HolonType { get; set; }
        /// <summary>When true also returns holons from other avatars that are explicitly marked IsPublic=true. Defaults to false.</summary>
        public bool IncludePublic { get; set; } = false;
    }
}