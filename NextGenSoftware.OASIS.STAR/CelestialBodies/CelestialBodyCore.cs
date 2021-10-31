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

namespace NextGenSoftware.OASIS.STAR.CelestialBodies
{
    public abstract class CelestialBodyCore : ZomeBase, ICelestialBodyCore
    {
        public delegate void HolonsLoaded(object sender, HolonsLoadedEventArgs e);
        //  public event HolonsLoaded OnHolonsLoaded;
        public event Events.HolonsLoaded OnHolonsLoaded;

        public delegate void ZomesLoaded(object sender, ZomesLoadedEventArgs e);
        //public event ZomesLoaded OnZomesLoaded;
        public event Events.ZomesLoaded OnZomesLoaded;

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

        public async Task<OASISResult<IEnumerable<IZome>>> LoadZomesAsync()
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IEnumerable<IHolon>>  holonResult = await base.LoadHolonsForParentAsync();
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonResult, ref result);

            if (holonResult.Result != null && !holonResult.IsError)
            {
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonResult.Result);
                OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Zomes = Zomes });
            }

            return result;
        }

        public OASISResult<IEnumerable<IZome>> LoadZomes()
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IEnumerable<IHolon>> holonResult = base.LoadHolonsForParent();
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonResult, ref result);

            if (holonResult.Result != null && !holonResult.IsError)
            {
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonResult.Result);
                OnZomesLoaded?.Invoke(this, new ZomesLoadedEventArgs { Zomes = Zomes });
            }

            return result;
        }

        public async Task<OASISResult<IZome>> AddZomeAsync(IZome zome)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();

            //TODO: Dont think we need this because the SaveHolonsAsync method below automatically saves the entire collection?
            //if (zome.Id == Guid.Empty)
            //    result = await zome.SaveAsync();
 
            if (!result.IsError)
            {
                this.Zomes.Add(zome);
                
                //TODO: This is used in quite a few places but not sure how efficient it is because it will always save the entire collection even if its not needed?
                OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(this.Zomes);
                OASISResultCollectionToHolonHelper<IEnumerable<IHolon>, IZome>.CopyResult(holonsResult, ref result);
            }

            return result;
        }

        public OASISResult<IZome> AddZome(IZome zome)
        {
            OASISResult<IZome> result = new OASISResult<IZome>();

            //TODO: Dont think we need this because the SaveHolonsAsync method below automatically saves the entire collection?
            //if (zome.Id == Guid.Empty)
            //    result = await zome.SaveAsync();

            if (!result.IsError)
            {
                this.Zomes.Add(zome);

                //TODO: This is used in quite a few places but not sure how efficient it is because it will always save the entire collection even if its not needed?
                OASISResult<IEnumerable<IHolon>> holonsResult = base.SaveHolons(this.Zomes);
                OASISResultCollectionToHolonHelper<IEnumerable<IHolon>, IZome>.CopyResult(holonsResult, ref result);
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<IZome>>> RemoveZomeAsync(IZome zome)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();

            this.Zomes.Remove(zome);
            OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(this.Zomes);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonsResult, ref result);

            if (!holonsResult.IsError && holonsResult.Result != null)
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonsResult.Result);

            return result;
        }

        public OASISResult<IEnumerable<IZome>> RemoveZome(IZome zome)
        {
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();

            this.Zomes.Remove(zome);
            OASISResult<IEnumerable<IHolon>> holonsResult = base.SaveHolons(this.Zomes);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IZome>>.CopyResult(holonsResult, ref result);

            if (!holonsResult.IsError && holonsResult.Result != null)
                result.Result = Mapper<IHolon, Zome>.MapBaseHolonProperties(holonsResult.Result);

            return result;
        }

        public async Task<OASISResult<IHolon>> SaveCelestialBodyAsync(IHolon savingHolon)
        {
            return await base.SaveHolonAsync(savingHolon, false);
        }

        public OASISResult<IHolon> SaveCelestialBody(IHolon savingHolon)
        {
            //TODO: Not sure if this is a good way of doing this?
            return SaveCelestialBodyAsync(savingHolon).Result;
        }

        public async Task<OASISResult<T>> SaveCelestialBodyAsync<T>(IHolon savingHolon) where T : IHolon, new()
        {
            return await base.SaveHolonAsync<T>(savingHolon, false);
        }

        public OASISResult<T> SaveCelestialBody<T>(IHolon savingHolon) where T : IHolon, new()
        {
            //TODO: Not sure if this is a good way of doing this?
            return SaveCelestialBodyAsync<T>(savingHolon).Result;
        }

        public async Task<OASISResult<ICelestialBody>> LoadCelestialBodyAsync()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            OASISResult<IHolon> holonResult = await base.LoadHolonAsync();
            OASISResultHolonToHolonHelper<IHolon, ICelestialBody>.CopyResult(holonResult, ref result);

            if (!holonResult.IsError && holonResult.Result != null)
            {
                //result.Result = Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(holonResult.Result);
                result.Result = (ICelestialBody)holonResult.Result; //TODO: Not sure if this cast will work? Probably not... Need to map...
            }

            return result;
        }

        public OASISResult<ICelestialBody> LoadCelestialBody()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();
            OASISResult<IHolon> holonResult = base.LoadHolon();
            OASISResultHolonToHolonHelper<IHolon, ICelestialBody>.CopyResult(holonResult, ref result);

            if (!holonResult.IsError && holonResult.Result != null)
            {
                //result.Result = Mapper<IHolon, CelestialBody>.MapBaseHolonProperties(holonResult.Result);
                result.Result = (ICelestialBody)holonResult.Result; //TODO: Not sure if this cast will work? Probably not... Need to map...
            }

            return result;
        }

        protected virtual async Task<OASISResult<IHolon>> AddHolonToCollectionAsync(IHolon parentCelestialBody, IHolon holon, List<IHolon> holons)
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
            holon.ParentOmiverseId = parentCelestialBody.ParentOmiverseId;
            holon.ParentMultiverseId = parentCelestialBody.ParentMultiverseId;
            holon.ParentUniverseId = parentCelestialBody.ParentUniverseId;
            holon.ParentDimensionId = parentCelestialBody.ParentDimensionId;
            holon.ParentGalaxyClusterId = parentCelestialBody.ParentGalaxyClusterId;
            holon.ParentGalaxyId = parentCelestialBody.ParentGalaxyId;
            holon.ParentSolarSystemId = parentCelestialBody.ParentSolarSystemId;
            holon.ParentGreatGrandSuperStarId = parentCelestialBody.ParentGreatGrandSuperStarId;
            holon.ParentGrandSuperStarId = parentCelestialBody.ParentGrandSuperStarId;
            holon.ParentSuperStarId = parentCelestialBody.ParentSuperStarId;
            holon.ParentStarId = parentCelestialBody.ParentStarId;
            holon.ParentPlanetId = parentCelestialBody.ParentPlanetId;
            holon.ParentMoonId = parentCelestialBody.ParentMoonId;
            holon.ParentZomeId = parentCelestialBody.ParentZomeId;
            holon.ParentHolonId = parentCelestialBody.ParentHolonId;

            switch (parentCelestialBody.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    holon.ParentGreatGrandSuperStarId = parentCelestialBody.Id;
                    break;

                case HolonType.GrandSuperStar:
                    holon.ParentGrandSuperStarId = parentCelestialBody.Id;
                    break;

                case HolonType.SuperStar:
                    holon.ParentSuperStarId = parentCelestialBody.Id;
                    break;

                case HolonType.Multiverse:
                    holon.ParentMultiverseId = parentCelestialBody.Id;
                    break;

                case HolonType.Universe:
                    holon.ParentUniverseId = parentCelestialBody.Id;
                    break;

                case HolonType.Dimension:
                    holon.ParentDimensionId = parentCelestialBody.Id;
                    break;

                case HolonType.GalaxyCluster:
                    holon.ParentGalaxyClusterId = parentCelestialBody.Id;
                    break;

                case HolonType.Galaxy:
                    holon.ParentGalaxyId = parentCelestialBody.Id;
                    break;

                case HolonType.SolarSystem:
                    holon.ParentSolarSystemId = parentCelestialBody.Id;
                    break;

                case HolonType.Star:
                    holon.ParentStarId = parentCelestialBody.Id;
                    break;

                case HolonType.Planet:
                    holon.ParentPlanetId = parentCelestialBody.Id;
                    break;

                case HolonType.Moon:
                    holon.ParentMoonId = parentCelestialBody.Id;
                    break;

                case HolonType.Zome:
                    holon.ParentZomeId = parentCelestialBody.Id;
                    break;

                case HolonType.Holon:
                    holon.ParentHolonId = parentCelestialBody.Id;
                    break;
            }
            
            holons.Add(holon);

            //OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, false);
            OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons, true); //TODO: Temp to test new code...
            OASISResultCollectionToHolonHelper<IEnumerable<IHolon>, IHolon>.CopyResult(holonsResult, ref result);

            if (!holonsResult.IsError)
            {
                IHolon savedHolon = holons.FirstOrDefault(x => x.Name == holon.Name);
                result.Result = savedHolon;
            }

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
