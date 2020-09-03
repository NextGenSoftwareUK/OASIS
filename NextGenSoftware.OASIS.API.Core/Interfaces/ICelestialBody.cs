
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface ICelestialBody : IHolon
    {
        //HoloNETClientBase HoloNETClient { get; }

        //event PlanetBase.DataReceived OnDataReceived;
        //event PlanetBase.Disconnected OnDisconnected;
        //event PlanetBase.HolonLoaded OnHolonLoaded;
        //event PlanetBase.HolonSaved OnHolonSaved;
        //event PlanetBase.Initialized OnInitialized;
        //event PlanetBase.ZomeError OnZomeError;

        List<IZome> Zomes { get; set; }
        List<IHolon> Holons { get; set; }

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
        void LoadHolons();


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