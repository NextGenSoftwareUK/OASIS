using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.STAR
{
    public interface IZome : OASIS.API.Core.IZome
    {
        HoloNETClientBase HoloNETClient { get; }
        //  string ZomeName { get; set; }
        List<Holon> Holons { get; set; }

        //event ZomeBase.DataReceived OnDataReceived; //TODO: May rename to OnSynapseFired ?
        //event ZomeBase.Disconnected OnDisconnected;
        //event ZomeBase.HolonLoaded OnHolonLoaded;
        //event ZomeBase.HolonSaved OnHolonSaved;
        //event ZomeBase.Initialized OnInitialized;
        //event ZomeBase.ZomeError OnZomeError;

        Task Initialize(string zomeName, HoloNETClientBase holoNETClient);
        Task Initialize(string zomeName, string holochainConductorURI, HoloNETClientType type);
        //Task<IHolon> LoadHolonAsync(string holonType, string hcEntryAddressHash);
        //Task<IHolon> SaveHolonAsync(string holonType, IHolon hcObject);
    }
}