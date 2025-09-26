using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.ORM.Collections;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HcAvatarCollection : HoloNETCollection<HcAvatar>
    {
        public HcAvatarCollection() : base("oasis", "get_all_avatars", "add_avatar", "remove_avatar", "batch_update_avatars", true, new HoloNETDNA() { AutoStartHolochainConductor = false, AutoShutdownHolochainConductor = false }) { }
        public HcAvatarCollection(HoloNETClientAppAgent holoNETClient) : base("oasis", "get_all_avatars", "add_avatar", "remove_avatar", holoNETClient, "batch_update_avatars") { }
    }
}