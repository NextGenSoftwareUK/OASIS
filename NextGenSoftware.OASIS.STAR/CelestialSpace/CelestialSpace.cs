using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using static NextGenSoftware.OASIS.API.Core.Events.Events;
using NextGenSoftware.OASIS.API.Core.Events;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public abstract class CelestialSpace : CelestialHolon, ICelestialSpace
    {
        public event CelestialSpaceLoaded OnCelestialSpaceLoaded;
        public event CelestialBodiesLoaded OnCelestialBodiesLoaded;
        public event CelestialSpaceSaved OnCelestialSpaceSaved;
        public event CelestialBodiesSaved OnCelestialBodiesSaved;
        public event CelestialSpaceError OnCelestialSpaceError;

        public List<ICelestialBody> CelestialBodies = new List<ICelestialBody>();

        public enum LoadSaveCelestialBodiesEnum
        {
            Load,
            Save
        }

        public CelestialSpace(HolonType holonType) : base(holonType)
        {
            Initialize();  
        }

        public CelestialSpace(Guid id, HolonType holonType) : base(id, holonType)
        {
            Initialize();
        }

        public CelestialSpace(Dictionary<ProviderType, string> providerKey, HolonType holonType) : base(providerKey, holonType)
        {
            Initialize();  
        }

        protected override void Initialize()
        {
            WireUpEvents();

            if (Id != Guid.Empty || (ProviderKey != null && ProviderKey.Keys.Count > 0))
            {
                //LoadCelestialBody();

                OASISResult<ICelestialSpace> celestialSpaceResult = Load();

                if (celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                    Mapper.MapBaseHolonProperties(celestialSpaceResult.Result, this);
                else
                    throw new Exception($"ERROR: Error loading CelesitalSpace, reason: {celestialSpaceResult.Message}"); //TODO: Replace exception with bubbling up OASISResult (needs to be OASIS wide ASAP).

                LoadZomes();


                // OASISResult<IEnumerable<IZome>> zomesResult = LoadZomes();

                //if (zomesResult != null && !zomesResult.IsError && zomesResult.Result != null)
                //    this.CelestialBodyCore.Zomes = (List<IZome>)zomesResult.Result;
                //else
                //    throw new Exception($"ERROR: Error loading CelesitalBody Zomes, reason: {zomesResult.Message}"); //TODO: Replace exception with bubbling up OASISResult (needs to be OASIS wide ASAP).
            }

            base.Initialize();
        }

        protected override async Task InitializeAsync()
        {
            //TODO: Load and Wireup Events like CelestialBody, etc.
            await base.InitializeAsync();
        }

        public async Task<OASISResult<ICelestialSpace>> LoadAsync(bool loadChildren = true, bool continueOnError = true)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            OASISResult<IHolon> holonResult = new OASISResult<IHolon>();

            switch (this.HolonType)
            {
                case HolonType.Omiverse:
                    holonResult = await this.ParentGreatGrandSuperStar.CelestialBodyCore.LoadHolonAsync(this.Id);
                    break;

                case HolonType.Multiverse:
                case HolonType.Universe:
                    holonResult = await this.ParentGrandSuperStar.CelestialBodyCore.LoadHolonAsync(this.Id);
                    break;

                case HolonType.Galaxy:
                case HolonType.GalaxyCluster:
                    holonResult = await this.ParentSuperStar.CelestialBodyCore.LoadHolonAsync(this.Id);
                    break;

                case HolonType.SolarSystem:
                    holonResult = await this.ParentStar.CelestialBodyCore.LoadHolonAsync(this.Id);
                    break;

                default:
                    {
                        if (this.ParentStar != null)
                            holonResult = await this.ParentStar.CelestialBodyCore.LoadHolonAsync(this.Id);

                        else if (this.ParentSuperStar != null)
                            holonResult = await this.ParentSuperStar.CelestialBodyCore.LoadHolonAsync(this.Id);

                        else if (this.ParentGrandSuperStar != null)
                            holonResult = await this.ParentGrandSuperStar.CelestialBodyCore.LoadHolonAsync(this.Id);

                        else if (this.ParentGreatGrandSuperStar != null)
                            holonResult = await this.ParentGreatGrandSuperStar.CelestialBodyCore.LoadHolonAsync(this.Id);
                    }
            }

            if ((holonResult != null && !holonResult.IsError && holonResult.Result != null)
                || ((holonResult == null || holonResult.IsError || holonResult.Result == null) && continueOnError))
            {
                if (loadChildren)
                {
                    OASISResult<ICelestialSpace> celestialBodiesResult = await LoadCelestialBodiesAsync();

                    if (!(celestialBodiesResult != null && !celestialBodiesResult.IsError && celestialBodiesResult.Result != null))
                        ErrorHandling.HandleWarning(ref result, $"The CelestialSpace {this.Name} with type {Enum.GetName(typeof(HolonType), this.HolonType)} loaded fine but one or more of it's celestialbodies failed to load. Reason: {celestialBodiesResult.Message}");
                }
            }

            OnCelestialSpaceLoaded?.Invoke(this, new CelestialSpaceLoadedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<ICelestialSpace> Load(bool loadChildren = true, bool continueOnError = true)
        {
            return LoadAsync(loadChildren, continueOnError).Result;
        }

        //public override async Task<OASISResult<ICelestialSpace>> LoadAsync(bool loadChildren = true, bool continueOnError = true)
        public async Task<OASISResult<ICelestialSpace>> LoadCelestialBodiesAsync(bool loadChildren = true, bool continueOnError = true)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            OASISResult<ICelestialBody> celestialBodyResult = null;

            foreach (ICelestialBody celestialBody in CelestialBodies)
            {
                celestialBodyResult = await celestialBody.LoadAsync(loadChildren, continueOnError);

                if (!(celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError))
                {
                    result.ErrorCount++;
                    string message = $"There was an error whilst loading the CelestialBody {celestialBody.Name} of type {Enum.GetName(typeof(HolonType), celestialBody.HolonType)}. Reason: {celestialBodyResult.Message}";
                    result.InnerMessages.Add(message);
                    ErrorHandling.HandleWarning(ref celestialBodyResult, message);

                    if (!continueOnError)
                        break;
                }
                else
                    result.LoadedCount++;
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured loading {CelestialBodies.Count} CelestialBodies in the CelestialSpace {this.Name} of type {Enum.GetName(typeof(HolonType), this.HolonType)}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.LoadedCount == 0)
                    ErrorHandling.HandleError(ref result, message);
                else
                {
                    ErrorHandling.HandleWarning(ref result, message);
                    result.IsLoaded = true;
                }
            }
            else
                result.IsLoaded = true;

            OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = result, CelestialBodies = this.CelestialBodies });
            return result;
        }

        //public override OASISResult<ICelestialSpace> Load(bool loadChildren = true, bool continueOnError = true)
        public OASISResult<ICelestialSpace> LoadCelestialBodies(bool loadChildren = true, bool continueOnError = true)
        {
            return LoadCelestialBodiesAsync(loadChildren, continueOnError).Result;
        }

        public async Task<OASISResult<ICelestialSpace>> SaveAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new()
        {

        }

        public OASISResult<ICelestialSpace> Save<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new()
        {

        }


        public async Task<OASISResult<ICelestialSpace>> SaveCelestialBodiesAsync<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new()
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            OASISResult<ICelestialBody> celestialBodyResult = null;

            //Save all CelestialBodies contained within this space.
            foreach (ICelestialBody celestialBody in CelestialBodies)
            {
                celestialBodyResult = await celestialBody.SaveAsync(saveChildren, continueOnError);

                if (!(celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError))
                {
                    result.ErrorCount++;
                    string message = $"There was an error whilst saving the CelestialBody {celestialBody.Name} of type {Enum.GetName(typeof(HolonType), celestialBody.HolonType)}. Reason: {celestialBodyResult.Message}";
                    result.InnerMessages.Add(message);
                    ErrorHandling.HandleWarning(ref celestialBodyResult, message);

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
                    ErrorHandling.HandleError(ref result, message);
                else
                {
                    ErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }
            }
            else
                result.IsSaved = true;

            //OnCelestialSpaceSaved?.Invoke(this, new CelestialSpaceSavedEventArgs() { Result = result});
            OnCelestialBodiesSaved?.Invoke(this, new CelestialBodiesSavedEventArgs() { Result = result, CelestialBodies = this.CelestialBodies });
            return result;
        }

        public OASISResult<ICelestialSpace> SaveCelestialBodies<T>(bool saveChildren = true, bool continueOnError = true) where T : ICelestialSpace, new()
        {
            return SaveCelestialBodiesAsync<T>(saveChildren, continueOnError).Result;
        }

        protected void RegisterCelestialBodies(IEnumerable<ICelestialBody> celestialBodies)
        {
            this.CelestialBodies.AddRange(celestialBodies);
            WireUpEvents();
        }

        protected void UnregisterAllCelestialBodies()
        {
            //First unsubscibe events to prevent any memory leaks.
            foreach (CelestialBody celestialBody in this.CelestialBodies)
            {
                celestialBody.OnHolonsLoaded -= CelestialBody_OnHolonsLoaded;
                celestialBody.OnHolonLoaded -= CelestialBody_OnHolonLoaded;
                celestialBody.OnHolonSaved -= CelestialBody_OnHolonSaved;
                celestialBody.OnZomesLoaded -= CelestialBody_OnZomesLoaded;
                celestialBody.OnZomeError -= CelestialBody_OnZomeError;
            }

            this.CelestialBodies = new List<ICelestialBody>(); 
        }

        private void WireUpEvents()
        {
            foreach (CelestialBody celestialBody in this.CelestialBodies)
            {
                celestialBody.OnHolonsLoaded += CelestialBody_OnHolonsLoaded;
                celestialBody.OnHolonLoaded += CelestialBody_OnHolonLoaded;
                celestialBody.OnHolonSaved += CelestialBody_OnHolonSaved;
                celestialBody.OnZomesLoaded += CelestialBody_OnZomesLoaded;
                celestialBody.OnZomeError += CelestialBody_OnZomeError;
            }
        }

        private void CelestialBody_OnZomesLoaded(object sender, API.Core.Events.ZomesLoadedEventArgs e)
        {
           
        }

        private void CelestialBody_OnZomeError(object sender, API.Core.Events.ZomeErrorEventArgs e)
        {
           
        }

        private void CelestialBody_OnHolonSaved(object sender, API.Core.Events.HolonSavedEventArgs e)
        {
            
        }

        private void CelestialBody_OnHolonLoaded(object sender, API.Core.Events.HolonLoadedEventArgs e)
        {
            
        }

        private void CelestialBody_OnHolonsLoaded(object sender, API.Core.Events.HolonsLoadedEventArgs e)
        {
            
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
                    ErrorHandling.HandleWarning(ref celestialBodyResult, message);

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
                    ErrorHandling.HandleError(ref result, message);
                else
                {
                    ErrorHandling.HandleWarning(ref result, message);
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
        //        ErrorHandling.HandleWarning(ref celestialBodyResult, message);

        //        if (!continueOnError)
        //            break;
        //    }
        //    else
        //        result.SavedCount++;
        //}
    }
}