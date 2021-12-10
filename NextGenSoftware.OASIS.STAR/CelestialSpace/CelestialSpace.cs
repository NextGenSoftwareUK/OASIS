using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Events;
using static NextGenSoftware.OASIS.API.Core.Events.Events;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.STAR.Holons;
using NextGenSoftware.OASIS.STAR.CelestialBodies;

namespace NextGenSoftware.OASIS.STAR.CelestialSpace
{
    public abstract class CelestialSpace : CelestialHolon, ICelestialSpace
    {
        public event CelestialSpaceLoaded OnCelestialSpaceLoaded;
        public event CelestialBodiesLoaded OnCelestialBodiesLoaded;
        public event CelestialSpaceSaved OnCelestialSpaceSaved;
        public event CelestialBodiesSaved OnCelestialBodiesSaved;
        public event CelestialSpaceError OnCelestialSpaceError;
        public event CelestialBodyLoaded OnCelestialBodyLoaded;
        public event CelestialBodySaved OnCelestialBodySaved;
        public event CelestialBodyError OnCelestialBodyError;
        public event ZomesLoaded OnZomesLoaded;
        public event ZomeError OnZomeError;
        public event HolonLoaded OnHolonLoaded;
        public event HolonsLoaded OnHolonsLoaded;
        public event HolonSaved OnHolonSaved;
      
        public List<ICelestialBody> CelestialBodies = new List<ICelestialBody>();

        //public enum LoadSaveCelestialBodiesEnum
        //{
        //    Load,
        //    Save
        //}

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
                OASISResult<ICelestialSpace> celestialSpaceResult = Load();

                if (celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                    base.Initialize();

                //if (celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                //{
                //    Mapper.MapBaseHolonProperties(celestialSpaceResult.Result, this);
                //    base.Initialize();
                //}
                //else
                //{
                //    string msg = $"ERROR: Error loading {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}, reason: {celestialSpaceResult.Message}";

                //    if (OnCelestialSpaceError != null)
                //        OnCelestialSpaceError(this, new CelestialSpaceErrorEventArgs() { Reason = msg, Result = celestialSpaceResult });
                //    else
                //        throw new OASISException<ICelestialSpace>(msg, celestialSpaceResult);
                //}
            }
        }

        protected override async Task InitializeAsync()
        {
            WireUpEvents();

            if (Id != Guid.Empty || (ProviderKey != null && ProviderKey.Keys.Count > 0))
            {
                OASISResult<ICelestialSpace> celestialSpaceResult = await LoadAsync();

                if (celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                    await base.InitializeAsync();

                //if (celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                //{
                //    Mapper.MapBaseHolonProperties(celestialSpaceResult.Result, this);
                //    await base.InitializeAsync();
                //}
                //else
                //{
                //    string msg = $"ERROR: Error loading {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}, reason: {celestialSpaceResult.Message}";

                //    if (OnCelestialSpaceError != null)
                //        OnCelestialSpaceError(this, new CelestialSpaceErrorEventArgs() { Reason = msg, Result = celestialSpaceResult });
                //    else
                //        throw new OASISException<ICelestialSpace>(msg, celestialSpaceResult);
                //}
            }
        }

        public async Task<OASISResult<ICelestialSpace>> LoadAsync(bool loadChildren = true, bool continueOnError = true)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                ErrorHandling.HandleError(ref result, $"Error loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = "Error occured in CelestialSpace.LoadAsync method. See Result.Message Property For More Info.", Result = result });
                return result;
            }

            OASISResult<IHolon> holonResult = await star.CelestialBodyCore.LoadHolonAsync(this.Id);

            if ((holonResult != null && !holonResult.IsError && holonResult.Result != null)
                || ((holonResult == null || holonResult.IsError || holonResult.Result == null) && continueOnError))
            {
                if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                    Mapper.MapBaseHolonProperties(holonResult.Result, this);                   
                else
                {
                    // If there was an error then continueOnError must have been set to true.
                    ErrorHandling.HandleWarning(ref result, $"An errror occured loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. ContinueOnError is set to true so continuing to attempt to load the celestial bodies... Reason: {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"Error occured in CelestialSpace.LoadHolonAsync method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace holon")}. See Result.Message Property For More Info.", Result = result });
                }

                if (loadChildren)
                {
                    OASISResult<ICelestialSpace> celestialBodiesResult = await LoadCelestialBodiesAsync();

                    if (!(celestialBodiesResult != null && !celestialBodiesResult.IsError && celestialBodiesResult.Result != null))
                    {
                        if (result.IsWarning)
                            ErrorHandling.HandleError(ref result, $"The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} failed to load and one or more of it's celestialbodies failed to load. Reason: {celestialBodiesResult.Message}");
                        else
                            ErrorHandling.HandleWarning(ref result, $"The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} loaded fine but one or more of it's celestialbodies failed to load. Reason: {celestialBodiesResult.Message}");
                        
                        OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = "Error occured in CelestialSpace.LoadAsync method. See Result.Message Property For More Info.", Result = result });
                    }
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

