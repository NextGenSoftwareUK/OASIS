using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.ORM.Collections;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HcAvatarDetailCollection : HoloNETCollection<HcAvatarDetail>
    {
        public HcAvatarDetailCollection() : base("oasis", "get_all_avatar_details", "add_avatar_detail", "remove_avatar_detail", "batch_update_avatar_details", true, new HoloNETDNA() { AutoStartHolochainConductor = false, AutoShutdownHolochainConductor = false }) { }
        public HcAvatarDetailCollection(HoloNETClientAppAgent holoNETClient) : base("oasis", "get_all_avatar_details", "add_avatar_detail", "remove_avatar_detail", holoNETClient, "batch_update_avatar_details") { }
    }
}