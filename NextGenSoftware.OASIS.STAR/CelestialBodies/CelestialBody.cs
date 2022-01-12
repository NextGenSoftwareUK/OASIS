using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
//using Mapster;
using NextGenSoftware.OASIS.API.Core.Events;
using static NextGenSoftware.OASIS.API.Core.Events.Events;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.Holons;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract class CelestialBody : CelestialHolon, ICelestialBody
    {
        public ICelestialBodyCore CelestialBodyCore { get; set; } // This is the core zome of the star/planet/moon/etc (OAPP), which links to all the other stars/planets/moons/etc/zomes/holons...
                                                                  // public GenesisType GenesisType { get; set; }
                                                                  //public OASISAPIManager OASISAPI = new OASISAPIManager(new List<IOASISProvider>() { new SEEDSOASIS() });
        public event CelestialBodyLoaded OnCelestialBodyLoaded;
        public event CelestialBodySaved OnCelestialBodySaved;
        public event CelestialBodyError OnCelestialBodyError;
        public event ZomeLoaded OnZomeLoaded;
        public event ZomeSaved OnZomeSaved;
        public event ZomeAdded OnZomeAdded;
        public event ZomeRemoved OnZomeRemoved;
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

        public int Mass { get; set; }
        public int Density { get; set; }
        public int RotationPeriod { get; set; } //How long it takes to rotate on its axis.
        public int OrbitPeriod { get; set; } //How long it takes to orbit its ParentStar.
        public int Weight { get; set; }
        public int GravitaionalPull { get; set; }
        public int OrbitPositionFromParentStar { get; set; }
        //public int OrbitPositionFromParentSuperStar { get; set; } //Only applies to SolarSystems. //TODO: Maybe better to make SolarSystem.ParentStar point to the SuperStar it orbits rather than the Star at the centre of it?
        public int CurrentOrbitAngleOfParentStar { get; set; } //Angle between 0 and 360 degrees of how far around the orbit it it of its parent star.
        public int DistanceFromParentStarInMetres { get; set; }
        public int RotationSpeed { get; set; }
        public int TiltAngle { get; set; }
        public int NumberRegisteredAvatars { get; set; }
        public int NunmerActiveAvatars { get; set; }

        public CelestialBody(HolonType holonType) : base(holonType)
        {
            Initialize();
        }

        public CelestialBody(Guid id, HolonType holonType) : base(id, holonType)
        {
            Initialize();
        }

        public CelestialBody(Dictionary<ProviderType, string> providerKey, HolonType holonType) : base(providerKey, holonType)
        {
            Initialize();
        }

        public async Task<OASISResult<ICelestialBody>> LoadAsync(bool loadChildren = true, bool recursive = true, bool continueOnError = true)
        {
            OASISResult<ICelestialBody> result = await CelestialBodyCore.LoadCelestialBodyAsync(loadChildren, recursive, continueOnError);

            if ((result != null && !result.IsError && result.Result != null)
                || ((result == null || result.IsError || result.Result == null) && continueOnError))
            {
                if (result != null && !result.IsError && result.Result != null)
                    Mapper.MapBaseHolonProperties(result.Result, this);
                else
                {
                    // If there was an error then continueOnError must have been set to true.
                    ErrorHandling.HandleWarning(ref result, $"An errror occured in CelestialBody.LoadAsync method whilst loading the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. ContinueOnError is set to true so continuing to attempt to load the celestial body zomes... Reason: {result.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = $"{result.Message}", Result = result });
                }

                if (loadChildren)
                {
                    OASISResult<IEnumerable<IZome>> zomeResult = await LoadZomesAsync(loadChildren, recursive, continueOnError);

                    if (!(zomeResult != null && !zomeResult.IsError && zomeResult.Result != null))
                    {
                        if (result.IsWarning)
                            ErrorHandling.HandleError(ref result, $"The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} failed to load and one or more of it's zomes failed to load. Reason: {zomeResult.Message}");
                        else
                            ErrorHandling.HandleWarning(ref result, $"The {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} loaded fine but one or more of it's zomes failed to load. Reason: {zomeResult.Message}");

                        OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Reason = "Error occured in CelestialBody.LoadAsync method. See Result.Message Property For More Info.", Result = result });
                    }
                }
            }

            OnCelestialBodyLoaded?.Invoke(this, new CelestialBodyLoadedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<ICelestialBody> Load(bool loadChildren = true, bool recursive = true, bool continueOnError = true)
        {
            return LoadAsync(loadChildren, recursive, continueOnError).Result;
        }

        public async Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool recursive = true, bool continueOnError = true)
        {
            OASISResult<IEnumerable<IZome>> result = await CelestialBodyCore.LoadZomesAsync(loadChildren, recursive, continueOnError);
            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool recursive = true, bool continueOnError = true)
        {
            OASISResult<IEnumerable<IZome>> result = CelestialBodyCore.LoadZomes(loadChildren, recursive, continueOnError);
            OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<ICelestialBody>> SaveAsync(bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(this);
            OASISResult<IHolon> celestialBodyHolonResult = new OASISResult<IHolon>();
            OASISResult<IEnumerable<IZome>> zomesResult = null;

            IsSaving = true;

            if (!STAR.IsStarIgnited)
                STAR.ShowStatusMessage(new EventArgs.StarStatusChangedEventArgs() { MessageType = Enums.StarStatusMessageType.Processing, Message = $"Creating CelestialBody {this.Name}..." });

            if (this.Children == null)
                this.Children = new ObservableCollection<IHolon>();

            switch (this.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    {
                        SetParentIdsForGreatGrandSuperStar((IGreatGrandSuperStar)this);

                        //If the parent Omniverse is not already saving (and it's children) then begin saving them now...
                        if (saveChildren && !((IGreatGrandSuperStar)this).ParentOmniverse.IsSaving)
                        {
                            OASISResult<ICelestialSpace> celestialSpaceResult = await ((IGreatGrandSuperStar)this).ParentOmniverse.SaveAsync(saveChildren, recursive, continueOnError);

                            if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                            {
                                ErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (GreatGrandSuperStar) ParentOmniverse. Reason: {celestialSpaceResult.Message}");
                                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                                if (!continueOnError)
                                {
                                    IsSaving = false;
                                    return result;
                                }
                            }
                        }

                        //TODO: NEED TO ADD SAVE METHODS TO EVERY CELESTIALSPACEHOLON, WHICH WILL RECURSIVELY SAVE ALL IT'S CHILDREN, SAME AS CELESTIALBODY
                        //THIS WILL REPLACE ALL THE MANUAL CODE BELOW! ;-) DOH! LOL
                        //if (saveChildren)
                        //{


                        /*
                        holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IMultiverse, Holon>.MapBaseHolonProperties(((IGreatGrandSuperStar)this).ParentOmniverse.Multiverses));

                        if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                        {
                            ErrorHandling.HandleError(ref result, string.Concat("Error occured saving GreatGrandSuperStar.ParentOmniverse.Multiverses. Error Details: ", holonsResult.Message));
                            return result;
                        }

                        //TODO: Finish this later...
                        foreach (IMultiverse multiverse in ParentOmniverse.Multiverses)
                        {
                            await multiverse.SaveAsync(saveChildren, recursive, continueOnError);

                            holonResult = await CelestialBodyCore.SaveHolonAsync(Mapper<IUniverse, Holon>.MapBaseHolonProperties(multiverse.Dimensions.ThirdDimension.UniversePrime));

                            if (!continueOnError && (holonResult.IsError || holonResult.Result == null))
                            {
                                result.IsError = true;
                                result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmniverse.Multiverses[].Dimensions.ThirdDimension.UniversePrime. Error Details: ", holonResult.Message);
                                return result;
                            }

                            holonResult = await CelestialBodyCore.SaveHolonAsync(Mapper<IUniverse, Holon>.MapBaseHolonProperties(multiverse.Dimensions.ThirdDimension.MagicVerse));

                            if (!continueOnError && (holonResult.IsError || holonResult.Result == null))
                            {
                                result.IsError = true;
                                result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmniverse.Multiverses[].Dimensions.ThirdDimension.MagicVerse. Error Details: ", holonResult.Message);
                                return result;
                            }

                            holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IUniverse, Holon>.MapBaseHolonProperties(multiverse.Dimensions.ThirdDimension.ParallelUniverses));

                            if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                            {
                                result.IsError = true;
                                result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmniverse.Multiverses[].Dimensions.ThirdDimension.ParallelUniverses. Error Details: ", holonsResult.Message);
                                return result;
                            }

                            //celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)multiverse.Dimensions.ThirdDimension.ParallelUniverses);

                            //Need to save all other dimensions too... ;-)*/
                        //}
                        // }
                    }
                    break;

                case HolonType.GrandSuperStar:
                    {
                        SetParentIdsForGrandSuperStar(this.ParentGreatGrandSuperStar, (IGrandSuperStar)this);

                        if (saveChildren && !((IGrandSuperStar)this).ParentMultiverse.IsSaving)
                        {
                            OASISResult<ICelestialSpace> celestialSpaceResult = await ((IGrandSuperStar)this).ParentMultiverse.SaveAsync(saveChildren, recursive, continueOnError);

                            if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                            {
                                ErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (GrandSuperStar) ParentMultiverse. Reason: {celestialSpaceResult.Message}");
                                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                                if (!continueOnError)
                                {
                                    IsSaving = false;
                                    return result;
                                }
                            }
                        }

                        /*
                        if (saveChildren)
                        {
                            holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IGalaxyCluster, Holon>.MapBaseHolonProperties(((IGrandSuperStar)this).ParentUniverse.GalaxyClusters));

                            if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                            {
                                result.IsError = true;
                                result.Message = string.Concat("Error occured saving GrandSuperStar.ParentUniverse.GalaxyClusters. Error Details: ", holonsResult.Message);
                                return result;
                            }

                            foreach (IGalaxyCluster galaxyCluster in ((IGrandSuperStar)this).ParentUniverse.GalaxyClusters)
                            {
                                holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IGalaxy, Holon>.MapBaseHolonProperties(galaxyCluster.Galaxies));

                                if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
                                {
                                    result.IsError = true;
                                    result.Message = string.Concat("Error occured saving GrandSuperStar.ParentUniverse.GalaxyClusters[].Galaxies. Error Details: ", holonsResult.Message);
                                    return result;
                                }

                                //TODO: Finish this later...
                            }
                        }*/
                    }
                    break;

                case HolonType.SuperStar:
                    {
                        SetParentIdsForSuperStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, (ISuperStar)this);

                        if (saveChildren && !((ISuperStar)this).ParentGalaxy.IsSaving)
                        {
                            OASISResult<ICelestialSpace> celestialSpaceResult = await ((ISuperStar)this).ParentGalaxy.SaveAsync(saveChildren, recursive, continueOnError);

                            if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                            {
                                ErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (SuperStar) ParentGalaxy. Reason: {celestialSpaceResult.Message}");
                                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                                if (!continueOnError)
                                {
                                    IsSaving = false;
                                    return result;
                                }
                            }
                        }
                    }
                    break;

                case HolonType.Star:
                    {
                        SetParentIdsForStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, (IStar)this);

                        if (saveChildren && !((IStar)this).ParentSolarSystem.IsSaving)
                        {
                            OASISResult<ICelestialSpace> celestialSpaceResult = await ((IStar)this).ParentSolarSystem.SaveAsync(saveChildren, recursive, continueOnError);

                            if (!(celestialSpaceResult != null && !celestialSpaceResult.IsError && celestialSpaceResult.Result != null))
                            {
                                ErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (Star) ParentSolarSystem. Reason: {celestialSpaceResult.Message}");
                                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                                if (!continueOnError)
                                {
                                    IsSaving = false;
                                    return result;
                                }
                            }
                        }
                    }
                    break;

                case HolonType.Planet:
                    {
                        SetParentIdsForPlanet(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, (IPlanet)this);

                        if (saveChildren)
                        {
                            OASISResult<IEnumerable<IMoon>> moonsResult = await ((PlanetCore)this.CelestialBodyCore).SaveMoonsAsync(saveChildren, recursive, continueOnError);

                            if (!(moonsResult != null && !moonsResult.IsError))
                            {
                                ErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} (Planet) Moons. Reason: {moonsResult.Message}");
                                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                                if (!continueOnError)
                                {
                                    IsSaving = false;
                                    return result;
                                }
                            }
                            else
                                result.SavedCount++;
                        }
                    }
                    break;
            }

            if (saveChildren)
            {
                zomesResult = await SaveZomesAsync(saveChildren, recursive, continueOnError);

                if (!(zomesResult != null && !zomesResult.IsError && zomesResult.Result != null))
                {
                    ErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} zomes. Reason: {zomesResult.Message}");
                    OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                    if (!continueOnError)
                    {
                        IsSaving = false;
                        return result;
                    }
                }
                else
                    result.SavedCount++;
            }

            celestialBodyHolonResult = await CelestialBodyCore.SaveCelestialBodyAsync(this, saveChildren, recursive, continueOnError);
            OASISResultHolonToHolonHelper<IHolon, ICelestialBody>.CopyResult(celestialBodyHolonResult, result);

            if (celestialBodyHolonResult != null && !celestialBodyHolonResult.IsError && celestialBodyHolonResult.Result != null)
            {
                //celestialBodyHolonResult.Result.Adapt(this);
                //Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(celestialBodyHolonResult.Result, this);
                result.SavedCount++;
                SetProperties(celestialBodyHolonResult.Result);
            }
            else
            {
                ErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} holon. Reason: {celestialBodyHolonResult.Message}");
                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });
            }

            if (result.WarningCount > 0)
            {
                if (result.SavedCount == 0)
                    ErrorHandling.HandleError(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. All operations failed, please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                else
                {
                    ErrorHandling.HandleWarning(ref result, $"There was {result.WarningCount} error(s) in CelestialBody.SaveAsync method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. {result.SavedCount} operations did save correctly however. Please check the logs and InnerMessages property for more details. Inner Messages: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}");
                    result.IsSaved = true;
                }

                OnCelestialBodyError?.Invoke(this, new CelestialBodyErrorEventArgs() { Result = result });

                if (!STAR.IsStarIgnited)
                    STAR.ShowStatusMessage(new EventArgs.StarStatusChangedEventArgs() { MessageType = Enums.StarStatusMessageType.Error, Message = $"Error Creating CelestialBody {this.Name}. Reason: {result.Message}" });
            }
            else
            {
                result.IsSaved = true;

                if (!STAR.IsStarIgnited)
                    STAR.ShowStatusMessage(new EventArgs.StarStatusChangedEventArgs() { MessageType = Enums.StarStatusMessageType.Success, Message = $"CelestialBody {this.Name} Created." });
            }

            IsSaving = false;
            OnCelestialBodySaved?.Invoke(this, new CelestialBodySavedEventArgs() { Result = result });
            return result;
        }

        //public async Task<OASISResult<ICelestialBody>> SaveAsync<T>(bool saveChildren = true, bool recursive = true, bool continueOnError = true) where T : ICelestialBody, new()
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>(this);
        //    OASISResult<T> celestialBodyHolonResult = new OASISResult<T>();
        //    OASISResult<IEnumerable<IZome>> zomesResult = null;
        //    OASISResult<IEnumerable<IHolon>> holonsResult = null;
        //    OASISResult<IHolon> holonResult = null;

        //    if (this.Children == null)
        //        this.Children = new ObservableCollection<IHolon>();

        //    // Only save if the holon has any changes.
        //    if (!HasHolonChanged())
        //    {
        //        result.Message = "No changes need saving";
        //        return result;
        //    }

        //    //TODO: Don't think we need to save here to get the Id (saves having to save it twice!), we can generate it here instead and set the IsNewHolon property so the providers know it is a new holon (they use to check if the Id had been set).
        //    if (this.Id == Guid.Empty)
        //    {
        //        Id = Guid.NewGuid();
        //        IsNewHolon = true;
        //    }

        //    // TODO: Not sure if ParentStar and ParentPlanet will be set?
        //    switch (this.HolonType)
        //    {
        //        case HolonType.GreatGrandSuperStar:
        //            {
        //                SetParentIdsForGreatGrandSuperStar((IGreatGrandSuperStar)this);

        //                //TODO: NEED TO ADD SAVE METHODS TO EVERY CELESTIALSPACEHOLON, WHICH WILL RECURSIVELY SAVE ALL IT'S CHILDREN, SAME AS CELESTIALBODY
        //                //THIS WILL REPLACE ALL THE MANUAL CODE BELOW! ;-) DOH! LOL
        //                if (saveChildren)
        //                {
        //                    holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IMultiverse, Holon>.MapBaseHolonProperties(((IGreatGrandSuperStar)this).ParentOmniverse.Multiverses));

        //                    if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
        //                    {
        //                        result.IsError = true;
        //                        result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmniverse.Multiverses. Error Details: ", holonsResult.Message);
        //                        return result;
        //                    }

        //                    //TODO: Finish this later...
        //                    foreach (IMultiverse multiverse in ParentOmniverse.Multiverses)
        //                    {
        //                        OASISResult<Universe> universeResult = await CelestialBodyCore.SaveHolonAsync<Universe>(Mapper<IUniverse, Holon>.MapBaseHolonProperties(multiverse.Dimensions.ThirdDimension.UniversePrime));

        //                        if (!continueOnError && (universeResult.IsError || universeResult.Result == null))
        //                        {
        //                            result.IsError = true;
        //                            result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmniverse.Multiverses[].Dimensions.ThirdDimension.UniversePrime. Error Details: ", universeResult.Message);
        //                            return result;
        //                        }

        //                        OASISResult<Universe> magicverseResult = await CelestialBodyCore.SaveHolonAsync<Universe>(Mapper<IUniverse, Holon>.MapBaseHolonProperties(multiverse.Dimensions.ThirdDimension.MagicVerse));

        //                        if (!continueOnError && (magicverseResult.IsError || magicverseResult.Result == null))
        //                        {
        //                            result.IsError = true;
        //                            result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmniverse.Multiverses[].Dimensions.ThirdDimension.MagicVerse. Error Details: ", magicverseResult.Message);
        //                            return result;
        //                        }

        //                        holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IMultiverse, Holon>.MapBaseHolonProperties(((IGreatGrandSuperStar)this).ParentOmniverse.Multiverses));

        //                        if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
        //                        {
        //                            result.IsError = true;
        //                            result.Message = string.Concat("Error occured saving GreatGrandSuperStar.ParentOmniverse.Multiverses[].Dimensions.ThirdDimension.ParallelUniverses. Error Details: ", holonsResult.Message);
        //                            return result;
        //                        }

        //                        //celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)multiverse.Dimensions.ThirdDimension.ParallelUniverses);

        //                        //Need to save all other dimensions too... ;-)
        //                    }
        //                }
        //            }
        //            break;

        //        case HolonType.GrandSuperStar:
        //            {
        //                SetParentIdsForGrandSuperStar(this.ParentGreatGrandSuperStar, (IGrandSuperStar)this);

        //                if (saveChildren)
        //                {
        //                    holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IGalaxyCluster, Holon>.MapBaseHolonProperties(((IGrandSuperStar)this).ParentUniverse.GalaxyClusters));

        //                    if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
        //                    {
        //                        result.IsError = true;
        //                        result.Message = string.Concat("Error occured saving GrandSuperStar.ParentUniverse.GalaxyClusters. Error Details: ", holonsResult.Message);
        //                        return result;
        //                    }

        //                    foreach (IGalaxyCluster galaxyCluster in ((IGrandSuperStar)this).ParentUniverse.GalaxyClusters)
        //                    {
        //                        holonsResult = await CelestialBodyCore.SaveHolonsAsync(Mapper<IGalaxy, Holon>.MapBaseHolonProperties(galaxyCluster.Galaxies));

        //                        if (!continueOnError && (holonsResult.IsError || holonsResult.Result == null))
        //                        {
        //                            result.IsError = true;
        //                            result.Message = string.Concat("Error occured saving GrandSuperStar.ParentUniverse.GalaxyClusters[].Galaxies. Error Details: ", holonsResult.Message);
        //                            return result;
        //                        }

        //                        //TODO: Finish this later...
        //                    }
        //                }
        //            }
        //            break;

        //        case HolonType.SuperStar:
        //            {
        //                SetParentIdsForSuperStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, (ISuperStar)this);

        //                //TODO: Finish this later...
        //                // if (saveChildren)
        //                //   celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)((ISuperStar)this).ParentGalaxy.Stars);
        //            }
        //            break;

        //        case HolonType.Star:
        //            {
        //                SetParentIdsForStar(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, (IStar)this);

        //                //TODO: Finish this later...
        //                //  if (saveChildren)
        //                //     celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)((IStar)this).ParentSolarSystem.Planets);
        //            }
        //            break;

        //        case HolonType.Planet:
        //            {
        //                SetParentIdsForPlanet(this.ParentGreatGrandSuperStar, this.ParentGrandSuperStar, this.ParentSuperStar, this.ParentStar, (IPlanet)this);

        //                //TODO: Finish this later...
        //                // if (saveChildren)
        //                //    celestialBodyChildrenResult = await SaveCelestialBodyChildrenAsync((IEnumerable<IZome>)((IPlanet)this).Moons);
        //            }
        //            break;
        //    }

        //    if (saveChildren)
        //    {
        //        zomesResult = await SaveZomesAsync(saveChildren, recursive, continueOnError);

        //        if (!(zomesResult != null && !zomesResult.IsError && zomesResult.Result != null))
        //        {
        //            ErrorHandling.HandleWarning(ref result, $"There was an error in CelestialBody.SaveAsync<T> method whilst saving the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")} zomes. Reason: {zomesResult.Message}");

        //            if (!continueOnError)
        //                return result;
        //        }
        //    }

        //    // Finally we need to save again so the child holon ids's are stored in the graph...
        //    // TODO: We may not need to do this save again in future since when we load the zome we could lazy load its child holons seperatley from their parentIds.
        //    // But loading the celestialbody with all its child holons will be faster than loading them seperatley (but only if the current OASIS Provider supports this, so far MongoDBOASIS does).

        //    celestialBodyHolonResult = await CelestialBodyCore.SaveCelestialBodyAsync<T>(this);
        //    result.Message = celestialBodyHolonResult.Message;
        //    result.IsSaved = celestialBodyHolonResult.IsSaved;
        //    result.IsError = celestialBodyHolonResult.IsError;

        //    if (!celestialBodyHolonResult.IsError && celestialBodyHolonResult.Result != null)
        //        //celestialBodyHolonResult.Result.Adapt(this);
        //        //Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(celestialBodyHolonResult.Result, this);
        //        SetProperties(celestialBodyHolonResult.Result);

        //    return result;
        //}

        public OASISResult<ICelestialBody> Save(bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            return SaveAsync(saveChildren, recursive, continueOnError).Result; 
        }

        //public OASISResult<ICelestialBody> Save<T>(bool saveChildren = true, bool recursive = true, bool continueOnError = true) where T : ICelestialBody, new()
        //{
        //    return SaveAsync<T>(saveChildren, recursive, continueOnError).Result; 
        //}

        public async Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool loadChildren = true, bool recursive = true, bool continueOnError = true)
        {
            return await CelestialBodyCore.SaveZomesAsync(loadChildren, recursive, continueOnError);
        }

        public OASISResult<IEnumerable<IZome>> SaveZomes(bool loadChildren = true, bool recursive = true, bool continueOnError = true)
        {
            return CelestialBodyCore.SaveZomes(loadChildren, recursive, continueOnError);
        }

        ////TODO: Do we need to use ICelestialBody or IZome here? It will call different Saves depending which we use...
        //public async Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        //{
        //    OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
        //    OASISResult<IZome> zomeResult = new OASISResult<IZome>();

        //    if (this.CelestialBodyCore.Zomes != null)
        //    {
        //        foreach (IZome zome in this.CelestialBodyCore.Zomes)
        //        {
        //            if (zome.HasHolonChanged())
        //            {
        //                zomeResult = await zome.SaveAsync(saveChildren, recursive, continueOnError);

        //                if (zomeResult != null && zomeResult.Result != null && !zomeResult.IsError)
        //                    result.SavedCount++;
        //                else
        //                {
        //                    result.ErrorCount++;
        //                    ErrorHandling.HandleWarning(ref zomeResult, $"There was an error in the CelestialBody.SaveZomes method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {zomeResult.Message}", true, false, false, true, false);
        //                    //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

        //                    if (!continueOnError)
        //                        break;
        //                }
        //            }
        //        }
        //    }

        //    if (result.ErrorCount > 0)
        //    {
        //        string message = $"{result.ErrorCount} Error(s) occured in CelestialBody.SaveZomes method whilst saving {CelestialBodyCore.Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

        //        if (result.SavedCount == 0)
        //            ErrorHandling.HandleError(ref result, message);
        //        else
        //        {
        //            ErrorHandling.HandleWarning(ref result, message);
        //            result.IsSaved = true;
        //        }

        //        OnZomeError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}");
        //    }
        //    else
        //        result.IsSaved = true;

        //    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
        //    return result;
        //}

        //public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        //{
        //    OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
        //    OASISResult<IZome> zomeResult = new OASISResult<IZome>();

        //    if (this.CelestialBodyCore.Zomes != null)
        //    {
        //        foreach (IZome zome in this.CelestialBodyCore.Zomes)
        //        {
        //            if (zome.HasHolonChanged())
        //            {
        //                zomeResult = zome.Save(saveChildren, recursive, continueOnError);

        //                if (zomeResult != null && zomeResult.Result != null && !zomeResult.IsError)
        //                    result.SavedCount++;
        //                else
        //                {
        //                    result.ErrorCount++;
        //                    ErrorHandling.HandleWarning(ref zomeResult, $"There was an error in the CelestialBody.SaveZomes method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {zomeResult.Message}", true, false, false, true, false);
        //                    //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

        //                    if (!continueOnError)
        //                        break;
        //                }
        //            }
        //        }
        //    }

        //    if (result.ErrorCount > 0)
        //    {
        //        string message = $"{result.ErrorCount} Error(s) occured in CelestialBody.SaveZomes method whilst saving {CelestialBodyCore.Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

        //        if (result.SavedCount == 0)
        //            ErrorHandling.HandleError(ref result, message);
        //        else
        //        {
        //            ErrorHandling.HandleWarning(ref result, message);
        //            result.IsSaved = true;
        //        }

        //        OnZomeError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}");
        //    }
        //    else
        //        result.IsSaved = true;

        //    OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
        //    return result;
        //}

        // Build
        public CoronalEjection Flare()
        {
            return new CoronalEjection();
            // return Star.Flare(this);
        }

        // Activate & Launch - Launch & activate the planet (OAPP) by shining the star's light upon it...
        public void Shine()
        {
            // Star.Shine(this);
        }

        // Deactivate the planet (OAPP)
        public void Dim()
        {
            //Star.Dim(this);
        }

        // Deploy the planet (OAPP)
        public void Seed()
        {
            //Star.Seed(this);
        }

        // Run Tests
        public void Twinkle()
        {
            //Star.Twinkle(this);
        }

        // Highlight the Planet (OAPP) in the OAPP Store (StarNET). *Admin Only*
        public void Radiate()
        {
            //Star.Radiate(this);
        }

        // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
        public void Emit()
        {
            // Star.Emit(this);
        }

        // Show stats of the Planet (OAPP).
        public void Reflect()
        {
            // Star.Reflect(this);
        }

        // Upgrade/update a Planet (OAPP).
        public void Evolve()
        {
            //Star.Evolve(this);
        }

        // Import/Export hApp, dApp & others.
        public void Mutate()
        {
            // Star.Mutate(this);
        }

        // Send/Receive Love
        public void Love()
        {
            // Star.Love(this);
        }

        // Reserved For Future Use...
        public void Super()
        {
            //Star.Super(this);
        }

        protected override async Task InitializeAsync()
        {
            InitCelestialBodyCore();
            WireUpEvents();

            if (!IsNewHolon && (Id != Guid.Empty || (ProviderKey != null && ProviderKey.Keys.Count > 0)))
            {
                OASISResult<ICelestialBody> celestialBodyResult = await LoadAsync();

                if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
                    await base.InitializeAsync();
            }
        }

        protected override void Initialize()
        {
            InitCelestialBodyCore();
            WireUpEvents();

            
            if (!IsNewHolon && (Id != Guid.Empty || (ProviderKey != null && ProviderKey.Keys.Count > 0)))
            {
                OASISResult<ICelestialBody> celestialBodyResult = Load();

                if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
                    base.Initialize();
            }
        }

        private void InitCelestialBodyCore()
        {
            // The Id/ProviderKey will be set from LoadCelestialBody/SetProperties.
            switch (this.HolonType)
            {
                case HolonType.Moon:
                    CelestialBodyCore = new MoonCore((IMoon)this);
                    break;

                case HolonType.Planet:
                    CelestialBodyCore = new PlanetCore((IPlanet)this);
                    break;

                case HolonType.Star:
                    CelestialBodyCore = new StarCore((IStar)this);
                    break;

                case HolonType.SuperStar:
                    CelestialBodyCore = new SuperStarCore((ISuperStar)this);
                    break;

                case HolonType.GrandSuperStar:
                    CelestialBodyCore = new GrandSuperStarCore((IGrandSuperStar)this);
                    break;

                case HolonType.GreatGrandSuperStar:
                    CelestialBodyCore = new GreatGrandSuperStarCore((IGreatGrandSuperStar)this);
                    break;
            }

            CelestialBodyCore.Id = this.Id;
            CelestialBodyCore.ProviderKey = this.ProviderKey;
        }

        private void WireUpEvents()
        {
            if (CelestialBodyCore != null)
            {
                ((CelestialBodyCore)CelestialBodyCore).OnHolonLoaded += CelestialBody_OnHolonLoaded;
                ((CelestialBodyCore)CelestialBodyCore).OnHolonSaved += CelestialBodyCore_OnHolonSaved;
                ((CelestialBodyCore)CelestialBodyCore).OnHolonError += CelestialBody_OnHolonError;
                ((CelestialBodyCore)CelestialBodyCore).OnHolonsLoaded += CelestialBodyCore_OnHolonsLoaded;
                ((CelestialBodyCore)CelestialBodyCore).OnHolonsSaved += CelestialBody_OnHolonsSaved;
                ((CelestialBodyCore)CelestialBodyCore).OnHolonsError += CelestialBody_OnHolonsError;
                ((CelestialBodyCore)CelestialBodyCore).OnZomeLoaded += CelestialBody_OnZomeLoaded;
                ((CelestialBodyCore)CelestialBodyCore).OnZomeSaved += CelestialBody_OnZomeSaved;
                ((CelestialBodyCore)CelestialBodyCore).OnZomeAdded += CelestialBody_OnZomeAdded;
                ((CelestialBodyCore)CelestialBodyCore).OnZomeRemoved += CelestialBody_OnZomeRemoved;
                ((CelestialBodyCore)CelestialBodyCore).OnZomeError += CelestialBodyCore_OnZomeError;
                ((CelestialBodyCore)CelestialBodyCore).OnZomesLoaded += CelestialBodyCore_OnZomesLoaded;
                ((CelestialBodyCore)CelestialBodyCore).OnZomesSaved += CelestialBody_OnZomesSaved;
                ((CelestialBodyCore)CelestialBodyCore).OnZomesError += CelestialBody_OnZomesError;
            }
        }

        private void SetProperties(IHolon holon)
        {
            this.Id = holon.Id;
            this.ProviderKey = holon.ProviderKey;
            this.CelestialBodyCore.Id = holon.Id;
            this.CelestialBodyCore.ProviderKey = holon.ProviderKey;
            this.Name = holon.Name;
            this.Description = holon.Description;
            this.HolonType = holon.HolonType;
            this.ParentGreatGrandSuperStar = holon.ParentGreatGrandSuperStar;
            this.ParentGreatGrandSuperStarId = holon.ParentGreatGrandSuperStarId;
            this.ParentGrandSuperStar = holon.ParentGrandSuperStar;
            this.ParentGrandSuperStarId = holon.ParentGrandSuperStarId;
            this.ParentSuperStar = holon.ParentSuperStar;
            this.ParentSuperStarId = holon.ParentSuperStarId;
            this.ParentStar = holon.ParentStar;
            this.ParentStarId = holon.ParentStarId;
            this.ParentPlanet = holon.ParentPlanet;
            this.ParentPlanetId = holon.ParentPlanetId;
            this.ParentMoon = holon.ParentMoon;
            this.ParentMoonId = holon.ParentMoonId;
            this.ParentZome = holon.ParentZome;
            this.ParentZomeId = holon.ParentZomeId;
            this.ParentHolon = holon.ParentHolon;
            this.ParentHolonId = holon.ParentHolonId;
            this.ParentOmniverse = holon.ParentOmniverse;
            this.ParentOmniverseId = holon.ParentOmniverseId;
            this.ParentMultiverse = holon.ParentMultiverse;
            this.ParentMultiverseId = holon.ParentMultiverseId;
            this.ParentUniverse = holon.ParentUniverse;
            this.ParentUniverseId = holon.ParentUniverseId;
            this.ParentGalaxyCluster = holon.ParentGalaxyCluster;
            this.ParentGalaxyClusterId = holon.ParentGalaxyClusterId;
            this.ParentGalaxy = holon.ParentGalaxy;
            this.ParentGalaxyId = holon.ParentGalaxyId;
            this.ParentSolarSystem = holon.ParentSolarSystem;
            this.ParentSolarSystemId = holon.ParentSolarSystemId;
            this.Children = holon.Children;
            this.Nodes = holon.Nodes;
            this.CreatedByAvatar = holon.CreatedByAvatar;
            this.CreatedByAvatarId = holon.CreatedByAvatarId;
            this.CreatedDate = holon.CreatedDate;
            this.ModifiedByAvatar = holon.ModifiedByAvatar;
            this.ModifiedByAvatarId = holon.ModifiedByAvatarId;
            this.ModifiedDate = holon.ModifiedDate;
            this.DeletedByAvatar = holon.DeletedByAvatar;
            this.DeletedByAvatarId = holon.DeletedByAvatarId;
            this.DeletedDate = holon.DeletedDate;
            this.Version = holon.Version;
            this.IsActive = holon.IsActive;
            this.IsChanged = holon.IsChanged;
            this.IsNewHolon = holon.IsNewHolon;
            this.MetaData = holon.MetaData;
            this.ProviderMetaData = holon.ProviderMetaData;
            this.Original = holon.Original;
        }


        // TODO: COME BACK TO SETTING THESE PARENTID'S, NEED TO NOT BE TIRED TO WORK IT ALL OUT! ;-) LOL
        // PLUS NOT EVEN SURE WE NEED TO DO THIS BECAUSE ALL THE ADD METHODS ALREADY SET THE PARENTID'S?!
        // MAYBE THEY CAN ADD ITEMS MANUALLY TO THE COLLECTIONS WITHOUT USING THE CORRECT ADDMETHODS? THIS IS WHERE THIS COULD BE NEEDED?
        private void SetParentIdsForMoon(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet, IMoon moon)
        {
            if (moon.CelestialBodyCore.Zomes != null)
            {
                foreach (Zome zome in moon.CelestialBodyCore.Zomes)
                    ZomeHelper.SetParentIdsForZome(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon, zome);
            }
        }

        private void SetParentIdsForPlanet(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star, IPlanet planet)
        {
            if (greatGrandSuperStar != null)
            {
                planet.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
                planet.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
                planet.ParentGreatGrandSuperStar = greatGrandSuperStar;
                planet.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;
            }

            if (grandSuperStar != null)
            {
                planet.ParentMultiverse = grandSuperStar.ParentMultiverse;
                planet.ParentMultiverseId = grandSuperStar.ParentMultiverseId;
                planet.ParentGrandSuperStar = grandSuperStar;
                planet.ParentGrandSuperStarId = grandSuperStar.Id;
            }

            if (superStar != null)
            {
                planet.ParentDimension = superStar.ParentDimension;
                planet.ParentDimensionId = superStar.ParentDimensionId;
                planet.ParentUniverse = superStar.ParentUniverse;
                planet.ParentUniverseId = superStar.ParentUniverseId;
                planet.ParentGalaxyCluster = superStar.ParentGalaxyCluster;
                planet.ParentGalaxyClusterId = superStar.ParentGalaxyCluster.Id;
                planet.ParentGalaxy = superStar.ParentGalaxy;
                planet.ParentGalaxyId = superStar.ParentGalaxy.Id;
                planet.ParentSuperStar = superStar;
                planet.ParentSuperStarId = superStar.Id;
            }

            if (star != null)
            {
                planet.ParentSolarSystem = star.ParentSolarSystem;
                planet.ParentSolarSystemId = star.ParentSolarSystem.Id;
                planet.ParentStar = star;
                planet.ParentStarId = star.Id;
            }

            if (planet.CelestialBodyCore.Zomes != null)
            {
                foreach (IZome zome in planet.CelestialBodyCore.Zomes)
                    ZomeHelper.SetParentIdsForZome(greatGrandSuperStar, grandSuperStar, superStar, star, planet, null, zome);
            }

            if (planet.Moons != null)
            {
                foreach (IMoon moon in planet.Moons)
                    SetParentIdsForMoon(greatGrandSuperStar, grandSuperStar, superStar, star, planet, moon);
            }
        }

        private void SetParentIdsForStar(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar, IStar star)
        {
            star.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
            star.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
            star.ParentGreatGrandSuperStar = greatGrandSuperStar;
            star.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

            star.ParentUniverse = grandSuperStar.ParentUniverse;
            star.ParentUniverseId = grandSuperStar.ParentUniverseId;
            star.ParentGrandSuperStar = grandSuperStar;
            star.ParentGrandSuperStarId = grandSuperStar.Id;

            star.ParentGalaxy = grandSuperStar.ParentGalaxy;
            star.ParentGalaxyId = grandSuperStar.ParentGalaxy.Id;
            star.ParentSuperStar = superStar;
            star.ParentSuperStarId = superStar.Id;

            if (star.ParentSolarSystem.Planets != null)
            {
                foreach (IPlanet planet in star.ParentSolarSystem.Planets)
                {
                    SetParentIdsForPlanet(greatGrandSuperStar, grandSuperStar, superStar, star, planet);
                }
            }

            //TODO: Do we want to add Zomes to a Star? Maybe?
        }

        private void SetParentIdsForSuperStar(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar, ISuperStar superStar)
        {
            foreach (IStar star in superStar.ParentGalaxy.Stars)
            {
                // Stars outside of a Solar System (does not have any planets)
                SetParentIdsForStar(greatGrandSuperStar, grandSuperStar, superStar, star);
            }

            foreach (ISolarSystem solarSystem in superStar.ParentGalaxy.SolarSystems)
            {
                solarSystem.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
                solarSystem.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
                solarSystem.ParentGreatGrandSuperStar = greatGrandSuperStar;
                solarSystem.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                solarSystem.ParentUniverse = grandSuperStar.ParentUniverse;
                solarSystem.ParentUniverseId = grandSuperStar.ParentUniverseId;
                solarSystem.ParentGrandSuperStar = grandSuperStar;
                solarSystem.ParentGrandSuperStarId = grandSuperStar.Id;

                solarSystem.ParentGalaxy = grandSuperStar.ParentGalaxy;
                solarSystem.ParentGalaxyId = grandSuperStar.ParentGalaxy.Id;
                solarSystem.ParentSuperStar = superStar;
                solarSystem.ParentSuperStarId = superStar.Id;

                SetParentIdsForStar(greatGrandSuperStar, grandSuperStar, superStar, solarSystem.Star);
            }
        }

        private void SetParentIdsForGrandSuperStar(IGreatGrandSuperStar greatGrandSuperStar, IGrandSuperStar grandSuperStar)
        {
            Mapper.MapParentCelestialBodyProperties(greatGrandSuperStar, grandSuperStar);
            grandSuperStar.ParentGreatGrandSuperStar = greatGrandSuperStar;
            grandSuperStar.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.FirstDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.SecondDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.ThirdDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.FourthDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.FifthDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.SixthDimension, grandSuperStar);
            SetParentIdsForMultiverseDimension(grandSuperStar.ParentMultiverse.Dimensions.SeventhDimension, grandSuperStar);
        }

        private void SetParentIdsForMultiverseDimension(IMultiverseDimension dimension, IGrandSuperStar grandSuperStar)
        {
            Mapper.MapParentCelestialBodyProperties(grandSuperStar, dimension);
            Mapper.MapParentCelestialBodyProperties(dimension, dimension.Universe);
            dimension.Universe.ParentDimension = dimension;
            dimension.Universe.ParentDimensionId = dimension.Id;

            foreach (IStar star in dimension.Universe.Stars)
            {
                // Stars that are outside of a Galaxy (do not have a superstar).
                star.ParentUniverse = dimension.Universe;
                star.ParentUniverseId = dimension.Universe.Id;

                SetParentIdsForStar(null, grandSuperStar, null, star);
            }

            foreach (IPlanet planet in dimension.Universe.Planets)
            {
                // Planets that are outside of a Galaxy (do not have a superstar).
                planet.ParentUniverse = dimension.Universe;
                planet.ParentUniverseId = dimension.Universe.Id;

                SetParentIdsForPlanet(null, grandSuperStar, null, null, planet);
            }

            foreach (ISolarSystem solarSystem in dimension.Universe.SolarSystems)
            {
                // SolarSystems that are outside of a Galaxy (do not have a superstar).
                solarSystem.ParentUniverse = dimension.Universe;
                solarSystem.ParentUniverseId = dimension.Universe.Id;

                //TODO: Implement method below:
                //SetParentIdsForSolarSystems(greatGrandSuperStar, grandSuperStar, null, null, solarSystem);
            }

            foreach (INebula nebula in dimension.Universe.Nebulas)
            {
                // SolarSystems that are outside of a Galaxy (do not have a superstar).
                nebula.ParentUniverse = dimension.Universe;
                nebula.ParentUniverseId = dimension.Universe.Id;

                //TODO: Implement method below:
                //SetParentIdsForNebulas(greatGrandSuperStar, grandSuperStar, null, null, nebula);
            }

            //TODO: Add rest of CelestialBodies/Spaces in Universe here...

            foreach (IGalaxyCluster galaxyCluster in dimension.Universe.GalaxyClusters)
            {
                Mapper.MapParentCelestialBodyProperties(dimension.Universe, galaxyCluster);
                galaxyCluster.ParentUniverse = dimension.Universe;
                galaxyCluster.ParentUniverseId = dimension.Universe.Id;

                foreach (IGalaxy galaxy in galaxyCluster.Galaxies)
                {
                    Mapper.MapParentCelestialBodyProperties(galaxyCluster, galaxy);
                    galaxy.ParentGalaxyCluster = galaxyCluster;
                    galaxy.ParentGalaxyClusterId = galaxyCluster.Id;

                    //SetParentIdsForSuperStar(greatGrandSuperStar, grandSuperStar, galaxy.SuperStar);
                    SetParentIdsForSuperStar(null, grandSuperStar, galaxy.SuperStar);
                }
            }

            ThirdDimension thirdDimension = dimension as ThirdDimension;

            if (thirdDimension != null)
            {
                Mapper.MapParentCelestialBodyProperties(grandSuperStar, thirdDimension.MagicVerse);
                //Mapper.MapParentCelestialBodyProperties(grandSuperStar, thirdDimension.UniversePrime);

                foreach (IUniverse universe in thirdDimension.ParallelUniverses)
                    Mapper.MapParentCelestialBodyProperties(grandSuperStar, universe);
            }
        }

        private void SetParentIdsForGreatGrandSuperStar(IGreatGrandSuperStar greatGrandSuperStar)
        {
            foreach (IMultiverse multiverse in greatGrandSuperStar.ParentOmniverse.Multiverses)
            {
                Mapper.MapParentCelestialBodyProperties(greatGrandSuperStar, multiverse);
                multiverse.ParentGreatGrandSuperStar = greatGrandSuperStar;
                multiverse.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                foreach (IUniverse universe in multiverse.Dimensions.ThirdDimension.ParallelUniverses)
                {
                    universe.ParentOmniverse = greatGrandSuperStar.ParentOmniverse;
                    universe.ParentOmniverseId = greatGrandSuperStar.ParentOmniverseId;
                    universe.ParentGreatGrandSuperStar = greatGrandSuperStar;
                    universe.ParentGreatGrandSuperStarId = greatGrandSuperStar.Id;

                    //SetParentIdsForGrandSuperStar(greatGrandSuperStar, universe.GrandSuperStar);
                    SetParentIdsForGrandSuperStar(greatGrandSuperStar, universe.ParentGrandSuperStar);
                }
            }
        }



        /*
        private async Task<bool> SaveZomesAndHolons()
        {
            foreach (ZomeBase zome in Zomes)
            {
                //TODO: Need to check if any state has changed and only save if it has...
                //await zome.SaveHolonAsync(zome);
                await zome.SaveHolonAsync(this.RustHolonType, zome); //TODO: FIX ASAP!

                foreach (Holon holon in zome.Holons)
                {
                    //TODO: Need to check if any state has changed and only save if it has...
                    await zome.SaveHolonAsync(this.RustHolonType, holon);
                }
            }

            return true;
        }
        */

        public IZome GetZomeThatHolonBelongsTo(IHolon holon)
        {
            return (IZome)CelestialBodyCore.Holons.FirstOrDefault(x => x.Id == holon.Id).ParentHolon;
        }

        public List<IHolon> GetHolonsThatBelongToZome(IZome zome)
        {
            return CelestialBodyCore.Holons.Where(x => x.ParentHolon.Id == zome.Id).ToList();
        }

        public IZome GetZomeByName(string name)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Name == name);
        }

        public IZome GetZomeById(Guid id)
        {
            return CelestialBodyCore.Zomes.FirstOrDefault(x => x.Id == id);
        }
        private void CelestialBody_OnZomeLoaded(object sender, ZomeLoadedEventArgs e)
        {
            OnZomeLoaded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeAdded(object sender, ZomeAddedEventArgs e)
        {
            OnZomeAdded?.Invoke(sender, e);
        }

        private void CelestialBody_OnZomeRemoved(object sender, ZomeRemovedEventArgs e)
        {
            OnZomeRemoved?.Invoke(sender, e);
        }

        private void CelestialBodyCore_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private async void CelestialBodyCore_OnZomesLoaded(object sender, ZomesLoadedEventArgs e)
        {
            OnZomesLoaded?.Invoke(sender, e);

            // TODO: Dont think this is needed now?
            // This was going to load each of the zomes holons once the zomes were loaded for this Planet. 
            // But maybe it is better to allow them be lazy loaded as and when they are needed rather than pulling them all back in one go?
            // Trade offs between the 2 approaches... for now we leave as lazy loading so will only load when they are needed...

            /*
            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                await zome.Initialize(zome.Name, this.HoloNETClient);
                zome.OnHolonLoaded += Zome_OnHolonLoaded;
                zome.OnHolonSaved += Zome_OnHolonSaved;
            }*/

            //TODO: Not sure whether to delegate holons being loaded by zomes if can just load direct from PlanetCore?
            //Nice for Zomes to manage their own collections of holons (good practice) so will see... :)
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

        private async void CelestialBodyCore_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);

            // 10/12/21: OBSOLETE: NO LONGER NEEDED, ZOMES/HOLONS ARE AUTOMATICALLY SAVED WHEN CELESTIALBODY IS (IF SAVECHILDREN PARAM IS SET TO TRUE, OTHERWISE CAN CALL SAVEZOMES LATER TO SAVE ALL ZOMES.
            // 10/12/21: TODO:     MAY ADD ABILITY TO SAVE INDIVIDUAL ZOMES/HOLONS BY EITHER NAME/ID/PROVIDERKEY

            /*
            //TODO: Come back to this...
            return;

            //TODO: Handle error.
            if (!e.Result.IsError)
            {
                if (e.Result.Result.HolonType == HolonType.Planet)
                {
                    // This is the hc Address of the planet (we can use this as the anchor/coreProviderKey to load all future zomes/holons belonging to this planet).
                    this.ProviderKey = e.Result.Result.ProviderKey;

                    //Just in case the zomes/holons have been added since the planet was last saved.
                    foreach (Zome zome in CelestialBodyCore.Zomes)
                    {
                        switch (HolonType)
                        {
                            case HolonType.Star:
                                zome.ParentStar = (IStar)this;
                                zome.ParentStarId = this.Id;
                                break;

                            case HolonType.Planet:
                                zome.ParentPlanet = (IPlanet)this;
                                zome.ParentPlanetId = this.Id;
                                break;

                            case HolonType.Moon:
                                zome.ParentMoon = (IMoon)this;
                                zome.ParentMoonId = this.Id;
                                break;
                        }

                        zome.ParentHolonId = this.Id;
                        zome.ParentHolon = this;

                        // TODO: Need to sort this.Holons collection too (this is a list of ALL holons that belong to ALL zomes for this planet.
                        // So the same holon will be in both collections, just that this.Holons has been flatterned. Why it's Fractal Holonic! ;-)
                        foreach (Holon holon in zome.Holons)
                        {
                            holon.ParentHolon = zome;
                            holon.ParentHolonId = zome.Id;

                            switch (HolonType)
                            {
                                case HolonType.Star:
                                    zome.ParentStar = (IStar)this;
                                    zome.ParentStarId = this.Id;
                                    break;

                                case HolonType.Planet:
                                    zome.ParentPlanet = (IPlanet)this;
                                    zome.ParentPlanetId = this.Id;
                                    break;

                                case HolonType.Moon:
                                    zome.ParentMoon = (IMoon)this;
                                    zome.ParentMoonId = this.Id;
                                    break;
                            }
                        }

                        await zome.SaveHolonsAsync(zome.Holons);
                    }
                }
            }*/
        }

        private void CelestialBody_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(sender, e);
        }

        private void CelestialBodyCore_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
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


        //TODO: Come back to this, this is what is fired when each zome is loaded once the celestialbody is loaded but I think for now we will lazy load them later...
        private void Zome_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);

            // 10/12/21: OBSOLETE: NO LONGER NEEDED, ZOMES ARE AUTOMATICALLY LOADED WHEN CELESTIALBODY IS (IF LOADZOMES PARAM IS SET TO TRUE0, OTHERWISE CAN CALL LOADZOMES LATER TO LOAD ALL ZOMES.
            // 10/12/21: TODO:     MAY ADD ABILITY TO LOAD INDIVIDUAL ZOMES BY EITHER NAME/ID/PROVIDERKEY

            /*
            bool holonFound = false;

            foreach (ZomeBase zome in CelestialBodyCore.Zomes)
            {
                foreach (Holon holon in zome.Holons)
                {
                    if (holon.Id == e.Holon.Id)
                    {
                        holonFound = true;
                        break;
                    }
                }
            }

            // If the zome or holon is not stored in the cache yet then add it now...
            // Currently the collection will fill up as the individual zome loads each holon.
            // They can call the LoadAll function to load all Holons and Zomes linked to this Planet (OAPP).

            //TODO: Now all zomes and holons belonging to a planet (OAPP) are loaded in init method using hc anchor pattern.
            //Maybe it can be a setting to choose between lazy loading (loading only as needed) or to prefetch and load everything up front.
            //Pros and Cons to both methods, Lazy loading = quicker init load time and less memory but then if you start loading lots of zomes/holons after, that's a lot more network traffic, etc.
            //Loading up front- Longer init load time and uses more memory but then all data cached so no more loading or network traffic needed.

            if (!holonFound)
            {
                //IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Name == e.Holon.Parent.Name);
                IZome zome = CelestialBodyCore.Zomes.FirstOrDefault(x => x.Parent.Id == e.Holon.Parent.Id);

                if (zome == null)
                {
                    zome = new Zome(e.Holon.Parent.Id);
                    zome.Holons.Add(e.Holon);
                    CelestialBodyCore.Zomes.Add(zome);
                    //CelestialBodyCore.Zomes.Add(new Zome(HoloNETClient, e.Holon.Parent.Name));
                }

                ((ZomeBase)zome).Holons.Add((Holon)e.Holon);
            }

            OnHolonLoaded?.Invoke(this, e);
            */
        }

        private void Zome_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);

            // CODE BELOW IS OBSOLETE, PROVIDER KEYS AND PARENTS ARE AUTOMATICALLY SET BEFORE SAVING AND JUST AFTER SAVING SO NO NEED FOR BELOW...

            /*
            //TODO: Handle Error.
            if (!e.Result.IsError)
            {
                if (e.Result.Result.HolonType == HolonType.Zome)
                {
                    IZome zome = GetZomeById(e.Result.Result.Id);

                    if (zome != null)
                    {
                        //If the ProviderKey is empty then this is the first time the zome has been saved so we now need to save the zomes holons.
                        //if (string.IsNullOrEmpty(zome.ProviderKey))
                        // {
                        zome.ProviderKey = e.Result.Result.ProviderKey;
                        zome.ParentHolon = e.Result.Result;

                        switch (HolonType)
                        {
                            case HolonType.Star:
                                zome.ParentStar = (IStar)this;
                                zome.ParentStarId = this.Id;
                                break;

                            case HolonType.Planet:
                                zome.ParentPlanet = (IPlanet)this;
                                zome.ParentPlanetId = this.Id;
                                break;

                            case HolonType.Moon:
                                zome.ParentMoon = (IMoon)this;
                                zome.ParentMoonId = this.Id;
                                break;
                        }

                        foreach (Holon holon in GetHolonsThatBelongToZome(zome))
                            zome.SaveHolonAsync(holon);
                    }
                }
                else
                {
                    IHolon holon = CelestialBodyCore.Holons.FirstOrDefault(x => x.Id == e.Result.Result.Id);

                    //TODO: Come back to this... Wouldn't parent already be set? Same for zomes? Need to check...
                    if (holon != null)
                    {
                        holon.ProviderKey = e.Result.Result.ProviderKey;
                        //holon.Parent = e.Holon;
                        //holon.ParentCelestialBody = this;

                        switch (HolonType)
                        {
                            case HolonType.Star:
                                holon.ParentStar = (IStar)this;
                                holon.ParentStarId = this.Id;
                                break;

                            case HolonType.Planet:
                                holon.ParentPlanet = (IPlanet)this;
                                holon.ParentPlanetId = this.Id;
                                break;

                            case HolonType.Moon:
                                holon.ParentMoon = (IMoon)this;
                                holon.ParentMoonId = this.Id;
                                break;
                        }
                    }
                }

                OnHolonSaved?.Invoke(this, e);
            }*/
        }
    }
}