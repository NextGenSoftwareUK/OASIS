using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.ORM.Collections;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public class HcHolonCollection : HoloNETCollection<HcHolon>
    {
        public HcHolonCollection() : base("oasis", "get_all_holons", "add_holon", "remove_holon", "batch_update_holons", true, new HoloNETDNA() { AutoStartHolochainConductor = false, AutoShutdownHolochainConductor = false }) { }
        public HcHolonCollection(HoloNETClientAppAgent holoNETClient) : base("oasis", "get_all_holons", "add_holon", "remove_holon", holoNETClient, "batch_update_holons") { }
    }
}