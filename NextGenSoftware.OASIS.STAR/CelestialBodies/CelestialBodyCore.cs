using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.Zomes;
using static NextGenSoftware.OASIS.API.Core.Events.Events;

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract class CelestialBodyCore : ZomeBase, ICelestialBodyCore
    {
        //public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        ////  public event HolonsLoaded OnHolonsLoaded;
        //public event Events.HolonsLoaded OnHolonsLoaded;

        //public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        ////public event ZomesLoaded OnZomesLoaded;
        //public event Events.ZomesLoaded OnZomesLoaded;

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

        public List<IZome> Zomes { get; set; } = new List<IZome>();

        public new IEnumerable<IHolon> Holons
        {
            get
            {
                if (Zomes != null)
                {
                    List<IHolon> holons = new List<IHolon>();

                    foreach (IZome zome in Zomes)
                        holons.Add(zome);

                    //Now we need to add the base holons that are linked directly to the celestialbody.
                    holons.AddRange(base.Holons);
                    return holons;
                }

                return null;
            }
        }

        public CelestialBodyCore(Dictionary<ProviderType, string> providerKey) : base()
        {
            this.ProviderKey = providerKey;
        }

        public CelestialBodyCore(Guid id) : base()
        {
            this.Id = id;
        }

        public CelestialBodyCore() : base()
        {
        }

        public async Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync(bool loadChildren = true, bool resursive = true, bool continueOnError = true)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IEnumerable<IHolon>>  holonResult = await base.LoadHolonsForParentAsync(HolonType.Zome, loadChildren, resursive, continueOnError);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonResult, ref result);

            if (holonResult.Result != null && !holonResult.IsError)
            {
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonResult.Result);
                this.Zomes = (List<IZome>)result.Result;
                OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = result });
            }
            else
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

            return result;
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes(bool loadChildren = true, bool resursive = true, bool continueOnError = true)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IEnumerable<IHolon>> holonResult = base.LoadHolonsForParent(HolonType.Zome, loadChildren, resursive, continueOnError);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonResult, ref result);

            if (holonResult.Result != null && !holonResult.IsError)
            {
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonResult.Result);
                this.Zomes = (List<IZome>)result.Result;
                OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Result = result });
            }
            else
                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

            return result;
        }

        //TODO: Do we need to use ICelestialBody or IZome here? It will call different Saves depending which we use...
        public async Task<OASISResult<IEnumerable<IZome>>> SaveZomesAsync(bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IZome> zomeResult = new OASISResult<IZome>();

            if (this.Zomes != null)
            {
                foreach (IZome zome in this.Zomes)
                {
                    if (zome.HasHolonChanged())
                    {
                        zomeResult = await zome.SaveAsync(saveChildren, recursive, continueOnError);

                        if (zomeResult != null && zomeResult.Result != null && !zomeResult.IsError)
                            result.SavedCount++;
                        else
                        {
                            result.ErrorCount++;
                            ErrorHandling.HandleWarning(ref zomeResult, $"There was an error in the CelestialBodyCore.SaveZomes method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {zomeResult.Message}", true, false, false, true, false);
                            //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                            if (!continueOnError)
                                break;
                        }
                    }
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured in CelestialBodyCore.SaveZomes method whilst saving {Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.SavedCount == 0)
                    ErrorHandling.HandleError(ref result, message);
                else
                {
                    ErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }

                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });
            }
            else
                result.IsSaved = true;

            OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
            return result;
        }

        public OASISResult<IEnumerable<IZome>> SaveZomes(bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IZome> zomeResult = new OASISResult<IZome>();

            if (this.Zomes != null)
            {
                foreach (IZome zome in this.Zomes)
                {
                    if (zome.HasHolonChanged())
                    {
                        zomeResult = zome.Save(saveChildren, recursive, continueOnError);

                        if (zomeResult != null && zomeResult.Result != null && !zomeResult.IsError)
                            result.SavedCount++;
                        else
                        {
                            result.ErrorCount++;
                            ErrorHandling.HandleWarning(ref zomeResult, $"There was an error in the CelestialBodyCore.SaveZomes method whilst saving the {LoggingHelper.GetHolonInfoForLogging(zome, "Zome")}. Reason: {zomeResult.Message}", true, false, false, true, false);
                            //OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });

                            if (!continueOnError)
                                break;
                        }
                    }
                }
            }

            if (result.ErrorCount > 0)
            {
                string message = $"{result.ErrorCount} Error(s) occured in CelestialBodyCore.SaveZomes method whilst saving {Zomes.Count} Zomes in the {LoggingHelper.GetHolonInfoForLogging(this, "CelestialBody")}. Please check the logs and InnerMessages for more info. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}";

                if (result.SavedCount == 0)
                    ErrorHandling.HandleError(ref result, message);
                else
                {
                    ErrorHandling.HandleWarning(ref result, message);
                    result.IsSaved = true;
                }

                OnZomesError?.Invoke(this, new ZomesErrorEventArgs() { Reason = $"{result.Message}", Result = result });
            }
            else
                result.IsSaved = true;

            OnZomesSaved?.Invoke(this, new ZomesSavedEventArgs() { Result = result });
            return result;
        }

        public async Task<OASISResult<IZome>> AddZomeAsync(IZome zome, bool saveChildren = true, bool resursive = true, bool continueOnError = true)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();

            //TODO: Dont think we need this because the SaveHolonsAsync method below automatically saves the entire collection?
            //if (zome.Id == Guid.Empty)
            //    result = await zome.SaveAsync();
 
            if (!result.IsError)
            {
                this.Zomes.Add(zome);
                
                //TODO: This is used in quite a few places but not sure how efficient it is because it will always save the entire collection even if its not needed?
                OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(this.Zomes, true, saveChildren, resursive, continueOnError);
                OASISResultCollectionToHolonHelper<IEnumerable<IHolon>, IZome>.CopyResult(holonsResult, ref result);
            }

            return result;
        }

        public OASISResult<IZome> AddZome(IZome zome, bool saveChildren = true, bool resursive = true, bool continueOnError = true)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();

            //TODO: Dont think we need this because the SaveHolonsAsync method below automatically saves the entire collection?
            //if (zome.Id == Guid.Empty)
            //    result = await zome.SaveAsync();

            if (!result.IsError)
            {
                this.Zomes.Add(zome);

                //TODO: This is used in quite a few places but not sure how efficient it is because it will always save the entire collection even if its not needed?
                OASISResult<IEnumerable<IHolon>> holonsResult = base.SaveHolons(this.Zomes, true, saveChildren, resursive, continueOnError);
                OASISResultCollectionToHolonHelper<IEnumerable<IHolon>, IZome>.CopyResult(holonsResult, ref result);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IZome>>> RemoveZomeAsync(IZome zome)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            this.Zomes.Remove(zome);

            //Don't think we need to save children if we are just removing a holon?
            OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(this.Zomes, true, false, false, false);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonsResult, ref result);

            if (!holonsResult.IsError && holonsResult.Result != null)
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonsResult.Result);

            return result;
        }

        public OASISResult<IEnumerable<IZome>> RemoveZome(IZome zome)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            this.Zomes.Remove(zome);

            //Don't think we need to save children if we are just removing a zome?
            OASISResult<IEnumerable<IHolon>> holonsResult = base.SaveHolons(this.Zomes, true, false, false, false);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonsResult, ref result);

            if (!holonsResult.IsError && holonsResult.Result != null)
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonsResult.Result);

            return result;
        }

        public async Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon, bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            return await base.SaveHolonAsync(savingHolon, false, saveChildren, recursive, continueOnError);
        }

        public OASISResult<IHolon> SaveCelestialBody(IHolon savingHolon, bool saveChildren = true, bool recursive = true, bool continueOnError = true)
        {
            //TODO: Not sure if this is a good way of doing this?
            return SaveCelestialBodyAsync(savingHolon, saveChildren, recursive, continueOnError).Result;
        }

        public async Task<OASISResult<T>> SaveCelestialBodyAsync<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, bool continueOnError = true) where T : IHolon, new()
        {
            return await base.SaveHolonAsync<T>(savingHolon, false, saveChildren, recursive, continueOnError);
        }

        public OASISResult<T> SaveCelestialBody<T>(IHolon savingHolon, bool saveChildren = true, bool recursive = true, bool continueOnError = true) where T : IHolon, new()
        {
            //TODO: Not sure if this is a good way of doing this?
            return SaveCelestialBodyAsync<T>(savingHolon, saveChildren, recursive, continueOnError).Result;
        }

        //public async Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync()
        //{
        //    OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
        //    OASISResult<IHolon> holonResult = await base.LoadHolonAsync();
        //    OASISResultHolonToHolonHelper<IHolon, ICelestialBody>.CopyResult(holonResult, ref result);

        //    if (!holonResult.IsError && holonResult.Result != null)
        //    {
        //        //result.Result = Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(holonResult.Result);
        //        result.Result = (ICelestialBody)holonResult.Result; //TODO: Not sure if this cast will work? Probably not... Need to map...
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IHolon>> LoadCelestialBodyAsync()
        //{
        //    return await base.LoadHolonAsync();
        //}

        public async Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync(bool loadChildren = true, bool recursive = true, bool continueOnError = true)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            OASISResult<IHolon> holonResult = await base.LoadHolonAsync(loadChildren, recursive, continueOnError);
            result.Result = (ICelestialBody)holonResult.Result;
            OASISResultHolonToHolonHelper<IHolon, ICelestialBody>.CopyResult(holonResult, result);
            //result.Result = (ICelestialBody)await base.LoadHolonAsync();

            return result;
        }

        /*
        public OASISResult<ICelestialBody> LoadCelestialBody()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            OASISResult<IHolon> holonResult = base.LoadHolon();
            OASISResultHolonToHolonHelper<IHolon, ICelestialBody>.CopyResult(holonResult, ref result);

            if (!holonResult.IsError && holonResult.Result != null)
            {
                //result.Result = Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(holonResult.Result);
                //result.Result = Mapper.MapBaseHolonProperties(holonResult.Result, (ICelestialBody)result.Result);

                result.Result = new CelestialBody();
                result.Result.Id = holonResult.Result.Id;
                result.Result.ProviderKey = holonResult.Result .ProviderKey;
                result.Result.Name = holonResult.Result .Name;
                result.Result.Description = holonResult.Result .Description;
                result.Result.HolonType = holonResult.Result .HolonType;
                result.Result.ParentGreatGrandSuperStar = holonResult.Result .ParentGreatGrandSuperStar;
                result.Result.ParentGreatGrandSuperStarId = holonResult.Result .ParentGreatGrandSuperStarId;
                result.Result.ParentGrandSuperStar = holonResult.Result .ParentGrandSuperStar;
                result.Result.ParentGrandSuperStarId = holonResult.Result .ParentGrandSuperStarId;
                result.Result.ParentSuperStar = holonResult.Result .ParentSuperStar;
                result.Result.ParentSuperStarId = holonResult.Result .ParentSuperStarId;
                result.Result.ParentStar = holonResult.Result .ParentStar;
                result.Result.ParentStarId = holonResult.Result .ParentStarId;
                result.Result.ParentPlanet = holonResult.Result .ParentPlanet;
                result.Result.ParentPlanetId = holonResult.Result .ParentPlanetId;
                result.Result.ParentMoon = holonResult.Result .ParentMoon;
                result.Result.ParentMoonId = holonResult.Result .ParentMoonId;
                result.Result.ParentZome = holonResult.Result .ParentZome;
                result.Result.ParentZomeId = holonResult.Result .ParentZomeId;
                result.Result.ParentHolon = holonResult.Result .ParentHolon;
                result.Result.ParentHolonId = holonResult.Result .ParentHolonId;
                result.Result.ParentOmiverse = holonResult.Result .ParentOmiverse;
                result.Result.ParentOmiverseId = holonResult.Result .ParentOmiverseId;
                result.Result.ParentUniverse = holonResult.Result .ParentUniverse;
                result.Result.ParentUniverseId = holonResult.Result .ParentUniverseId;
                result.Result.ParentGalaxy = holonResult.Result .ParentGalaxy;
                result.Result.ParentGalaxyId = holonResult.Result .ParentGalaxyId;
                result.Result.ParentSolarSystem = holonResult.Result .ParentSolarSystem;
                result.Result.ParentSolarSystemId = holonResult.Result .ParentSolarSystemId;
                result.Result.Children = holonResult.Result .Children;
                result.Result.Nodes = holonResult.Result .Nodes;
                //result.Result.CelestialBodyCore.Id = holonResult.Result .Id; //TODO: Dont think need result.Result now?
                //result.Result.CelestialBodyCore.ProviderKey = holonResult.Result .ProviderKey; //TODO: Dont think need result.Result now?
                result.Result.CreatedByAvatar = holonResult.Result .CreatedByAvatar;
                result.Result.CreatedByAvatarId = holonResult.Result .CreatedByAvatarId;
                result.Result.CreatedDate = holonResult.Result .CreatedDate;
                result.Result.ModifiedByAvatar = holonResult.Result .ModifiedByAvatar;
                result.Result.ModifiedByAvatarId = holonResult.Result .ModifiedByAvatarId;
                result.Result.ModifiedDate = holonResult.Result .ModifiedDate;
                result.Result.DeletedByAvatar = holonResult.Result .DeletedByAvatar;
                result.Result.DeletedByAvatarId = holonResult.Result .DeletedByAvatarId;
                result.Result.DeletedDate = holonResult.Result .DeletedDate;
                result.Result.Version = holonResult.Result .Version;
                result.Result.IsActive = holonResult.Result .IsActive;
                result.Result.IsChanged = holonResult.Result .IsChanged;
                result.Result.IsNewHolon = holonResult.Result .IsNewHolon;
                result.Result.MetaData = holonResult.Result .MetaData;
                result.Result.ProviderMetaData = holonResult.Result .ProviderMetaData;
                result.Result.Original = holonResult.Result .Original;
            }

            return result;
        }*/

        //public OASISResult<IHolon> LoadCelestialBody()
        //{
        //    return base.LoadHolon();
        //}

        public OASISResult<ICelestialBody> LoadCelestialBody(bool loadChildren = true, bool recursive = true, bool continueOnError = true)
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            OASISResult<IHolon> holonResult = base.LoadHolon(loadChildren, recursive, continueOnError);
            result.Result = (ICelestialBody)holonResult.Result;
            OASISResultHolonToHolonHelper<IHolon, ICelestialBody>.CopyResult(holonResult, result);
            return result;
        }

        protected virtual async Task<OASISResult<IHolon>> AddHolonToCollectionAsync(IHolon parentCelestialBody, IHolon holon, List<IHolon> holons, bool saveHolon = true)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();

            if (holons == null)
                holons = new List<IHolon>();

            else if (holons.Any(x => x.Name == holon.Name))
            {
                result.IsError = true;
                result.Message = string.Concat("The name ", holon.Name, " is already taken, please choose another.");
                return result;
            }

            // TODO: Need to double check this logic below is right! ;-)
            holon.IsNewHolon = true; //TODO: I am pretty sure every holon being added to a collection using this method will be a new one?

            if (holon.ParentOmiverseId == Guid.Empty)
            {
                holon.ParentOmiverseId = parentCelestialBody.ParentOmiverseId;
                holon.ParentOmiverse = parentCelestialBody.ParentOmiverse;
            }

            if (holon.ParentMultiverseId == Guid.Empty)
            {
                holon.ParentMultiverseId = parentCelestialBody.ParentMultiverseId;
                holon.ParentMultiverse = parentCelestialBody.ParentMultiverse;
            }

            if (holon.ParentUniverseId == Guid.Empty)
            {
                holon.ParentUniverseId = parentCelestialBody.ParentUniverseId;
                holon.ParentUniverse = parentCelestialBody.ParentUniverse;
            }

            if (holon.ParentDimensionId == Guid.Empty)
            {
                holon.ParentDimensionId = parentCelestialBody.ParentDimensionId;
                holon.ParentDimension = parentCelestialBody.ParentDimension;
            }

            if (holon.ParentGalaxyClusterId == Guid.Empty)
            {
                holon.ParentGalaxyClusterId = parentCelestialBody.ParentGalaxyClusterId;
                holon.ParentGalaxyCluster = parentCelestialBody.ParentGalaxyCluster;
            }

            if (holon.ParentGalaxyId == Guid.Empty)
            {
                holon.ParentGalaxyId = parentCelestialBody.ParentGalaxyId;
                holon.ParentGalaxy = parentCelestialBody.ParentGalaxy;
            }

            if (holon.ParentSolarSystemId == Guid.Empty)
            {
                holon.ParentSolarSystemId = parentCelestialBody.ParentSolarSystemId;
                holon.ParentSolarSystem = parentCelestialBody.ParentSolarSystem;
            }

            if (holon.ParentGreatGrandSuperStarId == Guid.Empty)
            {
                holon.ParentGreatGrandSuperStarId = parentCelestialBody.ParentGreatGrandSuperStarId;
                holon.ParentGreatGrandSuperStar = parentCelestialBody.ParentGreatGrandSuperStar;
            }

            if (holon.ParentGrandSuperStarId == Guid.Empty)
            {
                holon.ParentGrandSuperStarId = parentCelestialBody.ParentGrandSuperStarId;
                holon.ParentGrandSuperStar = parentCelestialBody.ParentGrandSuperStar;
            }

            if (holon.ParentSuperStarId == Guid.Empty)
            {
                holon.ParentSuperStarId = parentCelestialBody.ParentSuperStarId;
                holon.ParentSuperStar = parentCelestialBody.ParentSuperStar;
            }

            if (holon.ParentStarId == Guid.Empty)
            {
                holon.ParentStarId = parentCelestialBody.ParentStarId;
                holon.ParentStar = parentCelestialBody.ParentStar;
            }

            if (holon.ParentPlanetId == Guid.Empty)
            {
                holon.ParentPlanetId = parentCelestialBody.ParentPlanetId;
                holon.ParentPlanet = parentCelestialBody.ParentPlanet;
            }

            if (holon.ParentMoonId == Guid.Empty)
            {
                holon.ParentMoonId = parentCelestialBody.ParentMoonId;
                holon.ParentMoon = parentCelestialBody.ParentMoon;
            }

            if (holon.ParentZomeId == Guid.Empty)
            {
                holon.ParentZomeId = parentCelestialBody.ParentZomeId;
                holon.ParentZome = parentCelestialBody.ParentZome;
            }

            if (holon.ParentHolonId == Guid.Empty)
            {
                holon.ParentHolonId = parentCelestialBody.ParentHolonId;
                holon.ParentHolon = parentCelestialBody.ParentHolon;
            }

            switch (parentCelestialBody.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    holon.ParentGreatGrandSuperStarId = parentCelestialBody.Id;
                    holon.ParentGreatGrandSuperStar = (IGreatGrandSuperStar)parentCelestialBody;
                    break;

                case HolonType.GrandSuperStar:
                    holon.ParentGrandSuperStarId = parentCelestialBody.Id;
                    holon.ParentGrandSuperStar = (IGrandSuperStar)parentCelestialBody;
                    break;

                case HolonType.SuperStar:
                    holon.ParentSuperStarId = parentCelestialBody.Id;
                    holon.ParentSuperStar = (ISuperStar)parentCelestialBody;
                    break;

                case HolonType.Multiverse:
                    holon.ParentMultiverseId = parentCelestialBody.Id;
                    holon.ParentMultiverse = (IMultiverse)parentCelestialBody;
                    break;

                case HolonType.Universe:
                    holon.ParentUniverseId = parentCelestialBody.Id;
                    holon.ParentUniverse = (IUniverse)parentCelestialBody;
                    break;

                case HolonType.Dimension:
                    holon.ParentDimensionId = parentCelestialBody.Id;
                    holon.ParentDimension = (IDimension)parentCelestialBody;
                    break;

                case HolonType.GalaxyCluster:
                    holon.ParentGalaxyClusterId = parentCelestialBody.Id;
                    holon.ParentGalaxyCluster = (IGalaxyCluster)parentCelestialBody;
                    break;

                case HolonType.Galaxy:
                    holon.ParentGalaxyId = parentCelestialBody.Id;
                    holon.ParentGalaxy = (IGalaxy)parentCelestialBody;
                    break;

                case HolonType.SolarSystem:
                    holon.ParentSolarSystemId = parentCelestialBody.Id;
                    holon.ParentSolarSystem = (ISolarSystem)parentCelestialBody;
                    break;

                case HolonType.Star:
                    holon.ParentStarId = parentCelestialBody.Id;
                    holon.ParentStar = (IStar)parentCelestialBody;
                    break;

                case HolonType.Planet:
                    holon.ParentPlanetId = parentCelestialBody.Id;
                    holon.ParentPlanet = (IPlanet)parentCelestialBody;
                    break;

                case HolonType.Moon:
                    holon.ParentMoonId = parentCelestialBody.Id;
                    holon.ParentMoon = (IMoon)parentCelestialBody;
                    break;

                case HolonType.Zome:
                    holon.ParentZomeId = parentCelestialBody.Id;
                    holon.ParentZome = (IZome)parentCelestialBody;
                    break;

                case HolonType.Holon:
                    holon.ParentHolonId = parentCelestialBody.Id;
                    holon.ParentHolon = parentCelestialBody;
                    break;
            }
            
            holons.Add(holon);

            //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false);
            //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false); //TODO: Temp to test new code...

            if (saveHolon)
            {
                result = await base.SaveHolonAsync(holon, false); //TODO: WE ONLY NEED TO SAVE THE NEW HOLON, NO NEED TO RE-SAVE THE WHOLE COLLECTION AGAIN! ;-)
                result.IsSaved = true;
            }
            else
            {
                result.Message = "Holon was not saved due to saveHolon being set to false.";
                result.IsSaved = false;
            }

            //OASISResultCollectionToHolonHelper<IEnumerable<IHolon>, IHolon>.CopyResult(holonsResult, ref result);

            //if (!holonsResult.IsError)
            //{
            //    IHolon savedHolon = holons.FirstOrDefault(x => x.Name == holon.Name);
            //    result.Result = savedHolon;
            //}

            return result;
        }

        protected virtual async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsAsync(IEnumerable<IHolon> holons, HolonType holonType, bool refresh = true)
        {
            OASISResult<IEnumerable<IHolon>> result = new OASISResult<IEnumerable<IHolon>>();

            if (holons == null || refresh)
                result = await base.LoadHolonsForParentAsync(holonType);
            else
            {
                result.Message = "Refresh not required";
                result.Result = holons;
            }

            return result;
        }
    }
}