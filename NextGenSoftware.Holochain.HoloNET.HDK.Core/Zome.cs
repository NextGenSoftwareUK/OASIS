
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class Zome : ZomeBase, IZome
    {
        private const string HOLONS_LOAD_ALL = "_holons_loadall";
        private const string HOLONS_ADD = "_holons_add";
        private const string HOLONS_REMOVE = "_holons_remove";

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public event HolonsLoaded OnHolonsLoaded;

        public Zome(HoloNETClientBase holoNETClient, string zomeName) : base(holoNETClient, zomeName)
        {
            this.Name = zomeName;
        }

        public Zome(string holochainConductorURI, HoloNETClientType type, string zomeName) : base(holochainConductorURI, zomeName, type)
        {
            this.Name = zomeName;
        }

        public async Task<IHolon> AddHolon(IZome zome)
        {
            return await base.SaveHolonAsync(string.Concat(this.Name, HOLONS_ADD), zome);
        }

        public async Task<IHolon> RemoveHolon(IZome zome)
        {
            return await base.SaveHolonAsync(string.Concat(this.Name, HOLONS_REMOVE), zome);
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
            foreach (IHolon holon in base.LoadHolonsAsync(string.Concat(this.Name, HOLONS_LOAD_ALL), ProviderKey).Result)
            {
                holons.Add((IZome)holon);
              //  coreHolons.Add((OASIS.API.Core.IZome)holon);
            }

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs { Holons = holons });

            //TODO: Make this return a Task so is awaitable...
            return holons;
        }

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
