
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data
{
    public class LoadAllHolonsRequest : BaseLoadHolonRequest
    {
        public string HolonType { get; set; }
        /// <summary>When true only returns holons owned by the current avatar. Defaults to true.</summary>
        public bool SearchOnlyForCurrentAvatar { get; set; } = true;
        /// <summary>When SearchOnlyForCurrentAvatar is false, also include holons from other avatars that are explicitly marked IsPublic=true.</summary>
        public bool IncludePublic { get; set; } = true;
    }
}