using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using System.Collections.ObjectModel;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public abstract class CelestialSpace : CelestialHolon, ICelestialSpace
    {
        private List<IHolon> _children = new List<IHolon>();
        private List<ICelestialSpace> _celestialSpaces = new List<ICelestialSpace>();
        private List<ICelestialBody> _celestialBodies = new List<ICelestialBody>();
        public event CelestialSpaceLoaded OnCelestialSpaceLoaded;
        public event CelestialSpaceSaved OnCelestialSpaceSaved;
        public event CelestialSpaceAdded OnCelestialSpaceAdded;
        public event CelestialSpaceRemoved OnCelestialSpaceRemoved;
        public event CelestialSpaceError OnCelestialSpaceError;
        public event CelestialSpacesLoaded OnCelestialSpacesLoaded;
        public event CelestialSpacesSaved OnCelestialSpacesSaved;
        public event CelestialSpacesError OnCelestialSpacesError;
        public event CelestialBodyLoaded OnCelestialBodyLoaded;
        public event CelestialBodySaved OnCelestialBodySaved;
        public event CelestialBodyAdded OnCelestialBodyAdded;
        public event CelestialBodyRemoved OnCelestialBodyRemoved;
        public event CelestialBodyError OnCelestialBodyError;
        public event CelestialBodiesLoaded OnCelestialBodiesLoaded;
        public event CelestialBodiesSaved OnCelestialBodiesSaved;
        public event CelestialBodiesError OnCelestialBodiesError;

        public event ZomeLoaded OnZomeLoaded;
        public event ZomeSaved OnZomeSaved;
        public event ZomeError OnZomeError;
        public event ZomesLoaded OnZomesLoaded;
        public event ZomesSaved OnZomesSaved;
        public event ZomesError OnZomesError;
        public event HolonLoaded OnHolonLoaded;
        public event HolonSaved OnHolonSaved;
        public event HolonError OnHolonError;
        public event HolonsLoaded OnHolonsLoaded;
        public event HolonsSaved OnHolonsSaved;
        public event HolonsError OnHolonsError;

        public IStar NearestStar { get; set; }

        public ReadOnlyCollection<ICelestialSpace> CelestialSpaces
        {
            get
            {
                return _celestialSpaces.AsReadOnly();
            }
        }

        public ReadOnlyCollection<ICelestialBody> CelestialBodies
        {
            get
            {
                return _celestialBodies.AsReadOnly();
            }
        }

        public new ReadOnlyCollection<IHolon> Children
        {
            get
            {
                return _children.AsReadOnly();
            }

            //protected set
            //{
            //    _children = value.ToList();
            //}
        }

        public CelestialSpace(HolonType holonType) : base(holonType)
        {
            Initialize();
        }

        public CelestialSpace(Guid id, HolonType holonType, bool autoLoad = true) : base(id, holonType)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(Guid id, HolonType holonType, IStar parentStar, bool autoLoad = true) : base(id, holonType, parentStar)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(Guid id, HolonType holonType, Guid parentStarId, bool autoLoad = true) : base(id, holonType, parentStarId)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(string providerKey, ProviderType providerType, HolonType holonType, bool autoLoad = true) : base(providerKey, providerType, holonType)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(string providerKey, ProviderType providerType, HolonType holonType, IStar parentStar, bool autoLoad = true) : base(providerKey, providerType, holonType, parentStar)
        {
            Initialize(autoLoad);
        }

        public CelestialSpace(string providerKey, ProviderType providerType, HolonType holonType, Guid parentStarId, bool autoLoad = true) : base(providerKey, providerType, holonType, parentStarId)
        {
            Initialize(autoLoad);
        }

        //public CelestialSpace(Dictionary<ProviderType, string> providerKey, HolonType holonType) : base(providerKey, holonType)
        //{
        //    Initialize();
        //}

        protected void Initialize(bool autoLoad = true)
        {
            RegisterCelestialBodies(this.CelestialBodies);
            RegisterCelestialSpaces(this.CelestialSpaces);

            if (autoLoad && !IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
            {
                OASISResult<ICelestialSpace> celestialSpaceResult = Load();

                if (celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                    base.Initialize();
            }
        }

        protected async Task InitializeAsync(bool autoLoad = true)
        {
            RegisterCelestialBodies(this.CelestialBodies);
            RegisterCelestialSpaces(this.CelestialSpaces);

            if (autoLoad && !IsNewHolon && (Id != Guid.Empty || (ProviderUniqueStorageKey != null && ProviderUniqueStorageKey.Keys.Count > 0)))
            {
                OASISResult<ICelestialSpace> celestialSpaceResult = await LoadAsync();

                if (celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                    await base.InitializeAsync();
            }
        }

        public new async Task<OASISResult<ICelestialSpace>> LoadAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadAsync method loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            HandleLoadCelestialSpace(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonAsync(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadAsync");

            /*
            if ((holonResult != null && !holonResult.IsError && holonResult.Result != null)
                || ((holonResult == null || holonResult.IsError || holonResult.Result == null) && continueOnError))
            {
                if (holonResult != null && !holonResult.IsError && holonResult.Result != null) 
                {
                    //Mapper.MapBaseHolonProperties(holonResult.Result, this);
                    //result.Result = this;
                }
                else
                {
                    // If there was an error then continueOnError must have been set to true.
                    OASISErrorHandling.HandleWarning(ref result, $"An errror occured in CelestialSpace.LoadAsync method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. ContinueOnError is set to true so continuing to attempt to load the celestial bodies... Reason: {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result });
                }

                if (loadChildren)
                {
                    OASISResult<IEnumerable<ICelestialBody>> celestialBodiesResult = await LoadCelestialBodiesAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

                    if (!(celestialBodiesResult != null && !celestialBodiesResult.IsError && celestialBodiesResult.Result != null))
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"Error occured in CelestialSpace.LoadAsync method. The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} failed to load and one or more of it's celestialbodies failed to load. Reason: {celestialBodiesResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"Error occured in CelestialSpace.LoadAsync method. The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} loaded fine but one or more of it's celestialbodies failed to load. Reason: {celestialBodiesResult.Message}");

                        OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                        if (!continueOnError)
                        {
                            OnCelestialSpaceLoaded?.Invoke(this, new CelestialSpaceLoadedEventArgs() { Result = result });
                            return result;
                        }
                    }

                    OASISResult<IEnumerable<ICelestialSpace>> celestialSpacesResult = await LoadCelestialSpacesAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

                    if (!(celestialSpacesResult != null && !celestialSpacesResult.IsError && celestialSpacesResult.Result != null))
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"Error occured in CelestialSpace.LoadAsync method. The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} failed to load and one or more of it's child celestialspaces failed to load. Reason: {celestialSpacesResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"Error occured in CelestialSpace.LoadAsync method. The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} loaded fine but one or more of it's child celestialspaces failed to load. Reason: {celestialSpacesResult.Message}");

                        OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result });
                    }
                }
            }*/

            return result;
        }

        public new OASISResult<ICelestialSpace> Load(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.Load method loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpace(result, star.CelestialBodyCore.GlobalHolonData.LoadHolon(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "Load");
            return result;
        }

        public new async Task<OASISResult<T>> LoadAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadAsync<T> method loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpace(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonAsync<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadAsync<T>");
            return result;
        }

        public new OASISResult<T> Load<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.Load<T> method loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpace(result, star.CelestialBodyCore.GlobalHolonData.LoadHolon<T>(this.Id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "Load<T>");
            return result;
        }


        public async Task<OASISResult<IEnumerable<ICelestialBody>>> LoadCelestialBodiesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAsync method loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAsync");
            return result;
        }

        public OASISResult<IEnumerable<ICelestialBody>> LoadCelestialBodies(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodies method loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodies(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodies");
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadCelestialBodiesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAsync<T> method loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync<T>(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAsync<T>");
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadCelestialBodies<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodies<T> method loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodies(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent<T>(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodies<T>");
            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialSpace>>> LoadCelestialSpacesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialSpacesAsync method loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialSpacesAsync");
            return result;
        }

        public OASISResult<IEnumerable<ICelestialSpace>> LoadCelestialSpaces(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialSpaces method loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpaces(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialSpaces");
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadCelestialSpacesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialSpacesAsync<T> method loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialSpacesAsync<T>");
            return result;
        }

        public OASISResult<IEnumerable<T>> LoadCelestialSpaces<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialSpaces<T> method loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialSpaces(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialSpaces<T>");
            return result;
        }


        public async Task<OASISResult<ICelestialBodiesAndSpaces>> LoadCelestialBodiesAndSpacesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBodiesAndSpaces> result = new OASISResult<ICelestialBodiesAndSpaces>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAndSpacesAsync method loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodiesAndSpaces(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAndSpacesAsync");
            return result;
        }

        public OASISResult<ICelestialBodiesAndSpaces> LoadCelestialBodiesAndSpaces(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBodiesAndSpaces> result = new OASISResult<ICelestialBodiesAndSpaces>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAndSpaces method loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodiesAndSpaces(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAndSpaces");
            return result;
        }

        public async Task<OASISResult<ICelestialBodiesAndSpaces<T1, T2>>> LoadCelestialBodiesAndSpacesAsync<T1, T2>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new()
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result = new OASISResult<ICelestialBodiesAndSpaces<T1, T2>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAndSpacesAsync<T1, T2> method loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodiesAndSpaces(result, await star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParentAsync(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAndSpacesAsync<T>");
            return result;
        }

        public OASISResult<ICelestialBodiesAndSpaces<T1, T2>> LoadCelestialBodiesAndSpaces<T1, T2>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new()
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result = new OASISResult<ICelestialBodiesAndSpaces<T1, T2>>();
            IStar star = GetCelestialSpaceNearestStar(result, $"Error occured in CelestialSpace.LoadCelestialBodiesAndSpaces<T1, T2> method loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
            result = HandleLoadCelestialBodiesAndSpaces(result, star.CelestialBodyCore.GlobalHolonData.LoadHolonsForParent(this.Id, HolonType.All, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version, providerType), "LoadCelestialBodiesAndSpaces<T>");
            return result;
        }

        /*
        public async Task<OASISResult<IEnumerable<ICelestialBody>>> LoadCelestialBodiesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            //OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>(this.CelestialBodies);
            //OASISResult<ICelestialBody> celestialBodyResult = null;
            
            //TODO: Find a way to use new generic version so can use ICelestialBody instead of IHolon.
            OASISResult<ICelestialBody> celestialBodyResult = null;

            foreach (ICelestialBody celestialBody in CelestialBodies)
            {
                //TODO: Find a way to use new generic version so can use ICelestialBody instead of IHolon.
                celestialBodyResult = await celestialBody.LoadAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

                if (celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError)
                    result.LoadedCount++;
                else
                {
                    result.ErrorCount++;
                    OASISErrorHandling.HandleWarning(ref celestialBodyResult, $"There was an error in CelestialSpace.LoadCelestialBodiesAsync method whilst loading the {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")}. Reason: {celestialBodyResult.Message}", true, false, false, true, false);

                    //TODO: Think better to just raise one error (below) rather than lots for every item?
                    //OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                    if (!continueOnError)
                        break;
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured in CelestialSpace.LoadCelestialBodiesAsync method loading {CelestialBodies.Count} CelestialBodies in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.LoadedCount == 0)
                    OASISErrorHandling.HandleError(ref result, message);
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, message);
                    result.IsLoaded = true;
                }

                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = result });
            }
            else
                result.IsLoaded = true;

            OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<ICelestialBody>> LoadCelestialBodies(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return LoadCelestialBodiesAsync(loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadCelestialBodiesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            return OASISResultHelperForHolons<ICelestialBody, T>.CopyResult(await LoadCelestialBodiesAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public OASISResult<IEnumerable<T>> LoadCelestialBodies<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            return OASISResultHelperForHolons<ICelestialBody, T>.CopyResult(LoadCelestialBodies(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public async Task<OASISResult<IEnumerable<ICelestialSpace>>> LoadCelestialSpacesAsync(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>(this.CelestialSpaces);
            OASISResult<ICelestialSpace> celestialSpaceResult = null;

            foreach (ICelestialSpace celestialSpace in CelestialSpaces)
            {
                celestialSpaceResult = await celestialSpace.LoadAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType);

                if (celestialSpaceResult != null && celestialSpaceResult.Result != null && !celestialSpaceResult.IsError)
                    result.LoadedCount++;
                else
                {
                    result.ErrorCount++;
                    OASISErrorHandling.HandleWarning(ref celestialSpaceResult, $"There was an error in CelestialSpace.LoadCelestialSpacesAsync method whilst loading the {LoggingHelper.GetHolonInfoForLogging(celestialSpace, "CelestialSpace")}. Reason: {celestialSpaceResult.Message}", true, false, false, true, false);

                    //TODO: Think better to just raise one error (below) rather than lots for every item?
                    //OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                    if (!continueOnError)
                        break;
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured in CelestialSpace.LoadCelestialSpacesAsync method loading {CelestialSpaces.Count} CelestialSpaces in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.LoadedCount == 0)
                    OASISErrorHandling.HandleError(ref result, message);
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, message);
                    result.IsLoaded = true;
                }

                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = result });
            }
            else
                result.IsLoaded = true;

            OnCelestialSpacesLoaded?.Invoke(this, new CelestialSpacesLoadedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<ICelestialSpace>> LoadCelestialSpaces(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return LoadCelestialSpacesAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType).Result;
        }

        public async Task<OASISResult<IEnumerable<T>>> LoadCelestialSpacesAsync<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new()
        {
            return OASISResultHelperForHolons<ICelestialSpace, T>.CopyResult(await LoadCelestialSpacesAsync(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }

        public OASISResult<IEnumerable<T>> LoadCelestialSpaces<T>(bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new()
        {
            return OASISResultHelperForHolons<ICelestialSpace, T>.CopyResult(LoadCelestialSpaces(loadChildren, recursive, maxChildDepth, continueOnError, version, providerType));
        }
        */

        public new virtual async Task<OASISResult<ICelestialSpace>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            string errorMessage = $"Error occured in CelestialSpace.SaveAsync saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialSpace(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync(this, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");

                //TODO: We could of course just save using the one line below instead of the 2 lines above but then it would break the STAR NET design of the stars being responsible for loading/saving the celestialspace/celestial bodies in its orbit.
                //HandleSaveCelestialSpace(result, await base.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            return result;
        }

        public new virtual OASISResult<ICelestialSpace> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            string errorMessage = $"Error occured in CelestialSpace.Save saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialSpace(result, star.CelestialBodyCore.GlobalHolonData.SaveHolon(this, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "Save");

                //TODO: We could of course just save using the one line below instead of the 2 lines above but then it would break the STAR NET design of the stars being responsible for loading/saving the celestialspace/celestial bodies in its orbit.
                //HandleSaveCelestialSpace(result, await base.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            return result;
        }

        public new virtual async Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in CelestialSpace.SaveAsync<T> saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialSpace(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync<T>(this, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonAsync<T>: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T, ICelestialSpace>(result), Exception = result.Exception });
            }

            return result;
        }

        public new virtual OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : IHolon, new()
        {
            OASISResult<T> result = new OASISResult<T>();
            string errorMessage = $"Error occured in CelestialSpace.Save<T> saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialSpace(result, star.CelestialBodyCore.GlobalHolonData.SaveHolon<T>(this, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolon<T>: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T, ICelestialSpace>(result), Exception = result.Exception });
            }

            return result;
        }

        /*
        public async Task<OASISResult<ICelestialSpace>> SaveAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            IsSaving = true;

            if (!STAR.IsStarIgnited)
                STAR.ShowStatusMessage(Enums.StarStatusMessageType.Processing, $"Creating CelestialSpace {this.Name}...");

            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in CelestialSpace.SaveAsync method saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result });
                IsSaving = false;
                return result;
            }

            OASISResult<IHolon> holonResult = await star.CelestialBodyCore.SaveHolonAsync(this, true, saveChildren, recursive, maxChildDepth, continueOnError, providerType);

            if ((holonResult != null && !holonResult.IsError && holonResult.Result != null)
                || ((holonResult == null || holonResult.IsError || holonResult.Result == null) && continueOnError))
            {
                if (!(holonResult != null && !holonResult.IsError && holonResult.Result != null))
                {
                    // If there was an error then continueOnError must have been set to true.
                    OASISErrorHandling.HandleWarning(ref result, $"An errror occured in CelestialSpace.SaveAsync method saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. ContinueOnError is set to true so continuing to attempt to save the celestial bodies... Reason: {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result });
                }
                else
                    result.Result = (ICelestialSpace)holonResult.Result;

                if (saveChildren)
                {
                    OASISResult<IEnumerable<ICelestialBody>> celestialBodiesResult = await SaveCelestialBodiesAsync(saveChildren, recursive, maxChildDepth, continueOnError, providerType);

                    if (!(celestialBodiesResult != null && !celestialBodiesResult.IsError && celestialBodiesResult.Result != null))
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"Error occured in CelestialSpace.SaveAsync method. The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} failed to save and one or more of it's celestialbodies failed to save. Reason: {celestialBodiesResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"Error occured in CelestialSpace.SaveAsync method. The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} saved fine but one or more of it's celestialbodies failed to save. Reason: {celestialBodiesResult.Message}");

                        OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                        if (!continueOnError)
                        {
                            OnCelestialSpaceSaved?.Invoke(this, new CelestialSpaceSavedEventArgs() { Result = result });
                            IsSaving = false;
                            return result;
                        }
                    }

                    OASISResult<IEnumerable<ICelestialSpace>> celestialSpacesResult = await SaveCelestialSpacesAsync(saveChildren, recursive, maxChildDepth, continueOnError, providerType);

                    if (!(celestialSpacesResult != null && !celestialSpacesResult.IsError && celestialSpacesResult.Result != null))
                    {
                        if (result.IsWarning)
                            OASISErrorHandling.HandleError(ref result, $"Error occured in CelestialSpace.SaveAsync method. The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} failed to save and one or more of it's child celestialspaces failed to save. Reason: {celestialSpacesResult.Message}");
                        else
                            OASISErrorHandling.HandleWarning(ref result, $"Error occured in CelestialSpace.SaveAsync method. The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} saved fine but one or more of it's child celestialspaces failed to save. Reason: {celestialSpacesResult.Message}");

                        OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = result });
                    }
                }
            }

            if (result.WarningCount > 0)
            {
                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, $"There was {result.WarningCount} error(s) in CelestialSpace.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. All operations failed, please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                else
                    OASISErrorHandling.HandleWarning(ref result, $"There was {result.WarningCount} error(s) in CelestialSpace.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. {result.SavedCount} operations did save correctly however. Please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");

                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result });

                if (!STAR.IsStarIgnited) //TODO: Not sure why this is here?! lol NEED TO DOUBLE CHECK ASAP...
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Error, $"Error Creating CelestialSpace {this.Name}. Reason: {result.Message}");
            }
            else
            {
                result.IsSaved = true;

                if (!STAR.IsStarIgnited) //TODO: Not sure why this is here?! lol NEED TO DOUBLE CHECK ASAP...
                    STAR.ShowStatusMessage(Enums.StarStatusMessageType.Success, $"CelestialSpace {this.Name} Created.");
            }

            IsSaving = false;
            OnCelestialSpaceSaved?.Invoke(this, new CelestialSpaceSavedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<ICelestialSpace> Save(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            return SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, providerType).Result;
        }

        public async Task<OASISResult<T>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new()
        {
            return OASISResultHelperForHolons<ICelestialSpace, T>.CopyResult(await SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }

        public OASISResult<T> Save<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialSpace, new()
        {
            return OASISResultHelperForHolons<ICelestialSpace, T>.CopyResult(Save(saveChildren, recursive, maxChildDepth, continueOnError, providerType));
        }
        */

        public async Task<OASISResult<ICelestialBodiesAndSpaces>> SaveCelestialBodiesAndSpacesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBodiesAndSpaces> result = new OASISResult<ICelestialBodiesAndSpaces>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAndSpacesAsync saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialBodiesAndSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync(_children, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAndSpacesAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public OASISResult<ICelestialBodiesAndSpaces> SaveCelestialBodiesAndSpaces(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBodiesAndSpaces> result = new OASISResult<ICelestialBodiesAndSpaces>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAndSpacesAsync saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialBodiesAndSpaces(result, star.CelestialBodyCore.GlobalHolonData.SaveHolons(_children, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAndSpaces");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<ICelestialBodiesAndSpaces<T1, T2>>> SaveCelestialBodiesAndSpacesAsync<T1, T2>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new()
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result = new OASISResult<ICelestialBodiesAndSpaces<T1, T2>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAndSpacesAsync<T1, T2> saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialBodiesAndSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync(_children, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAndSpacesAsync<T1, T2>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces<T1, T2>, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public OASISResult<ICelestialBodiesAndSpaces<T1, T2>> SaveCelestialBodiesAndSpaces<T1, T2>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T1 : ICelestialBody, new() where T2 : ICelestialSpace, new()
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result = new OASISResult<ICelestialBodiesAndSpaces<T1, T2>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAndSpaces<T1, T2> saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialBodiesAndSpaces(result, star.CelestialBodyCore.GlobalHolonData.SaveHolons(_children, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAndSpaces<T1, T2>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces<T1, T2>, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialBody>>> SaveCelestialBodiesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAsync saving the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync(_celestialBodies, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public OASISResult<IEnumerable<ICelestialBody>> SaveCelestialBodies(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialBody>> result = new OASISResult<IEnumerable<ICelestialBody>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodies saving the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialBodies(result, star.CelestialBodyCore.GlobalHolonData.SaveHolons(_celestialBodies, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodies");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveCelestialBodiesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodiesAsync<T> saving the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync<T>(Mapper.Convert<ICelestialBody, T>(_celestialBodies), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAsync<T>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T, ICelestialBody>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveCelestialBodies<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialBodies<T> saving the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialBodies(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync<T>(Mapper.Convert<ICelestialBody, T>(_celestialBodies), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialBodiesAsync<T>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T, ICelestialBody>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<ICelestialSpace>>> SaveCelestialSpacesAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialSpacesAsync saving the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync(_celestialSpaces, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialSpacesAsync");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public OASISResult<IEnumerable<ICelestialSpace>> SaveCelestialSpaces(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<ICelestialSpace>> result = new OASISResult<IEnumerable<ICelestialSpace>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialSpaces saving the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialSpaces(result, star.CelestialBodyCore.GlobalHolonData.SaveHolons(_celestialSpaces, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialSpaces");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = result, Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveCelestialSpacesAsync<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialSpacesAsync<T> saving the celestial Spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync<T>(Mapper.Convert<ICelestialSpace, T>(_celestialSpaces), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialSpacesAsync<T>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public async Task<OASISResult<IEnumerable<T>>> SaveCelestialSpaces<T>(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            string errorMessage = $"Error occured in CelestialSpace.SaveCelestialSpaces<T> saving the celestial Spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                IsSaving = true;
                IStar star = GetCelestialSpaceNearestStar(result, errorMessage);
                HandleSaveCelestialSpaces(result, await star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync<T>(Mapper.Convert<ICelestialSpace, T>(_celestialSpaces), saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType), "SaveCelestialSpacesAsync<T>");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling star.CelestialBodyCore.GlobalHolonData.SaveHolonsAsync: {ex}", ex);
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T, ICelestialSpace>(result), Exception = result.Exception });
            }

            IsSaving = false;
            return result;
        }

        public OASISResult<ICelestialBody> AddCelestialBody(ICelestialBody celestialBody, bool saveCelestialBody = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialBody adding the celestial body {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")} to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialBody)
                    result = celestialBody.Save(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if ((saveCelestialBody && result != null && !result.IsError) || !saveCelestialBody)
                {
                    _celestialBodies.Add(celestialBody);
                    _children.Add(celestialBody);

                    OnCelestialBodyAdded?.Invoke(this, new CelestialBodyAddedEventArgs() { Result = new OASISResult<ICelestialBody>(celestialBody) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialBody.Save method: {result.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<ICelestialBody>> AddCelestialBodyAsync(ICelestialBody celestialBody, bool saveCelestialBody = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialBodyAsync adding the celestial body {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")} to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialBody)
                    result = await celestialBody.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if ((saveCelestialBody && result != null && !result.IsError) || !saveCelestialBody)
                {
                    _celestialBodies.Add(celestialBody);
                    _children.Add(celestialBody);

                    OnCelestialBodyAdded?.Invoke(this, new CelestialBodyAddedEventArgs() { Result = new OASISResult<ICelestialBody>(celestialBody) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialBody.SaveAsync method: {result.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<ICelestialBody> RemoveCelestialBody(ICelestialBody celestialBody, bool deleteCelestialBody = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            OASISResult<IHolon> holonResult = null;
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialBody removing the celestial body {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")} from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (deleteCelestialBody)
                    holonResult = celestialBody.Delete(softDelete, providerType);

                if ((deleteCelestialBody && holonResult != null && !holonResult.IsError) || !deleteCelestialBody)
                {
                    _celestialBodies.Remove(celestialBody);
                    _children.Remove(celestialBody);

                    OnCelestialBodyRemoved?.Invoke(this, new CelestialBodyRemovedEventArgs() { Result = new OASISResult<ICelestialBody>(celestialBody) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialBody.Delete method: {holonResult.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<ICelestialBody>> RemoveCelestialBodyAsync(ICelestialBody celestialBody, bool deleteCelestialBody = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            OASISResult<IHolon> holonResult = null;
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialBodyAsync removing the celestial body {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")} from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (deleteCelestialBody)
                    holonResult = await celestialBody.DeleteAsync(softDelete, providerType);

                if ((deleteCelestialBody && holonResult != null && !holonResult.IsError) || !deleteCelestialBody)
                {
                    _celestialBodies.Remove(celestialBody);
                    _children.Remove(celestialBody);

                    OnCelestialBodyRemoved?.Invoke(this, new CelestialBodyRemovedEventArgs() { Result = new OASISResult<ICelestialBody>(celestialBody) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialBody.DeleteAsync method: {holonResult.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<ICelestialSpace> AddCelestialSpace(ICelestialSpace celestialSpace, bool saveCelestialSpace = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialSpace adding the celestial space {LoggingHelper.GetHolonInfoForLogging(celestialSpace, "CelestialSpace")} to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialSpace)
                    result = celestialSpace.Save(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if ((saveCelestialSpace && result != null && !result.IsError) || !saveCelestialSpace)
                {
                    _celestialSpaces.Remove(celestialSpace);
                    _children.Remove(celestialSpace);

                    OnCelestialSpaceAdded?.Invoke(this, new CelestialSpaceAddedEventArgs() { Result = new OASISResult<ICelestialSpace>(celestialSpace) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialSpace.Save method: {result.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<ICelestialSpace>> AddCelestialSpaceAsync(ICelestialSpace celestialSpace, bool saveCelestialSpace = true, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            string errorMessage = $"An error occured in CelestialSpace.AddCelestialSpaceAsync adding the celestial space {LoggingHelper.GetHolonInfoForLogging(celestialSpace, "CelestialSpace")} to the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (saveCelestialSpace)
                    result = await celestialSpace.SaveAsync(saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, providerType);

                if ((saveCelestialSpace && result != null && !result.IsError) || !saveCelestialSpace)
                {
                    _celestialSpaces.Remove(celestialSpace);
                    _children.Remove(celestialSpace);

                    OnCelestialSpaceAdded?.Invoke(this, new CelestialSpaceAddedEventArgs() { Result = new OASISResult<ICelestialSpace>(celestialSpace) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialSpace.SaveAsync method: {result.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public OASISResult<ICelestialSpace> RemoveCelestialSpace(ICelestialSpace celestialSpace, bool deleteCelestialSpace = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            OASISResult<IHolon> holonResult = null;
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialSpace removing the celestial space {LoggingHelper.GetHolonInfoForLogging(celestialSpace, "CelestialSpace")} from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (deleteCelestialSpace)
                    holonResult = celestialSpace.Delete(softDelete, providerType);

                if ((deleteCelestialSpace && holonResult != null && !holonResult.IsError) || !deleteCelestialSpace)
                {
                    _celestialSpaces.Remove(celestialSpace);
                    _children.Remove(celestialSpace);

                    OnCelestialSpaceRemoved?.Invoke(this, new CelestialSpaceRemovedEventArgs() { Result = new OASISResult<ICelestialSpace>(celestialSpace) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialSpace.Delete method: {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        public async Task<OASISResult<ICelestialSpace>> RemoveCelestialSpaceAsync(ICelestialSpace celestialSpace, bool deleteCelestialSpace = true, bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            OASISResult<IHolon> holonResult = null;
            string errorMessage = $"An error occured in CelestialSpace.RemoveCelestialSpaceAsync removing the celestial space {LoggingHelper.GetHolonInfoForLogging(celestialSpace, "CelestialSpace")} from the celestial space {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                if (deleteCelestialSpace)
                    holonResult = await celestialSpace.DeleteAsync(softDelete, providerType);

                if ((deleteCelestialSpace && holonResult != null && !holonResult.IsError) || !deleteCelestialSpace)
                {
                    _celestialSpaces.Remove(celestialSpace);
                    _children.Remove(celestialSpace);

                    OnCelestialSpaceRemoved?.Invoke(this, new CelestialSpaceRemovedEventArgs() { Result = new OASISResult<ICelestialSpace>(celestialSpace) });
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error returned from celestialSpace.DeleteAsync method: {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Result = result, Reason = result.Message, Exception = ex });
            }

            return result;
        }

        //public IEnumerable<ICelestialBody> GetCelestialBodies()
        //{
        //    return _celestialBodies;
        //}

        //public IEnumerable<ICelestialSpace> GetCelestialSpaces()
        //{
        //    return _celestialSpaces;
        //}

        protected void RegisterCelestialBodies(IEnumerable<ICelestialBody> celestialBodies, bool unregisterExistingBodiesFirst = true)
        {
            if (unregisterExistingBodiesFirst)
                UnregisterAllCelestialSpaces();

            _celestialBodies.AddRange(celestialBodies);

            foreach (ICelestialBody celestialBody in _celestialBodies)
            {
                celestialBody.OnCelestialBodyLoaded += CelestialBody_OnCelestialBodyLoaded;
                celestialBody.OnCelestialBodySaved += CelestialBody_OnCelestialBodySaved;
                celestialBody.OnCelestialBodyError += CelestialBody_OnCelestialBodyError;
                celestialBody.OnHolonLoaded += CelestialBody_OnHolonLoaded;
                celestialBody.OnHolonSaved += CelestialBody_OnHolonSaved;
                celestialBody.OnHolonError += CelestialBody_OnHolonError;
                celestialBody.OnHolonsLoaded += CelestialBody_OnHolonsLoaded;
                celestialBody.OnHolonsSaved += CelestialBody_OnHolonsSaved;
                celestialBody.OnHolonsError += CelestialBody_OnHolonsError;
                celestialBody.OnZomeLoaded += CelestialBody_OnZomeLoaded;
                celestialBody.OnZomeSaved += CelestialBody_OnZomeSaved;
                celestialBody.OnZomeError += CelestialBody_OnZomeError;
                celestialBody.OnZomesLoaded += CelestialBody_OnZomesLoaded;
                celestialBody.OnZomesSaved += CelestialBody_OnZomesSaved;
                celestialBody.OnZomesError += CelestialBody_OnZomesError;
            }
        }

        protected void RegisterCelestialSpaces(IEnumerable<ICelestialSpace> celestialSpaces, bool unregisterExistingSpacesFirst = true)
        {
            if (unregisterExistingSpacesFirst)
                UnregisterAllCelestialSpaces();

            _celestialSpaces.AddRange(celestialSpaces);

            foreach (ICelestialSpace celestialSpace in _celestialSpaces)
            {
                celestialSpace.OnCelestialSpaceLoaded += CelestialSpace_OnCelestialSpaceLoaded;
                celestialSpace.OnCelestialSpaceSaved += CelestialSpace_OnCelestialSpaceSaved;
                celestialSpace.OnCelestialSpaceError += CelestialSpace_OnCelestialSpaceError;
                celestialSpace.OnCelestialSpacesLoaded += CelestialSpace_OnCelestialSpacesLoaded;
                celestialSpace.OnCelestialSpacesSaved += CelestialSpace_OnCelestialSpacesSaved;
                celestialSpace.OnCelestialSpacesError += CelestialSpace_OnCelestialSpacesError;
                celestialSpace.OnCelestialBodyLoaded += CelestialSpace_OnCelestialBodyLoaded;
                celestialSpace.OnCelestialBodySaved += CelestialSpace_OnCelestialBodySaved;
                celestialSpace.OnCelestialBodyError += CelestialSpace_OnCelestialBodyError;
                celestialSpace.OnCelestialBodiesLoaded += CelestialSpace_OnCelestialBodiesLoaded;
                celestialSpace.OnCelestialBodiesSaved += CelestialSpace_OnCelestialBodiesSaved;
                celestialSpace.OnCelestialBodiesError += CelestialSpace_OnCelestialBodiesError;
                celestialSpace.OnHolonLoaded += CelestialSpace_OnHolonLoaded;
                celestialSpace.OnHolonSaved += CelestialSpace_OnHolonSaved;
                celestialSpace.OnHolonError += CelestialSpace_OnHolonError;
                celestialSpace.OnHolonsLoaded += CelestialSpace_OnHolonsLoaded;
                celestialSpace.OnHolonsSaved += CelestialSpace_OnHolonsSaved;
                celestialSpace.OnHolonsError += CelestialSpace_OnHolonsError;
                celestialSpace.OnZomeLoaded += CelestialSpace_OnZomeLoaded;
                celestialSpace.OnZomeSaved += CelestialSpace_OnZomeSaved;
                celestialSpace.OnZomeError += CelestialSpace_OnZomeError;
                celestialSpace.OnZomesLoaded += CelestialSpace_OnZomesLoaded;
                celestialSpace.OnZomesSaved += CelestialSpace_OnZomesSaved;
                celestialSpace.OnZomesError += CelestialSpace_OnZomesError;
            }
        }

        protected void UnregisterAllCelestialBodies()
        {
            //First unsubscibe events to prevent any memory leaks.
            foreach (ICelestialBody celestialBody in _celestialBodies)
            {
                celestialBody.OnCelestialBodyLoaded -= CelestialBody_OnCelestialBodyLoaded;
                celestialBody.OnCelestialBodySaved -= CelestialBody_OnCelestialBodySaved;
                celestialBody.OnCelestialBodyError -= CelestialBody_OnCelestialBodyError;
                celestialBody.OnHolonLoaded -= CelestialBody_OnHolonLoaded;
                celestialBody.OnHolonSaved -= CelestialBody_OnHolonSaved;
                celestialBody.OnHolonError -= CelestialBody_OnHolonError;
                celestialBody.OnHolonsLoaded -= CelestialBody_OnHolonsLoaded;
                celestialBody.OnHolonsSaved -= CelestialBody_OnHolonsSaved;
                celestialBody.OnHolonsError -= CelestialBody_OnHolonsError;
                celestialBody.OnZomeLoaded -= CelestialBody_OnZomeLoaded;
                celestialBody.OnZomeSaved -= CelestialBody_OnZomeSaved;
                celestialBody.OnZomeError -= CelestialBody_OnZomeError;
                celestialBody.OnZomesLoaded -= CelestialBody_OnZomesLoaded;
                celestialBody.OnZomesSaved -= CelestialBody_OnZomesSaved;
                celestialBody.OnZomesError -= CelestialBody_OnZomesError;
            }

            _celestialBodies = new List<ICelestialBody>();
        }

        protected void UnregisterAllCelestialSpaces()
        {
            //First unsubscibe events to prevent any memory leaks.
            foreach (ICelestialSpace celestialSpace in _celestialSpaces)
            {
                celestialSpace.OnCelestialSpaceLoaded -= CelestialSpace_OnCelestialSpaceLoaded;
                celestialSpace.OnCelestialSpaceSaved -= CelestialSpace_OnCelestialSpaceSaved;
                celestialSpace.OnCelestialSpaceError -= CelestialSpace_OnCelestialSpaceError;
                celestialSpace.OnCelestialSpacesLoaded -= CelestialSpace_OnCelestialSpacesLoaded;
                celestialSpace.OnCelestialSpacesSaved -= CelestialSpace_OnCelestialSpacesSaved;
                celestialSpace.OnCelestialSpacesError -= CelestialSpace_OnCelestialSpacesError;
                celestialSpace.OnCelestialBodyLoaded -= CelestialSpace_OnCelestialBodyLoaded;
                celestialSpace.OnCelestialBodySaved -= CelestialSpace_OnCelestialBodySaved;
                celestialSpace.OnCelestialBodyError -= CelestialSpace_OnCelestialBodyError;
                celestialSpace.OnCelestialBodiesLoaded -= CelestialSpace_OnCelestialBodiesLoaded;
                celestialSpace.OnCelestialBodiesSaved -= CelestialSpace_OnCelestialBodiesSaved;
                celestialSpace.OnCelestialBodiesError -= CelestialSpace_OnCelestialBodiesError;
                celestialSpace.OnHolonLoaded -= CelestialSpace_OnHolonLoaded;
                celestialSpace.OnHolonSaved -= CelestialSpace_OnHolonSaved;
                celestialSpace.OnHolonError -= CelestialSpace_OnHolonError;
                celestialSpace.OnHolonsLoaded -= CelestialSpace_OnHolonsLoaded;
                celestialSpace.OnHolonsSaved -= CelestialSpace_OnHolonsSaved;
                celestialSpace.OnHolonsError -= CelestialSpace_OnHolonsError;
                celestialSpace.OnZomeLoaded -= CelestialSpace_OnZomeLoaded;
                celestialSpace.OnZomeSaved -= CelestialSpace_OnZomeSaved;
                celestialSpace.OnZomeError -= CelestialSpace_OnZomeError;
                celestialSpace.OnZomesLoaded -= CelestialSpace_OnZomesLoaded;
                celestialSpace.OnZomesSaved -= CelestialSpace_OnZomesSaved;
                celestialSpace.OnZomesError -= CelestialSpace_OnZomesError;
            }

            _celestialSpaces = new List<ICelestialSpace>();
        }

        private IStar GetCelestialSpaceNearestStar(OASISResult<ICelestialBodiesAndSpaces> result, string errorMessage)
        {
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = CreateCelestialSpacesResult(result), Exception = result.Exception });
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = CreateCelestialBodiesResult(result), Exception = result.Exception });
            }

            return star;
        }

        private IStar GetCelestialSpaceNearestStar<T1, T2>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result, string errorMessage) where T1 : ICelestialBody where T2 : ICelestialSpace
        {
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = CreateCelestialSpacesResult(result), Exception = result.Exception });
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = CreateCelestialBodiesResult(result), Exception = result.Exception });
            }

            return star;
        }

        private IStar GetCelestialSpaceNearestStar<T>(OASISResult<IEnumerable<T>> result, string errorMessage) where T : IHolon
        {
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}. Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T, ICelestialSpace>(result), Exception = result.Exception });
            }

            return star;
        }

        private IStar GetCelestialSpaceNearestStar<T>(OASISResult<T> result, string errorMessage) where T : IHolon
        {
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T, ICelestialSpace>(result), Exception = result.Exception });
            }

            return star;
        }

        private IStar GetCelestialSpaceNearestStar()
        {
            switch (this.HolonType)
            {
                case HolonType.Omniverse:
                    NearestStar = ParentGreatGrandSuperStar;
                    break;

                case HolonType.Multiverse:
                case HolonType.Universe:
                    NearestStar = ParentGrandSuperStar;
                    break;

                case HolonType.Galaxy:
                case HolonType.GalaxyCluster:
                    NearestStar = ParentSuperStar;
                    break;

                case HolonType.SolarSystem:
                    NearestStar = ParentStar;
                    break;

                    //case HolonType.Omniverse:
                    //    NearestStar = ParentGreatGrandSuperStar != null ? ParentGreatGrandSuperStar : STAR.DefaultGreatGrandSuperStar;
                    //    break;

                    //case HolonType.Multiverse:
                    //case HolonType.Universe:
                    //    NearestStar = ParentGrandSuperStar != null ? ParentGrandSuperStar : STAR.DefaultGrandSuperStar;
                    //    break;

                    //case HolonType.Galaxy:
                    //case HolonType.GalaxyCluster:
                    //    NearestStar = ParentSuperStar != null ? ParentSuperStar : STAR.DefaultSuperStar;
                    //    break;

                    //case HolonType.SolarSystem:
                    //    NearestStar = ParentStar != null ? ParentStar : STAR.DefaultStar;
                    //    break;
            }

            //If we could not find the nearest star then we keep going up the chain of stars (STARNET/STARCHAIN) till we find one! ;-)
            if (NearestStar == null)
            {
                if (this.ParentStar != null)
                    NearestStar = ParentStar;

                else if (this.ParentSuperStar != null)
                    NearestStar = ParentSuperStar;

                else if (this.ParentGrandSuperStar != null)
                    NearestStar = ParentGrandSuperStar;

                else if (this.ParentGreatGrandSuperStar != null)
                    NearestStar = ParentGreatGrandSuperStar;

                else
                    NearestStar = STAR.DefaultGreatGrandSuperStar; //This is Godhead/Source (there is only ever one and is always avaiable to everyone! ;-) )
            }

            return NearestStar;
        }

        private OASISResult<T1> HandleLoadCelestialSpace<T1, T2>(OASISResult<T1> result, OASISResult<T2> holonResult, string methodName) where T1 : IHolon where T2 : IHolon
        {
            result = OASISResultHelper.CopyResult<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.Result.Children != null)
                {
                    GetCelestialBodies(result.Result.Children);
                    GetCelestialSpaces(result.Result.Children);
                }

                OnCelestialSpaceLoaded?.Invoke(this, new CelestialSpaceLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T1, ICelestialSpace>(result) });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T1, ICelestialSpace>(result), Exception = result.Exception });
            }

            return result;
        }


        private OASISResult<IEnumerable<T1>> HandleLoadCelestialSpaces<T1, T2>(OASISResult<IEnumerable<T1>> result, OASISResult<IEnumerable<T2>> holonResult, string methodName) where T1 : IHolon where T2 : IHolon
        {
            result = OASISResultHelper.CopyResult<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
            {
                GetCelestialSpaces(result.Result);
                OnCelestialSpacesLoaded?.Invoke(this, new CelestialSpacesLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T1, ICelestialSpace>(result) });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial bodies for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T1, ICelestialSpace>(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<IEnumerable<T1>> HandleLoadCelestialBodies<T1, T2>(OASISResult<IEnumerable<T1>> result, OASISResult<IEnumerable<T2>> holonResult, string methodName) where T1 : IHolon where T2 : IHolon
        {
            result = OASISResultHelper.CopyResult<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
            {
                GetCelestialBodies(result.Result);
                OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T1, ICelestialBody>(result) });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T1, ICelestialBody>(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces> HandleLoadCelestialBodiesAndSpaces<T>(OASISResult<ICelestialBodiesAndSpaces> result, OASISResult<IEnumerable<T>> holonResult, string methodName) where T : IHolon
        {
            result = MapCelestialBodieAndSpacessResult(holonResult, result);
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = CreateCelestialBodiesResult(result);
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacessResult = CreateCelestialSpacesResult(result);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = result.Result.CelestialBodies.ToList();
                _celestialSpaces = result.Result.CelestialSpaces.ToList();

                OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = celesialBodiesResult });
                OnCelestialSpacesLoaded?.Invoke(this, new CelestialSpacesLoadedEventArgs() { Result = celesialSpacessResult });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialBodiesResult, Exception = result.Exception });
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialSpacessResult, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces<T1, T2>> HandleLoadCelestialBodiesAndSpaces<T1, T2, T3>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result, OASISResult<IEnumerable<T3>> holonResult, string methodName) where T1 : ICelestialBody where T2 : ICelestialSpace where T3 : IHolon
        {
            result = MapCelestialBodieAndSpacessResult(holonResult, result);
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = CreateCelestialBodiesResult(result);
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacessResult = CreateCelestialSpacesResult(result);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = Mapper.Convert<T1, ICelestialBody>(result.Result.CelestialBodies).ToList();
                _celestialSpaces = Mapper.Convert<T2, ICelestialSpace>(result.Result.CelestialSpaces).ToList();

                OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = celesialBodiesResult });
                OnCelestialSpacesLoaded?.Invoke(this, new CelestialSpacesLoadedEventArgs() { Result = celesialSpacessResult });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialBodiesResult, Exception = result.Exception });
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialSpacessResult, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<T1> HandleSaveCelestialSpace<T1, T2>(OASISResult<T1> result, OASISResult<T2> holonResult, string methodName) where T1 : IHolon where T2 : IHolon
        {
            string errorMessage = $"An errror occured in CelestialSpace.{methodName} whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason:";

            try
            {
                result = OASISResultHelper.CopyResult<T2, T1>(holonResult);

                if (result != null && !result.IsError && result.Result != null)
                    OnCelestialSpaceLoaded?.Invoke(this, new CelestialSpaceLoadedEventArgs() { Result = OASISResultHelper.CopyResult<T1, ICelestialSpace>(result) });
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T1, ICelestialSpace>(result), Exception = result.Exception });
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex}", ex);
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T1, ICelestialSpace>(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<IEnumerable<T1>> HandleSaveCelestialBodies<T1, T2>(OASISResult<IEnumerable<T1>> result, OASISResult<IEnumerable<T2>> holonResult, string methodName) where T1 : IHolon where T2 : IHolon
        {
            result = OASISResultHelper.CopyResult<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
                OnCelestialBodiesSaved?.Invoke(this, new CelestialBodiesSavedEventArgs() { Result = OASISResultHelper.CopyResult<T1, ICelestialBody>(result) });
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T1, ICelestialBody>(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<IEnumerable<T1>> HandleSaveCelestialSpaces<T1, T2>(OASISResult<IEnumerable<T1>> result, OASISResult<IEnumerable<T2>> holonResult, string methodName) where T1 : IHolon where T2 : IHolon
        {
            result = OASISResultHelper.CopyResult<T2, T1>(holonResult);

            if (result != null && !result.IsError && result.Result != null)
                OnCelestialSpacesSaved?.Invoke(this, new CelestialSpacesSavedEventArgs() { Result = OASISResultHelper.CopyResult<T1, ICelestialSpace>(result) });
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst loading the celestial spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = OASISResultHelper.CopyResult<T1, ICelestialBody>(result), Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces> HandleSaveCelestialBodiesAndSpaces<T>(OASISResult<ICelestialBodiesAndSpaces> result, OASISResult<IEnumerable<T>> holonResult, string methodName) where T : IHolon
        {
            result = MapCelestialBodieAndSpacessResult(holonResult, result);
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = CreateCelestialBodiesResult(result);
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacessResult = CreateCelestialSpacesResult(result);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = result.Result.CelestialBodies.ToList();
                _celestialSpaces = result.Result.CelestialSpaces.ToList();

                OnCelestialBodiesSaved?.Invoke(this, new CelestialBodiesSavedEventArgs() { Result = celesialBodiesResult });
                OnCelestialSpacesSaved?.Invoke(this, new CelestialSpacesSavedEventArgs() { Result = celesialSpacessResult });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialBodiesResult, Exception = result.Exception });
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialSpacessResult, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces<T1, T2>> HandleSaveCelestialBodiesAndSpaces<T1, T2, T3>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result, OASISResult<IEnumerable<T3>> holonResult, string methodName) where T1 : ICelestialBody where T2 : ICelestialSpace where T3 : IHolon
        {
            result = MapCelestialBodieAndSpacessResult(holonResult, result);
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = CreateCelestialBodiesResult(result);
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacessResult = CreateCelestialSpacesResult(result);

            if (result != null && !result.IsError && result.Result != null)
            {
                _celestialBodies = Mapper.Convert<T1, ICelestialBody>(result.Result.CelestialBodies).ToList();
                _celestialSpaces = Mapper.Convert<T2, ICelestialSpace>(result.Result.CelestialSpaces).ToList();

                OnCelestialBodiesSaved?.Invoke(this, new CelestialBodiesSavedEventArgs() { Result = celesialBodiesResult });
                OnCelestialSpacesSaved?.Invoke(this, new CelestialSpacesSavedEventArgs() { Result = celesialSpacessResult });
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"An errror occured in CelestialSpace.{methodName} whilst saving the celestial bodies and spaces for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Reason: {holonResult.Message}");
                OnCelestialBodiesError?.Invoke(this, new CelestialBodiesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialBodiesResult, Exception = result.Exception });
                OnCelestialSpacesError?.Invoke(this, new CelestialSpacesErrorEventArgs() { Reason = $"{result.Message}", Result = celesialSpacessResult, Exception = result.Exception });
            }

            return result;
        }

        private OASISResult<ICelestialBodiesAndSpaces> MapCelestialBodieAndSpacessResult<T>(OASISResult<IEnumerable<T>> holonResult, OASISResult<ICelestialBodiesAndSpaces> result) where T : IHolon
        {
            OASISResult<ICelestialBodiesAndSpaces> celesialBodiesAndSpacesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T>, ICelestialBodiesAndSpaces>(holonResult);
            celesialBodiesAndSpacesResult.Result.CelestialSpaces = GetCelestialSpaces(holonResult.Result);
            celesialBodiesAndSpacesResult.Result.CelestialBodies = GetCelestialBodies(holonResult.Result);
            return celesialBodiesAndSpacesResult;
        }

        private OASISResult<ICelestialBodiesAndSpaces<T1, T2>> MapCelestialBodieAndSpacessResult<T1, T2, T3>(OASISResult<IEnumerable<T3>> holonResult, OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result) where T1 : ICelestialBody where T2 : ICelestialSpace where T3 : IHolon
        {
            OASISResult<ICelestialBodiesAndSpaces<T1, T2>> celesialBodiesAndSpacesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<T3>, ICelestialBodiesAndSpaces<T1, T2>>(holonResult);
            celesialBodiesAndSpacesResult.Result.CelestialSpaces = Mapper.Convert<T2>(GetCelestialSpaces(holonResult.Result));
            celesialBodiesAndSpacesResult.Result.CelestialBodies = Mapper.Convert<T1>(GetCelestialSpaces(holonResult.Result));
            return celesialBodiesAndSpacesResult;
        }

        private OASISResult<IEnumerable<ICelestialBody>> CreateCelestialBodiesResult(OASISResult<ICelestialBodiesAndSpaces> result)
        {
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces, IEnumerable<ICelestialBody>>(result);
            celesialBodiesResult.Result = result.Result.CelestialBodies;
            return celesialBodiesResult;
        }

        private OASISResult<IEnumerable<ICelestialBody>> CreateCelestialBodiesResult<T1, T2>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result) where T1 : ICelestialBody where T2 : ICelestialSpace
        {
            OASISResult<IEnumerable<ICelestialBody>> celesialBodiesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces<T1, T2>, IEnumerable<ICelestialBody>>(result);
            celesialBodiesResult.Result = Mapper.MapBaseHolonProperties(result.Result.CelestialBodies, celesialBodiesResult.Result);
            return celesialBodiesResult;
        }

        private OASISResult<IEnumerable<ICelestialSpace>> CreateCelestialSpacesResult(OASISResult<ICelestialBodiesAndSpaces> result)
        {
            OASISResult<IEnumerable<ICelestialSpace>> celesialSpacesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces, IEnumerable<ICelestialSpace>>(result);
            celesialSpacesResult.Result = result.Result.CelestialSpaces;
            return celesialSpacesResult;
        }

        private OASISResult<IEnumerable<ICelestialSpace>> CreateCelestialSpacesResult<T1, T2>(OASISResult<ICelestialBodiesAndSpaces<T1, T2>> result) where T1 : ICelestialBody where T2 : ICelestialSpace
        {
            OASISResult<IEnumerable<ICelestialSpace>> celesialBodiesResult = OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<ICelestialBodiesAndSpaces<T1, T2>, IEnumerable<ICelestialSpace>>(result);
            celesialBodiesResult.Result = Mapper.MapBaseHolonProperties(result.Result.CelestialBodies, celesialBodiesResult.Result);
            return celesialBodiesResult;
        }

        private IEnumerable<ICelestialBody> GetCelestialBodies<T>(IEnumerable<T> childHolons) where T : IHolon
        {
            List<ICelestialBody> celestialBodies = new List<ICelestialBody>();

            foreach (IHolon child in childHolons)
            {
                switch (child.HolonType)
                {
                    case HolonType.Comet:
                    case HolonType.Asteroid:
                    case HolonType.BlackHole:
                    case HolonType.Moon:
                    case HolonType.Planet:
                    case HolonType.Star:
                    case HolonType.SuperStar:
                    case HolonType.GrandSuperStar:
                    case HolonType.GreatGrandSuperStar:
                    case HolonType.Meteroid:
                        celestialBodies.Add((ICelestialBody)child);
                        //this.CelestialBodies.Add((ICelestialBody)child);
                        break;
                }
            }

            return celestialBodies;
        }

        private IEnumerable<ICelestialSpace> GetCelestialSpaces<T>(IEnumerable<T> childHolons) where T : IHolon
        {
            List<ICelestialSpace> celestialSpaces = new List<ICelestialSpace>();

            foreach (IHolon child in childHolons)
            {
                switch (child.HolonType)
                {
                    case HolonType.CosmicRay:
                    case HolonType.CosmicWave:
                    case HolonType.Dimension:
                    case HolonType.Galaxy:
                    case HolonType.GalaxyCluster:
                    case HolonType.GravitationalWave:
                    case HolonType.Multiverse:
                    case HolonType.Nebula:
                    case HolonType.Omniverse:
                    case HolonType.Portal:
                    case HolonType.SolarSystem:
                    case HolonType.SpaceTimeAbnormally:
                    case HolonType.SpaceTimeDistortion:
                    case HolonType.StarDust:
                    case HolonType.SuperVerse:
                    case HolonType.TemporalRift:
                    case HolonType.Universe:
                    case HolonType.WormHole:
                        celestialSpaces.Add((ICelestialSpace)child);
                        //this.CelestialSpaces.Add((ICelestialSpace)child);
                        break;
                }
            }

            return celestialSpaces;
        }

        private void CelestialSpace_OnCelestialSpaceLoaded(object sender, CelestialSpaceLoadedEventArgs e)
        {
            OnCelestialSpaceLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpaceSaved(object sender, CelestialSpaceSavedEventArgs e)
        {
            OnCelestialSpaceSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpaceError(object sender, CelestialSpaceErrorEventArgs e)
        {
            OnCelestialSpaceError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpacesLoaded(object sender, CelestialSpacesLoadedEventArgs e)
        {
            OnCelestialSpacesLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpacesSaved(object sender, CelestialSpacesSavedEventArgs e)
        {
            OnCelestialSpacesSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialSpacesError(object sender, CelestialSpacesErrorEventArgs e)
        {
            OnCelestialSpacesError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodyLoaded(object sender, CelestialBodyLoadedEventArgs e)
        {
            OnCelestialBodyLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            OnCelestialBodySaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            OnCelestialBodyError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodiesLoaded(object sender, CelestialBodiesLoadedEventArgs e)
        {
            OnCelestialBodiesLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodiesSaved(object sender, CelestialBodiesSavedEventArgs e)
        {
            OnCelestialBodiesSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnCelestialBodiesError(object sender, CelestialBodiesErrorEventArgs e)
        {
            OnCelestialBodiesError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            OnZomeLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            OnZomesSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            OnZomesError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            OnHolonsSaved?.Invoke(sender, e);
        }

        private void CelestialSpace_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            OnHolonsError?.Invoke(sender, e);
        }

        private void CelestialBody_OnCelestialBodyLoaded(object sender, CelestialBodyLoadedEventArgs e)
        {
            OnCelestialBodyLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            OnCelestialBodySaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            OnCelestialBodyError?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            OnZomeLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            OnZomesSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            OnZomesError?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            OnHolonsSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            OnHolonsError?.Invoke(sender, e);
        }

        /*
        private async Task<OASISResult<ICelestialSpace>> LoadSaveCelestialBodies<T>(LoadSaveCelestialBodiesEnum loadsave, bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new()
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            OASISResult<ICelestialBody> celestialBodyResult = null;

            foreach (ICelestialBody celestialBody in CelestialBodies)
            {
                if (loadsave == LoadSaveCelestialBodiesEnum.Load)
                    celestialBodyResult = await celestialBody.LoadAsync(saveChildren, continueOnError);
                else
                    celestialBodyResult = await celestialBody.SaveAsync(saveChildren, continueOnError);

                if (!(celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError))
                {
                    result.ErrorCount++;
                    string message = $"There was an error whilst saving the CelestialBody {celestialBody.Name} of type {Enum.GetName(typeof(HolonType), celestialBody.HolonType)}. Reason: {celestialBodyResult.Message}";
                    result.InnerMessages.Add(message);
                    OASISErrorHandling.HandleWarning(ref celestialBodyResult, message);

                    if (!continueOnError)
                        break;
                }
                else
                    result.SavedCount++;
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured saving {CelestialBodies.Count} CelestialBodies in the CelestialSpace {this.Name} of type {Enum.GetName(typeof(HolonType), this.HolonType)}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.SavedCount == 0)
                    OASISErrorHandling.HandleError(ref result, message);
                else
                {
                    OASISErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }
            }
            else
                result.IsSaved = true;

            //base.OnCelestialHolonSaved?.Invoke(this, new System.EventArgs());
            return result;
        }*/

        //private void HandleResult()
        //{
        //    if (!(celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError))
        //    {
        //        result.ErrorCount++;
        //        string message = $"There was an error whilst saving the CelestialBody {celestialBody.Name} of type {Enum.GetName(typeof(HolonType), celestialBody.HolonType)}. Reason: {celestialBodyResult.Message}";
        //        result.InnerMessages.Add(message);
        //        OASISErrorHandling.HandleWarning(ref celestialBodyResult, message);

        //        if (!continueOnError)
        //            break;
        //    }
        //    else
        //        result.SavedCount++;
        //}
    }
}