                if (celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError)
                    result.LoadedCount++;
                else
                {
                    result.ErrorCount++;
                    ErrorHandling.HandleWarning(ref celestialBodyResult, $"There was an error whilst loading the {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")}. Reason: {celestialBodyResult.Message}", true, false, false, true, false);
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"Error occured in CelestialSpace.LoadCelestialBodiesAsync method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace holon")}. See Result.Message Property For More Info.", Result = result });

                    if (!continueOnError)
                        break;
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured loading {CelestialBodies.Count} CelestialBodies in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";                

                if (result.LoadedCount == 0)
                    ErrorHandling.HandleError(ref result, message);
                else
                {
                    ErrorHandling.HandleWarning(ref result, message);
                    result.IsLoaded = true;
                }

                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = "Error occured in CelestialSpace.LoadCelestialBodiesAsync method. See Result.Message Property For More Info.", Result = result });
            }
            else
                result.IsLoaded = true;

            //TODO: Not sure if we should raise this event if an error occured? They can check the IsError and Message properties to check if it was successful but they can also check it in the OnError event...
            OnCelestialBodiesLoaded?.Invoke(this, new CelestialBodiesLoadedEventArgs() { Result = result, CelestialBodies = this.CelestialBodies });
            return result;
        }

        //public override OASISResult<ICelestialSpace> Load(bool loadChildren = true, bool continueOnError = true)
        public OASISResult<ICelestialSpace> LoadCelestialBodies(bool loadChildren = true, bool continueOnError = true)
        {
            return LoadCelestialBodiesAsync(loadChildren, continueOnError).Result;
        }

        public async Task<OASISResult<ICelestialSpace>> SaveAsync(bool saveChildren = true, bool continueOnError = true)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            IStar star = GetCelestialSpaceNearestStar();

            if (star == null)
            {
                ErrorHandling.HandleError(ref result, $"Error saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Could not find the nearest star for the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}.");
                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = "Error occured in CelestialSpace.SaveAsync method. See Result.Message Property For More Info.", Result = result });
                return result;
            }

            OASISResult<IHolon> holonResult = await star.CelestialBodyCore.SaveHolonAsync(this);

            if ((holonResult != null && !holonResult.IsError && holonResult.Result != null)
                || ((holonResult == null || holonResult.IsError || holonResult.Result == null) && continueOnError))
            {
                if (!(holonResult != null && !holonResult.IsError && holonResult.Result != null))
                {
                    // If there was an error then continueOnError must have been set to true.
                    ErrorHandling.HandleWarning(ref result, $"An errror occured saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. ContinueOnError is set to true so continuing to attempt to save the celestial bodies... Reason: {holonResult.Message}");
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"Error occured in CelestialSpace.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace holon")}. See Result.Message Property For More Info.", Result = result });
                }

                if (saveChildren)
                {
                    OASISResult<ICelestialSpace> celestialBodiesResult = await SaveCelestialBodiesAsync(saveChildren, continueOnError);

                    if (!(celestialBodiesResult != null && !celestialBodiesResult.IsError && celestialBodiesResult.Result != null))
                    {
                        if (result.IsWarning)
                            ErrorHandling.HandleError(ref result, $"The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} failed to save and one or more of it's celestialbodies failed to save. Reason: {celestialBodiesResult.Message}");
                        else
                            ErrorHandling.HandleWarning(ref result, $"The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")} saved fine but one or more of it's celestialbodies failed to save. Reason: {celestialBodiesResult.Message}");
                        
                        OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = "Error occured in CelestialSpace.SaveAsync method. See Result.Message Property For More Info.", Result = result });
                    }
                }
            }

            OnCelestialSpaceSaved?.Invoke(this, new CelestialSpaceSavedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<ICelestialSpace> Save(bool saveChildren = true, bool continueOnError = true)
        {
            return SaveAsync(saveChildren, continueOnError).Result;
        }

        public async Task<OASISResult<ICelestialSpace>> SaveCelestialBodiesAsync(bool saveChildren = true, bool continueOnError = true)
        {
            OASISResult<ICelestialSpace> result = new OASISResult<ICelestialSpace>();
            OASISResult<ICelestialBody> celestialBodyResult = null;

            //Save all CelestialBodies contained within this space.
            foreach (ICelestialBody celestialBody in CelestialBodies)
            {
                celestialBodyResult = await celestialBody.SaveAsync(saveChildren, continueOnError);

                if (celestialBodyResult != null && celestialBodyResult.Result != null && !celestialBodyResult.IsError)
                    result.SavedCount++;
                else
                {
                    result.ErrorCount++;
                    ErrorHandling.HandleWarning(ref celestialBodyResult, $"There was an error whilst saving the {LoggingHelper.GetHolonInfoForLogging(celestialBody, "CelestialBody")}. Reason: {celestialBodyResult.Message}", true, false, false, true, false);
                    OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"Error occured in CelestialSpace.SaveCelestialBodiesAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace holon")}. See Result.Message Property For More Info.", Result = result });

                    if (!continueOnError)
                        break;
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured saving {CelestialBodies.Count} CelestialBodies in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";
                
                if (result.SavedCount == 0)
                    ErrorHandling.HandleError(ref result, message);
                else
                {
                    ErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }

                OnCelestialSpaceError?.Invoke(this, new CelestialSpaceErrorEventArgs() { Reason = $"Error occured in CelestialSpace.SaveCelestialBodiesAsync method whilst saving the CelestialBodies for {LoggingHelper.GetHolonInfoForLogging(this, "CelestialSpace holon")}. See Result.Message Property For More Info.", Result = result });
            }
            else
                result.IsSaved = true;

            //TODO: Not sure if we should raise this event if an error occured? They can check the IsError and Message properties to check if it was successful but they can also check it in the OnError event...
            OnCelestialBodiesSaved?.Invoke(this, new CelestialBodiesSavedEventArgs() { Result = result, CelestialBodies = this.CelestialBodies });
            return result;
        }

        public OASISResult<ICelestialSpace> SaveCelestialBodies(bool saveChildren = true, bool continueOnError = true)
        {
            return SaveCelestialBodiesAsync(saveChildren, continueOnError).Result;
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
                celestialBody.OnCelestialBodyLoaded -= CelestialBody_OnCelestialBodyLoaded;
                celestialBody.OnCelestialBodySaved -= CelestialBody_OnCelestialBodySaved;
                celestialBody.OnCelestialBodyError -= CelestialBody_OnCelestialBodyError;
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
                celestialBody.OnCelestialBodyLoaded += CelestialBody_OnCelestialBodyLoaded;
                celestialBody.OnCelestialBodySaved += CelestialBody_OnCelestialBodySaved;
                celestialBody.OnCelestialBodyError += CelestialBody_OnCelestialBodyError;
                celestialBody.OnHolonLoaded += CelestialBody_OnHolonLoaded;
                celestialBody.OnHolonsLoaded += CelestialBody_OnHolonsLoaded;
                celestialBody.OnHolonSaved += CelestialBody_OnHolonSaved;
                celestialBody.OnZomesLoaded += CelestialBody_OnZomesLoaded;
                celestialBody.OnZomeError += CelestialBody_OnZomeError;
            }
        }

        private IStar GetCelestialSpaceNearestStar()
        {
            switch (this.HolonType)
            {
                case HolonType.Omiverse:
                    return ParentGreatGrandSuperStar;

                case HolonType.Multiverse:
                case HolonType.Universe:
                    return ParentGrandSuperStar;

                case HolonType.Galaxy:
                case HolonType.GalaxyCluster:
                    return ParentSuperStar;

                case HolonType.SolarSystem:
                    return ParentStar;

                default:
                    {
                        if (this.ParentStar != null)
                            return ParentStar;

                        else if (this.ParentSuperStar != null)
                            return ParentSuperStar;

                        else if (this.ParentGrandSuperStar != null)
                            return ParentGrandSuperStar;

                        else if (this.ParentGreatGrandSuperStar != null)
                            return ParentGreatGrandSuperStar;
                        
                        return null;
                    }
            }
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

        private void CelestialBody_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
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