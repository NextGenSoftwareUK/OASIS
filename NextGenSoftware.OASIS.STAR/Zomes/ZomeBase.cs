using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.STAR.Zomes
{
    public abstract class ZomeBase : Holon, IZomeBase
    {
        private HolonManager _holonManager = null;
        private const string CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET = "Both Id and ProviderUniqueStorageKey are null, one of these need to be set before calling this method.";
        private Dictionary<Guid, IOmiverse> _parentOmiverse = new Dictionary<Guid, IOmiverse>();
        private Dictionary<Guid, IDimension> _parentDimension = new Dictionary<Guid, IDimension>();
        private Dictionary<Guid, IMultiverse> _parentMultiverse = new Dictionary<Guid, IMultiverse>();
        private Dictionary<Guid, IUniverse> _parentUniverse = new Dictionary<Guid, IUniverse>();
        private Dictionary<Guid, IGalaxyCluster> _parentGalaxyCluster = new Dictionary<Guid, IGalaxyCluster>();
        private Dictionary<Guid, IGalaxy> _parentGalaxy = new Dictionary<Guid, IGalaxy>();
        private Dictionary<Guid, ISolarSystem> _parentSolarSystem = new Dictionary<Guid, ISolarSystem>();
        private Dictionary<Guid, IGreatGrandSuperStar> _parentGreatGrandSuperStar = new Dictionary<Guid, IGreatGrandSuperStar>();
        private Dictionary<Guid, IGrandSuperStar> _parentGrandSuperStar = new Dictionary<Guid, IGrandSuperStar>();
        private Dictionary<Guid, ISuperStar> _parentSuperStar = new Dictionary<Guid, ISuperStar>();
        private Dictionary<Guid, IStar> _parentStar = new Dictionary<Guid, IStar>();
        private Dictionary<Guid, IPlanet> _parentPlanet = new Dictionary<Guid, IPlanet>();
        private Dictionary<Guid, IMoon> _parentMoon = new Dictionary<Guid, IMoon>();
        private Dictionary<Guid, ICelestialSpace> _parentCelestialSpace = new Dictionary<Guid, ICelestialSpace>();
        private Dictionary<Guid, ICelestialBody> _parentCelestialBody = new Dictionary<Guid, ICelestialBody>();
        private Dictionary<Guid, IZome> _parentZome = new Dictionary<Guid, IZome>();
        private Dictionary<Guid, IHolon> _parentHolon = new Dictionary<Guid, IHolon>();
        private Dictionary<Guid, ICelestialBodyCore> _core = new Dictionary<Guid, ICelestialBodyCore>();

        public List<IHolon> _holons = new List<IHolon>();

        public List<IHolon> Holons
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

        public event Events.Initialized OnInitialized;
        public event Events.HolonLoaded OnHolonLoaded;
        //public event Events.HolonLoadedGeneric<T> OnHolonLoaded2;
        public event Events.HolonsLoaded OnHolonsLoaded;
        public event Events.HolonSaved OnHolonSaved;
       // public event Events.HolonSaved<T> OnHolonSaved;
        public event Events.HolonsSaved OnHolonsSaved;
        public event Events.ZomeSaved OnSaved;
        public event Events.HolonAdded OnHolonAdded;
        public event Events.HolonRemoved OnHolonRemoved;
        //public event Events.ZomesLoaded OnZomesLoaded;
        public event Events.ZomeError OnZomeError;

        ////TODO: Not sure if we want to expose the HoloNETClient events at this level? They can subscribe to them through the HoloNETClient property below...
        //public delegate void Disconnected(object sender, DisconnectedEventArgs e);
        //public event Disconnected OnDisconnected;
        //public delegate void DataReceived(object sender, DataReceivedEventArgs e);
        //public event DataReceived OnDataReceived;

        public ZomeBase(Guid id) : base(id)
        {
            Init();
        }

        public ZomeBase(string providerKey, ProviderType providerType) : base(providerKey, providerType)
        {
            Init();
        }

        //public ZomeBase(Dictionary<ProviderType, string> providerKeys) : base(providerKeys)
        //{
        //    Init();
        //}

        public ZomeBase()
        {
            Init();
        }

        private void Init()
        {
            _holonManager = HolonManager.Instance;
            OnInitialized?.Invoke(this, new System.EventArgs());

            //OASISResult<IOASISStorageProvider> result = OASISBootLoader.OASISBootLoader.GetAndActivateDefaultProvider();

            ////TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
            //if (result.IsError)
            //{
            //    string errorMessage = string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message);
            //    ErrorHandling.HandleError(ref result, errorMessage, true, false, true);
            //    OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = errorMessage });
            //}
            //else
            //{
            //    //TODO: Mot sure if want each Zome to have its own instance of HolonManager? Have to think about possible use cases for this?
            //    _holonManager = HolonManager.Instance;
            //    //_holonManager = new HolonManager(result.Result); //TODO: Change to use static instance instead!
            //    OnInitialized?.Invoke(this, new System.EventArgs());
            //}
        }

        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = await _holonManager.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = _holonManager.LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with id ", id, ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<T>> LoadHolonAsync<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual OASISResult<T> LoadHolon<T>(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(LoadHolon(id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = await _holonManager.LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonAsync method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IHolon> result = _holonManager.LoadHolon(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolon method with providerKey ", providerKey, ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonLoaded?.Invoke(this, new HolonLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<T>> LoadHolonAsync<T>(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await LoadHolonAsync(providerType, providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version));
        }

        public virtual OASISResult<T> LoadHolon<T>(ProviderType providerType, string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(LoadHolon(providerType, providerKey, loadChildren, recursive, maxChildDepth, continueOnError, version));
        }

        public virtual async Task<OASISResult<IHolon>> LoadHolonAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (this.HolonType == HolonType.GreatGrandSuperStar)
                GetGreatGrandSuperStar(ref result, await LoadAllHolonsAsync(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));

            else if (this.Id != Guid.Empty)
                result = await LoadHolonAsync(Id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
            {
                OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                    result = await LoadHolonAsync(providerType, providerKeyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, version);
                else
                    ErrorHandling.HandleError(ref result, $"Error occured in LoadHolonAsync. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
            }
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual OASISResult<IHolon> LoadHolon(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (this.HolonType == HolonType.GreatGrandSuperStar)
                GetGreatGrandSuperStar(ref result, LoadAllHolons(HolonType.GreatGrandSuperStar, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));

            else if (this.Id != Guid.Empty)
                result = LoadHolon(Id, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
            {
                OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                    result = LoadHolon(providerType, providerKeyResult.Result, loadChildren, recursive, maxChildDepth, continueOnError, version);
                else
                    ErrorHandling.HandleError(ref result, $"Error occured in LoadHolonAsync. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
            }
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            if (result != null && !result.IsError && result.Result != null)
                Mapper.MapBaseHolonProperties(result.Result, this);

            return result;
        }

        public virtual async Task<OASISResult<T>> LoadHolonAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await LoadHolonAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual OASISResult<T> LoadHolon<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(LoadHolon(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadAllHolonsAsync method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadAllHolons method with holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> LoadAllHolonsAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual OASISResult<IEnumerable<T>> LoadAllHolons<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(LoadAllHolons(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParentAsync method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParent method with id ", id, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(LoadHolonsForParent(id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParentAsync method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            OASISResult<IEnumerable<IHolon>> result = _holonManager.LoadHolonsForParent(providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in LoadHolonsForParent method with providerKey ", providerKey, " and holonType ", Enum.GetName(typeof(HolonType), holonType), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonsLoaded?.Invoke(this, new HolonsLoadedEventArgs() { Result = result });
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await LoadHolonsForParentAsync(providerType, providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version));
        }

        public virtual OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(ProviderType providerType, string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(LoadHolonsForParent(providerType, providerKey, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version));
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (this.Id != Guid.Empty)
                result = await LoadHolonsForParentAsync(Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
            {
                OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                    result = await LoadHolonsForParentAsync(providerType, providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);
                else
                    ErrorHandling.HandleError(ref result, $"Error occured in LoadHolonAsync. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
            }
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (this.Id != Guid.Empty)
                result = LoadHolonsForParent(Id, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

            else if (this.ProviderUniqueStorageKey != null && this.ProviderUniqueStorageKey.Count > 0)
            {
                OASISResult<string> providerKeyResult = GetCurrentProviderKey(providerType);

                if (!providerKeyResult.IsError && !string.IsNullOrEmpty(providerKeyResult.Result))
                    result = LoadHolonsForParent(providerType, providerKeyResult.Result, holonType, loadChildren, recursive, maxChildDepth, continueOnError, version);
                else
                    ErrorHandling.HandleError(ref result, $"Error occured in LoadHolonAsync. Reason: {providerKeyResult.Message}", providerKeyResult.DetailedMessage);
            }
            else
            {
                result.IsError = true;
                result.Message = CONST_USERMESSAGE_ID_OR_PROVIDERKEY_NOTSET;
            }

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> LoadHolonsForParentAsync<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await LoadHolonsForParentAsync(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual OASISResult<IEnumerable<T>> LoadHolonsForParent<T>(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(LoadHolonsForParent(holonType, loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public virtual async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<IHolon> result = await _holonManager.SaveHolonAsync(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError, providerType);
            HandleSaveHolonResult(savingHolon, "SaveHolonAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual OASISResult<IHolon> SaveHolon(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            savingHolon = RemoveCelesialBodies(savingHolon);
            OASISResult<IHolon> result = _holonManager.SaveHolon(savingHolon, saveChildren, recursive, maxChildDepth, continueOnError, providerType);
            HandleSaveHolonResult(savingHolon, "SaveHolon", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual async Task<OASISResult<T>> SaveHolonAsync<T>(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await SaveHolonAsync(savingHolon, mapBaseHolonProperties, saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public virtual OASISResult<T> SaveHolon<T>(IHolon savingHolon, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(SaveHolon(savingHolon, mapBaseHolonProperties, saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public virtual async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            if (savingHolons == null)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            savingHolons = RemoveCelesialBodies(savingHolons);
            OASISResult<IEnumerable<IHolon>> result = await _holonManager.SaveHolonsAsync(savingHolons, saveChildren, recursive, maxChildDepth, continueOnError, providerType);
            HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            if (savingHolons == null)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is null.", IsWarning = true };

            if (savingHolons.Count() == 0)
                return new OASISResult<IEnumerable<IHolon>>(savingHolons) { Message = "Holons collection is empty.", IsWarning = true };

            savingHolons = RemoveCelesialBodies(savingHolons);
            OASISResult<IEnumerable<IHolon>> result = _holonManager.SaveHolons(savingHolons, saveChildren, recursive, maxChildDepth, continueOnError, providerType);
            HandleSaveHolonsResult(savingHolons, "SaveHolonsAsync", ref result, mapBaseHolonProperties);
            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T>>> SaveHolonsAsync<T>(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await SaveHolonsAsync(savingHolons, mapBaseHolonProperties, saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public virtual OASISResult<IEnumerable<T>> SaveHolons<T>(IEnumerable<IHolon> savingHolons, bool mapBaseHolonProperties = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(SaveHolons(savingHolons, mapBaseHolonProperties, saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public virtual async Task<OASISResult<IZome>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> zomeResult = new OASISResult<IZome>((IZome)this);

            //First save the zome.
            OASISResult<IHolon> holonResult = await SaveHolonAsync(this, true, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

            if (holonResult.IsError)
            {
                zomeResult.IsError = true;
                zomeResult.Message = holonResult.Message;
            }

            if (saveChildren && (!zomeResult.IsError && holonResult.Result != null)
                || continueOnError && (zomeResult.IsError || holonResult.Result == null))
            {
                ZomeHelper.SetParentIdsForZome(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, this.ParentPlanet, this.ParentMoon, (IZome)this);

                // Now save the zome child holons (each OASIS Provider will recursively save each child holon, could do the recursion here and just save each holon indivudally with SaveHolonAsync but this way each OASIS Provider can optimise the way it saves (batches, etc), which would be quicker than making multiple calls...)
                OASISResult<IEnumerable<IHolon>> holonsResult = await SaveHolonsAsync(this.Holons, true, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

                if (holonsResult.IsError)
                {
                    zomeResult.IsError = true;
                    zomeResult.Message = holonsResult.Message;
                }

                if ((continueOnError && (holonsResult.IsError || holonResult.Result == null))
                    || (!holonResult.IsError && holonResult.Result != null))
                    this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.
            }

            OnSaved?.Invoke(this, new ZomeSavedEventArgs() { Result = zomeResult });
            return zomeResult;
        }

        public virtual OASISResult<IZome> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IZome> zomeResult = new OASISResult<IZome>((IZome)this);

            //First save the zome.
            OASISResult<IHolon> holonResult = SaveHolon(this, true, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

            if (holonResult.IsError)
            {
                zomeResult.IsError = true;
                zomeResult.Message = holonResult.Message;
            }

            if (saveChildren && (!zomeResult.IsError && holonResult.Result != null)
                || continueOnError && (zomeResult.IsError || holonResult.Result == null))
            {
                ZomeHelper.SetParentIdsForZome(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, this.ParentPlanet, this.ParentMoon, (IZome)this);

                // Now save the zome child holons (each OASIS Provider will recursively save each child holon, could do the recursion here and just save each holon indivudally with SaveHolonAsync but this way each OASIS Provider can optimise the the way it saves (batches, etc), which would be quicker than making multiple calls...)
                OASISResult<IEnumerable<IHolon>> holonsResult = SaveHolons(this.Holons, true, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

                if (holonsResult.IsError)
                {
                    zomeResult.IsError = true;
                    zomeResult.Message = holonsResult.Message;
                }
                
                if ((continueOnError && (holonsResult.IsError || holonResult.Result == null)) 
                    || (!holonResult.IsError && holonResult.Result != null))
                    this.Holons = (List<IHolon>)holonsResult.Result; // Update the holons collection now the holons will have their id's set.
            }

            OnSaved?.Invoke(this, new ZomeSavedEventArgs() { Result = zomeResult });
            return zomeResult;
        }

        public virtual async Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            return OASISResultHelperForHolons<IZome, T>.CopyResult(await SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public virtual OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IZome, new()
        {
            return OASISResultHelperForHolons<IZome, T>.CopyResult(Save(saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public async Task<OASISResult<IHolon>> AddHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            holon.ParentHolonId = this.Id;
            holon.ParentZomeId = this.Id;
            this.Holons.Add(holon);

            OASISResult<IHolon> result = await SaveHolonAsync(holon, true, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in AddHolonAsync method with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IHolon> AddHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            holon.ParentHolonId = this.Id;
            holon.ParentZomeId = this.Id;
            this.Holons.Add(holon);

            OASISResult<IHolon> result = SaveHolon(holon, true, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in AddHolon method with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });

            OnHolonAdded?.Invoke(this, new HolonAddedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<T>> AddHolonAsync<T>(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await AddHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public OASISResult<T> AddHolon<T>(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(AddHolon(holon, saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public async Task<OASISResult<IHolon>> RemoveHolonAsync(IHolon holon, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            holon.ParentHolonId = Guid.Empty;
            holon.ParentZomeId = Guid.Empty;
            this.Holons.Remove(holon);

            OASISResult<IHolon> result = await SaveHolonAsync(holon, true, false, false, 0, false, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in RemoveHolonAsync method attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });

            if (deleteHolon)
            {
                OASISResult<bool> deleteHolonResult = await _holonManager.DeleteHolonAsync(holon.Id, softDelete, providerType);

                if (deleteHolonResult.IsError)
                    OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in RemoveHolonAsync method attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });
            }

            OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IHolon> RemoveHolon(IHolon holon, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            holon.ParentHolonId = Guid.Empty;
            holon.ParentZomeId = Guid.Empty;
            this.Holons.Remove(holon);

            OASISResult<IHolon> result = SaveHolon(holon, true, false, false, 0, false, providerType);

            if (result.IsError)
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in RemoveHolon method attempting to save the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });

            if (deleteHolon)
            {
                OASISResult<bool> deleteHolonResult = _holonManager.DeleteHolon(holon.Id, softDelete, providerType);

                if (deleteHolonResult.IsError)
                    OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in RemoveHolon method attempting to delete the holon with ", LoggingHelper.GetHolonInfoForLogging(holon), ". Error Details: ", result.Message), Exception = result.Exception });
            }

            OnHolonRemoved?.Invoke(this, new HolonRemovedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<T>> RemoveHolonAsync<T>(IHolon holon, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(await RemoveHolonAsync(holon, deleteHolon, softDelete, providerType));
        }

        public OASISResult<T> RemoveHolon<T>(IHolon holon, bool deleteHolon = false, bool softDelete = true, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            return OASISResultHelperForHolons<IHolon, T>.CopyResult(RemoveHolon(holon, deleteHolon, softDelete, providerType));
        }

        private OASISResult<string> GetCurrentProviderKey(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();

            if (providerType == ProviderType.Default || providerType == ProviderType.All || providerType == ProviderType.None)
                providerType = ProviderManager.CurrentStorageProviderType.Value;

            if (ProviderUniqueStorageKey.ContainsKey(providerType) && !string.IsNullOrEmpty(ProviderUniqueStorageKey[providerType]))
                result.Result = ProviderUniqueStorageKey[providerType];
            else
                ErrorHandling.HandleError(ref result, string.Concat("ProviderUniqueStorageKey not found for CurrentStorageProviderType ", Enum.GetName(typeof(ProviderType), providerType)));

            return result;
        }

        private void GetGreatGrandSuperStar(ref OASISResult<IHolon> result, OASISResult<IEnumerable<IHolon>> holonsResult)
        {
            if (!holonsResult.IsError && holonsResult.Result != null)
            {
                List<IHolon> holons = (List<IHolon>)holonsResult.Result;

                if (holons.Count == 1)
                    result.Result = holons[0];
                else
                {
                    result.IsError = true;
                    result.Message = "ERROR, there should only be one GreatGrandSuperStar!";
                }
            }
        }

        private IEnumerable<IHolon> RemoveCelesialBodies(IEnumerable<IHolon> holons)
        {
            List<IHolon> holonsList = holons.ToList();

            for (int i = 0; i < holonsList.Count(); i++)
                holonsList[i] = RemoveCelesialBodies(holonsList[i]);

            return holonsList;
        }

        private IEnumerable<IHolon> RestoreCelesialBodies(IEnumerable<IHolon> holons)
        {
            List<IHolon> restoredHolons = new List<IHolon>();

            foreach (IHolon holon in holons)
                restoredHolons.Add(RestoreCelesialBodies(holon));

            return restoredHolons;
        }

        private IHolon RemoveCelesialBodies(IHolon holon)
        {
            if (holon.Id == Guid.Empty)
            {
                holon.Id = Guid.NewGuid();
                holon.IsNewHolon = true;
            }

            ICelestialBody celestialBody = holon as ICelestialBody;

            if (celestialBody != null)
            {
                _core[holon.Id] = celestialBody.CelestialBodyCore;
                celestialBody.CelestialBodyCore = null;
            }

            _parentOmiverse[holon.Id] = holon.ParentOmniverse;
            _parentDimension[holon.Id] = holon.ParentDimension;
            _parentMultiverse[holon.Id] = holon.ParentMultiverse;
            _parentUniverse[holon.Id] = holon.ParentUniverse;
            _parentGalaxyCluster[holon.Id] = holon.ParentGalaxyCluster;
            _parentGalaxy[holon.Id] = holon.ParentGalaxy;
            _parentSolarSystem[holon.Id] = holon.ParentSolarSystem;
            _parentGreatGrandSuperStar[holon.Id] = holon.ParentGreatGrandSuperStar;
            _parentGrandSuperStar[holon.Id] = holon.ParentGrandSuperStar;
            _parentSuperStar[holon.Id] = holon.ParentSuperStar;
            _parentStar[holon.Id] = holon.ParentStar;
            _parentPlanet[holon.Id] = holon.ParentPlanet;
            _parentMoon[holon.Id] = holon.ParentMoon;
            _parentCelestialSpace[holon.Id] = holon.ParentCelestialSpace;
            _parentCelestialBody[holon.Id] = holon.ParentCelestialBody;
            _parentZome[holon.Id] = holon.ParentZome;
            _parentHolon[holon.Id] = holon.ParentHolon;

            holon.ParentOmniverse = null;
            holon.ParentDimension = null;
            holon.ParentMultiverse = null;
            holon.ParentUniverse = null;
            holon.ParentGalaxyCluster = null;
            holon.ParentGalaxy = null;
            holon.ParentSolarSystem = null;
            holon.ParentGreatGrandSuperStar = null;
            holon.ParentGrandSuperStar = null;
            holon.ParentSuperStar = null;
            holon.ParentStar = null;
            holon.ParentPlanet = null;
            holon.ParentMoon = null;
            holon.ParentCelestialBody = null;
            holon.ParentCelestialSpace = null;
            holon.ParentZome = null;
            holon.ParentHolon = null;

            return holon;
        }

        private IHolon RestoreCelesialBodies(IHolon originalHolon) 
        {
            originalHolon.IsNewHolon = false;
            originalHolon.ParentOmniverse = _parentOmiverse[originalHolon.Id];
            originalHolon.ParentDimension = _parentDimension[originalHolon.Id];
            originalHolon.ParentMultiverse = _parentMultiverse[originalHolon.Id];
            originalHolon.ParentUniverse = _parentUniverse[originalHolon.Id];
            originalHolon.ParentGalaxyCluster = _parentGalaxyCluster[originalHolon.Id];
            originalHolon.ParentGalaxy = _parentGalaxy[originalHolon.Id];
            originalHolon.ParentSolarSystem = _parentSolarSystem[originalHolon.Id];
            originalHolon.ParentGreatGrandSuperStar = _parentGreatGrandSuperStar[originalHolon.Id];
            originalHolon.ParentGrandSuperStar = _parentGrandSuperStar[originalHolon.Id];
            originalHolon.ParentSuperStar = _parentSuperStar[originalHolon.Id];
            originalHolon.ParentStar = _parentStar[originalHolon.Id];
            originalHolon.ParentPlanet = _parentPlanet[originalHolon.Id];
            originalHolon.ParentMoon = _parentMoon[originalHolon.Id];
            originalHolon.ParentCelestialSpace = _parentCelestialSpace[originalHolon.Id];
            originalHolon.ParentCelestialBody = _parentCelestialBody[originalHolon.Id];
            originalHolon.ParentZome = _parentZome[originalHolon.Id];
            originalHolon.ParentHolon = _parentHolon[originalHolon.Id];

            _parentOmiverse.Remove(originalHolon.Id);
            _parentDimension.Remove(originalHolon.Id);
            _parentMultiverse.Remove(originalHolon.Id);
            _parentUniverse.Remove(originalHolon.Id);
            _parentGalaxyCluster.Remove(originalHolon.Id);
            _parentGalaxy.Remove(originalHolon.Id);
            _parentSolarSystem.Remove(originalHolon.Id);
            _parentGreatGrandSuperStar.Remove(originalHolon.Id);
            _parentGrandSuperStar.Remove(originalHolon.Id);
            _parentSuperStar.Remove(originalHolon.Id);
            _parentStar.Remove(originalHolon.Id);
            _parentPlanet.Remove(originalHolon.Id);
            _parentMoon.Remove(originalHolon.Id);
            _parentCelestialSpace.Remove(originalHolon.Id);
            _parentCelestialBody.Remove(originalHolon.Id);
            _parentZome.Remove(originalHolon.Id);
            _parentHolon.Remove(originalHolon.Id);

            ICelestialBody celestialBody = originalHolon as ICelestialBody;

            if (celestialBody != null)
            {
                celestialBody.CelestialBodyCore = _core[originalHolon.Id];
                _core.Remove(originalHolon.Id);
                return celestialBody;
            }

            return originalHolon;
        }

        private void HandleSaveHolonResult<T>(IHolon savingHolon, string callingMethodName, ref OASISResult<T> result, bool mapBaseHolonProperties = true) where T : IHolon, new()
        {
            if (!result.IsError && result.Result != null)
            {
                if (mapBaseHolonProperties)
                    Mapper<IHolon, T>.MapBaseHolonProperties(savingHolon, result.Result);

                result.Result = (T)RestoreCelesialBodies(savingHolon);
                OASISResult<IHolon> holonResult = new OASISResult<IHolon>(result.Result);
                OASISResultHelper<T, IHolon>.CopyResult(result, holonResult);
                OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = holonResult });
            }
            else
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), Exception = result.Exception });
        }

        private void HandleSaveHolonResult(IHolon savingHolon, string callingMethodName, ref OASISResult<IHolon> result, bool mapBaseHolonProperties = true)
        {
            if (!result.IsError && result.Result != null)
            {
                if (mapBaseHolonProperties)
                    Mapper.MapBaseHolonProperties(savingHolon, result.Result);

                result.Result = RestoreCelesialBodies(savingHolon);
                //result.Result = RestoreCelesialBodies(result.Result);
                OnHolonSaved?.Invoke(this, new HolonSavedEventArgs() { Result = result });
            }
            else
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in ", callingMethodName, " method for holon with ", LoggingHelper.GetHolonInfoForLogging(savingHolon), Enum.GetName(typeof(HolonType), savingHolon.HolonType), ". Error Details: ", result.Message), Exception = result.Exception });
        }

        private void HandleSaveHolonsResult(IEnumerable<IHolon> savingHolons, string callingMethodName, ref OASISResult<IEnumerable<IHolon>> result, bool mapBaseHolonProperties = true)
        {
            if (!result.IsError && result.Result != null)
            {
                //TODO: FIND OUT IF THIS IS STILL NEEDED ASAP?! THANKS! ;-)
                if (mapBaseHolonProperties)
                    result.Result = Mapper.MapBaseHolonProperties(savingHolons, result.Result);

                result.Result = RestoreCelesialBodies(savingHolons);
                OnHolonsSaved?.Invoke(this, new HolonsSavedEventArgs() { Result = result });
            }
            else
                OnZomeError?.Invoke(this, new ZomeErrorEventArgs() { Reason = string.Concat("Error in SaveHolonsAsync method. Error Details: ", result.Message), Exception = result.Exception });
        }
    }
}