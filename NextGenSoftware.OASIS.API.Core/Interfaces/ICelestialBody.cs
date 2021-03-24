
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface ICelestialBody : IHolon
    {
        //HoloNETClientBase HoloNETClient { get; }


        //TODO: Come back to this...
        /*
        //  event DataReceived OnDataReceived;
        // event Disconnected OnDisconnected;
        event Events.HolonLoaded OnHolonLoaded;
        event Events.HolonLoaded OnHolonsLoaded;
        event Events.HolonSaved OnHolonSaved;
        event Events.Initialized OnInitialized;
        event Events.ZomeError OnZomeError;
        event Events.ZomesLoaded OnZomesLoaded;
        */

        //List<IZome> Zomes { get; set; }
      //  List<IHolon> Holons { get; }

        void Dim();
        void Emit();
        void Evolve();
        //Task Initialize(Guid id, HoloNETClientBase holoNETClient);
        //Task Initialize(Guid id, string holochainConductorURI, PlanetBase.HoloNETClientType type);
        //Task Initialize(HoloNETClientBase holoNETClient);
        //Task Initialize(string holochainConductorURI, PlanetBase.HoloNETClientType type);
        CoronalEjection Flare();

        void LoadAll();
        void LoadZomes();
        //void LoadHolons();


        //Task<IHolon> LoadHolonAsync(string rustHolonType, string providerKey);

        //TODO: Come back to this...  we dont want to include rust in this base interface but need way to hide this method in sub-interfaces somehow...
        // abstract Task<IHolon> LoadHolonAsync(string providerKey);
        void Love();
        void Mutate();
        void Radiate();
        void Reflect();
        Task<bool> Save();
        //Task<IHolon> SaveHolonAsync(string rustHolonType, IHolon savingHolon);
        //abstract Task<IHolon> SaveHolonAsync(IHolon savingHolon);
        void Seed();
        void Shine();
        void Super();
        void Twinkle();
    }
}