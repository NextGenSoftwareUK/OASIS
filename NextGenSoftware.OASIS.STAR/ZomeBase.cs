using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core;

namespace NextGenSoftware.OASIS.STAR
{
    public abstract class ZomeBase: Holon, IZome
    {
        private HolonManager _holonManager = new HolonManager(OASISProviderManager.GetAndActivateProvider());
        public List<Holon> _holons = new List<Holon>();
        
        public List<Holon> Holons
        {
            get
            {
                return _holons;
            }
            set
            {
                _holons = value;
            }
        }

        public delegate void HolonSaved(object sender, HolonSavedEventArgs e);
        public event HolonSaved OnHolonSaved;

        public delegate void HolonLoaded(object sender, HolonLoadedEventArgs e);
        public event HolonLoaded OnHolonLoaded;

        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        public event HolonsLoaded OnHolonsLoaded;

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void ZomeError(object sender, ZomeErrorEventArgs e);
        public event ZomeError OnZomeError;

        ////TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        //public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        //public event Disconnected OnDisconnected;

        //public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //public event DataReceived OnDataReceived;
    
        public virtual async Task<IHolon> LoadHolonAsync(Guid id, HolonType type = HolonType.Holon)
        {
            return await _holonManager.LoadHolonAsync(id, type);
        }

        public virtual async Task<IHolon> LoadHolonAsync(string providerKey, HolonType type = HolonType.Holon)
        {
            return await _holonManager.LoadHolonAsync(providerKey, type);
        }

        public virtual async Task<IEnumerable<IHolon>> LoadHolonsAsync(Guid id, HolonType type = HolonType.Holon)
        {
            return await _holonManager.LoadHolonsAsync(id, type);
        }

        public virtual async Task<IEnumerable<IHolon>> LoadHolonsAsync(string providerKey, HolonType type = HolonType.Holon)
        {
            return await _holonManager.LoadHolonsAsync(providerKey, type);
        }

        public virtual async Task<IHolon> SaveHolonAsync(IHolon savingHolon)
        {
            return await _holonManager.SaveHolonAsync(savingHolon);
        }

        public virtual async Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons)
        {
            return await _holonManager.SaveHolonsAsync(savingHolons);
        }

    }
}
