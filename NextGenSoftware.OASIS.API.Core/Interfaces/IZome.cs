
using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface IZome : IHolon
    {
       // HoloNETClientBase HoloNETClient { get; }
        //  string ZomeName { get; set; }
        List<Holon> Holons { get; set; }

        //TODO: Come back to this, these are currently in HoloNETClient.
       // event Events.DataReceived OnDataReceived; //TODO: May rename to OnSynapseFired ?
       // event Events.Disconnected OnDisconnected;
        event Events.HolonLoaded OnHolonLoaded;
        event Events.HolonSaved OnHolonSaved;
        event Events.Initialized OnInitialized;
        event Events.ZomeError OnZomeError;

        //Task Initialize(string zomeName, HoloNETClientBase holoNETClient);
        //Task Initialize(string zomeName, string holochainConductorURI, HoloNETClientType type);
        Task<IHolon> LoadHolonAsync(string holonType, string providerKey);
        Task<IHolon> SaveHolonAsync(string holonType, IHolon holon);
    }
}