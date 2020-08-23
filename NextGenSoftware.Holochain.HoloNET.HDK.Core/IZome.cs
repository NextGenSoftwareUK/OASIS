using NextGenSoftware.Holochain.HoloNET.Client.Core;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public interface IZome
    {
        HoloNETClientBase HoloNETClient { get; }
        string ZomeName { get; set; }

        event ZomeBase.DataReceived OnDataReceived; //TODO: May rename to OnSynapseFired ?
        event ZomeBase.Disconnected OnDisconnected;
        event ZomeBase.HolonLoaded OnHolonLoaded;
        event ZomeBase.HolonSaved OnHolonSaved;
        event ZomeBase.Initialized OnInitialized;
        event ZomeBase.ZomeError OnZomeError;

        Task Initialize(string zomeName, HoloNETClientBase holoNETClient);
        Task Initialize(string zomeName, string holochainConductorURI, ZomeBase.HoloNETClientType type);
        Task<IHolon> LoadHolonAsync(string holonName, string hcEntryAddressHash);
        Task<IHolon> SaveHolonAsync(string holochainDataObjectName, IHolon hcObject);
    }
}