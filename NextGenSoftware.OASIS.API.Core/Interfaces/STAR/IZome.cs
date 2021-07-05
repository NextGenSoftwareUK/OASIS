

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IZome : IZomeBase
    {
       // HoloNETClientBase HoloNETClient { get; }
        //  string ZomeName { get; set; }
        //List<Holon> Holons { get; set; }

        //TODO: Come back to this, these are currently in HoloNETClient.
       // event Events.DataReceived OnDataReceived; //TODO: May rename to OnSynapseFired ?
       // event Events.Disconnected OnDisconnected;
        //event Events.HolonLoaded OnHolonLoaded;
        //event Events.HolonSaved OnHolonSaved;
        //event Events.Initialized OnInitialized;
        //event Events.ZomeError OnZomeError;

        //Task Initialize(string zomeName, HoloNETClientBase holoNETClient);
        //Task Initialize(string zomeName, string holochainConductorURI, HoloNETClientType type);













        //Task<IHolon> LoadHolonAsync(string providerKey, HolonType type = HolonType.Holon);
        //Task<IHolon> LoadHolonAsync(Guid id, HolonType type = HolonType.Holon);
        //Task<IHolon> SaveHolonAsync(IHolon holon);
    }
}