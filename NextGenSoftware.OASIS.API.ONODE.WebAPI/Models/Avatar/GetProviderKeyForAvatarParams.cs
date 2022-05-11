
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    public class GetProviderKeyForAvatarParams
    {
        public string AvatarID { get; set; }
        public string AvatarUsername { get; set; }
        public string AvatarEmail { get; set; } //TODO: Finish implementing email versions of all Key Methods...
        public string ProviderType { get; set; }
        //public string ProviderTypeToLoadAvatarFrom { get; set; }
    }
}