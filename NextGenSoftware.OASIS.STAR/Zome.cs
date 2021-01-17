
using System;

namespace NextGenSoftware.OASIS.STAR
{
    // TODO: What was the point of this class?! Need to remember! ;-) lol
    public class Zome : ZomeBase, IZome
    {
        //private const string HOLONS_LOAD_ALL = "_holons_loadall";
        //private const string HOLONS_ADD = "_holons_add";
        //private const string HOLONS_REMOVE = "_holons_remove";

        // public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        //public event HolonsLoaded OnHolonsLoaded;


        public Zome() : base()
        {

        }

        public Zome(Guid id) : base()
        {
            this.Id = id;
        }

        public Zome(string providerKey) : base()
        {
            this.ProviderKey = providerKey;
        }



        /*
        public Zome(HoloNETClientBase holoNETClient, string zomeName) : base(holoNETClient, zomeName)
        {
            this.Name = zomeName;
        }

        public Zome(string holochainConductorURI, HoloNETClientType type, string zomeName) : base(holochainConductorURI, zomeName, type)
        {
            this.Name = zomeName;
        }*/


        /*
        public async Task<IEnumerable<IHolon>> AddHolon(IHolon holon)
        {
            //return await base.SaveHolonAsync(string.Concat(this.Name, HOLONS_ADD), zome);
            this.Holons.Add((Holon)holon);
            return await base.SaveHolonsAsync(this.Holons);
        }

        public async Task<IEnumerable<IHolon>> RemoveHolon(IHolon holon)
        {
            //return await base.SaveHolonAsync(string.Concat(this.Name, HOLONS_REMOVE), zome);
            this.Holons.Remove((Holon)holon);
            return await base.SaveHolonsAsync(this.Holons);
        }

        public async Task<List<IHolon>> LoadHolons()
        {
            //TODO: Finish
            if (string.IsNullOrEmpty(ProviderKey))
                throw new ArgumentNullException("ProviderKey", "The ProviderKey must be set before this method can be called.");

            //TODO: Check to see if the method awaits till the zomes(holons) are loaded before returning (if it doesn't need to refacoring to subscribe to events like LoadHolons does)
            List<IHolon> holons = new List<IHolon>();
            //  List<OASIS.API.Core.IHolon> coreHolons = new List<OASIS.API.Core.IHolon>();

            //TODO: Come back to this, must be better way of doing this?
            //foreach (IHolon holon in base.LoadHolonsAsync(string.Concat(this.Name, HOLONS_LOAD_ALL), ProviderKey).Result)
            foreach (IHolon holon in base.LoadHolonsAsync(ProviderKey).Result)
            {
                holons.Add((IZome)holon);
              //  coreHolons.Add((OASIS.API.Core.IZome)holon);
            }

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs { Holons = holons });

            //TODO: Make this return a Task so is awaitable...
            return holons;
        }
        */


        //public async Task<IHolon> LoadHOLONAsync(string hcEntryAddressHash)
        //{
        //    return await base.LoadHolonAsync("{holon}", hcEntryAddressHash);
        //}

        //public async Task<IHolon> SaveHOLONAsync(IHolon holon)
        //{
        //    //return await base.SaveHolonAsync("{holon}", holon);
        //    return await base.SaveHolonAsync(holon);
        //}
    }
}
