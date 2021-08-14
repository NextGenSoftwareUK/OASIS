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
            ICelestialBodyCore core = null;
            IHolon parentHolon = null;
            IStar parentStar = null;
            IPlanet parentPlanet = null;
            IMoon parentMoon = null;
            //ICelestialBodyCore parentHolonCore = null;
            //ICelestialBodyCore parentStarCore = null;
            //ICelestialBodyCore parentPlanetCore = null;
            //ICelestialBodyCore parentMoonCore = null;

            ICelestialBody celestialBody = savingHolon as ICelestialBody;

            if (celestialBody != null)
            {
                core = celestialBody.CelestialBodyCore;
                celestialBody.CelestialBodyCore = null;
            }

            parentHolon = savingHolon.ParentHolon;
            parentStar = savingHolon.ParentStar;
            parentPlanet = savingHolon.ParentPlanet;
            parentMoon = savingHolon.ParentMoon;
            savingHolon.ParentHolon = null;
            savingHolon.ParentStar = null;
            savingHolon.ParentPlanet = null;
            savingHolon.ParentMoon = null;

            //ICelestialBody celestialBody = savingHolon as ICelestialBody;
            /*
            // Temp remove the cores from the celestialBody otherwise we have a infinite recursive issue in the OASIS Providers when saving/serialaizaing, etc.
            if (celestialBody != null)
            {
                core = celestialBody.CelestialBodyCore;
                celestialBody.CelestialBodyCore = null;

                parentHolon = celestialBody.ParentHolon;
                celestialBody.ParentHolon = null;

                
                //ICelestialBody parent = celestialBody.ParentHolon as ICelestialBody;

                //if (parent != null)
                //{
                //    parentHolon = parent;
                //    //parentHolonCore = parent.CelestialBodyCore;
                //}

                if (celestialBody.ParentStar != null)
                {
                    parentStar = celestialBody.ParentStar;
                    //parentStarCore = celestialBody.ParentStar.CelestialBodyCore;
                    //celestialBody.ParentStar.CelestialBodyCore = null;
                    celestialBody.ParentStar = null;
                }

                if (celestialBody.ParentPlanet != null)
                {
                    parentPlanetCore = celestialBody.ParentPlanet.CelestialBodyCore;
                    celestialBody.ParentPlanet.CelestialBodyCore = null;
                }

                if (celestialBody.ParentMoon != null)
                {
                    parentMoonCore = celestialBody.ParentMoon.CelestialBodyCore;
                    celestialBody.ParentMoon.CelestialBodyCore = null;
                }
            }*/

            // RemoveCores(savingHolon);
            OASISResult<IHolon> result = await base.SaveHolonAsync(savingHolon);

            // Restore the core.
            if (result.Result != null && core != null)
            {
                celestialBody = savingHolon as ICelestialBody;

                //Restore the cores.
                if (celestialBody != null)
                    celestialBody.CelestialBodyCore = core;

                celestialBody.ParentHolon = parentHolon;
                celestialBody.ParentStar = parentStar;
                celestialBody.ParentPlanet = parentPlanet;
                celestialBody.ParentMoon = parentMoon;

                /*
                ICelestialBody parent = celestialBody.ParentHolon as ICelestialBody;

                if (parent != null)
                    parent = parentHolon;
                    //parent.CelestialBodyCore = parentHolonCore;
                */
                /*
                if (celestialBody.ParentStar != null)
                {
                    celestialBody.ParentStar = parentStar;
                    //celestialBody.ParentStar.CelestialBodyCore = parentStarCore;
                }

                if (celestialBody.ParentPlanet != null)
                    celestialBody.ParentPlanet.CelestialBodyCore = parentPlanetCore;

                if (celestialBody.ParentMoon != null)
                    celestialBody.ParentMoon.CelestialBodyCore = parentMoonCore;*/
            }

            return result;
        }

        public OASISResult<IHolon> SaveCelestialBody(IHolon savingHolon)
        {
            //TODO: Not sure if this is a good way of doing this?
            return SaveCelestialBodyAsync(savingHolon).Result;
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

                case HolonType.SoloarSystem:
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

            OASISResult<IEnumerable<IHolon>> holonsResult = await base.SaveHolonsAsync(holons);
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

        /*
        private void RemoveCores(IHolon holon)
        {
            // Temp remove the core from the celestialBody otherwise we have a infinite recursive issue in the OASIS Providers when saving/serialaizaing, etc.
            ICelestialBodyCore core = null;
            ICelestialBodyCore parentHolonCore = null;
            ICelestialBodyCore parentStarCore = null;
            ICelestialBodyCore parentPlanetCore = null;
            ICelestialBodyCore parentMoonCore = null;
            ICelestialBody celestialBody = holon as ICelestialBody;

            if (celestialBody != null)
            {
                core = celestialBody.CelestialBodyCore;
                celestialBody.CelestialBodyCore = null;

                foreach (Zome zome in celestialBody.CelestialBodyCore.Zomes)
                    RemoveCores(zome);

                //ICelestialBody parent = celestialBody.ParentHolon as ICelestialBody;

                //if (parent != null)
                //    parentHolonCore = parent.CelestialBodyCore;

                //parentStarCore = celestialBody.ParentStar.CelestialBodyCore;
                //parentPlanetCore = celestialBody.ParentPlanet.CelestialBodyCore;
                //parentMoonCore = celestialBody.ParentMoon.CelestialBodyCore;

                RemoveCores(celestialBody.ParentHolon);
                RemoveCores(celestialBody.ParentStar);
                RemoveCores(celestialBody.ParentPlanet);
                RemoveCores(celestialBody.ParentMoon);
            }
        }

        private void RestoreCores(IHolon holon)
        {
            // Temp remove the core from the celestialBody otherwise we have a infinite recursive issue in the OASIS Providers when saving/serialaizaing, etc.
            ICelestialBodyCore core = null;
            ICelestialBodyCore parentHolonCore = null;
            ICelestialBodyCore parentStarCore = null;
            ICelestialBodyCore parentPlanetCore = null;
            ICelestialBodyCore parentMoonCore = null;
            ICelestialBody celestialBody = holon as ICelestialBody;

            if (celestialBody != null)
            {
                switch (celestialBody.HolonType)
                {
                    case HolonType.Star:
                        celestialBody.CelestialBodyCore = new StarCore(celestialBody.Id, (IStar)celestialBody);
                        break;

                    case HolonType.Planet:
                        celestialBody.CelestialBodyCore = new PlanetCore(celestialBody.Id, (IPlanet)celestialBody);
                        break;

                    case HolonType.Moon:
                        celestialBody.CelestialBodyCore = new MoonCore(celestialBody.Id, (IMoon)celestialBody);
                        break;
                }

                foreach (Zome zome in celestialBody.CelestialBodyCore.Zomes)
                    RemoveCores(zome);

                ICelestialBody parent = celestialBody.ParentHolon as ICelestialBody;

                if (parent != null)
                    parentHolonCore = parent.CelestialBodyCore;

                parentStarCore = celestialBody.ParentStar.CelestialBodyCore;
                parentPlanetCore = celestialBody.ParentPlanet.CelestialBodyCore;
                parentMoonCore = celestialBody.ParentMoon.CelestialBodyCore;

                RestoreCores(celestialBody.ParentHolon);
                RestoreCores(celestialBody.ParentStar);
                RestoreCores(celestialBody.ParentPlanet);
                RestoreCores(celestialBody.ParentMoon);
            }
        }*/
    }
}
