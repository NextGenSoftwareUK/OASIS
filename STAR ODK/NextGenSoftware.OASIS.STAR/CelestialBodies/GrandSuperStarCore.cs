using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.CelestialSpace;

namespace NextGenSoftware.OASIS.STAR
{
    // At the centre of each Multiverse (creates dimensions, universes, GalaxyClusters, SolarSystems (outside of a Galaxy), Stars (outside of a Galaxy) & Planets (outside of a Galaxy)). Prime Creator
    public class GrandSuperStarCore : CelestialBodyCore<Star>, IGrandSuperStarCore
    {
        public IGrandSuperStar GrandSuperStar { get; set; }

        public GrandSuperStarCore(IGrandSuperStar grandSuperStar) : base()
        {
            GrandSuperStar = grandSuperStar;
        }

        //public GrandSuperStarCore(IGrandSuperStar grandSuperStar, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        //{
        //    GrandSuperStar = grandSuperStar;
        //}

        public GrandSuperStarCore(IGrandSuperStar grandSuperStar, string providerKey, ProviderType providerType) : base(providerKey, providerType)
        {
            GrandSuperStar = grandSuperStar;
        }

        public GrandSuperStarCore(IGrandSuperStar grandSuperStar, Guid id) : base(id)
        {
            GrandSuperStar = grandSuperStar;
        }

        //public async Task<OASISResult<IUniverse>> AddUniverseToDimensionAsync(IDimension dimension, IUniverse universe)
        //{
        //    return OASISResultHelper<IHolon, IUniverse>.CopyResult(
        //        await AddHolonToCollectionAsync(GrandSuperStar, universe, (List<IHolon>)Mapper<IUniverse, Holon>.MapBaseHolonProperties(
        //            dimension.Un)), new OASISResult<IUniverse>());
        //}

        //public OASISResult<IUniverse> AddUniverseToDimension(IUniverse universe)
        //{
        //    return AddUniverseAsync(universe).Result;
        //}



        public async Task<OASISResult<IUniverse>> AddParallelUniverseToThirdDimensionAsync(IUniverse universe)
        {
            return OASISResultHelper.CopyResult(
                await GlobalHolonData.AddHolonToCollectionAsync(GrandSuperStar, universe, (List<IHolon>)Mapper<IUniverse, Holon>.MapBaseHolonProperties(
                    GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParallelUniverses)), new OASISResult<IUniverse>(new Universe()));
        }

        public OASISResult<IUniverse> AddParallelUniverseToThirdDimension(IUniverse universe)
        {
            return AddParallelUniverseToThirdDimensionAsync(universe).Result;
        }


        public async Task<OASISResult<IDimension>> AddDimensionToMultiverseAsync(IDimension dimension)
        {
            return OASISResultHelper.CopyResult(
                await GlobalHolonData.AddHolonToCollectionAsync(GrandSuperStar, dimension, (List<IHolon>)Mapper<IDimension, Holon>.MapBaseHolonProperties(
                    GrandSuperStar.ParentMultiverse.Dimensions.CustomDimensions)), new OASISResult<IDimension>());
        }

        public OASISResult<IDimension> AddDimensionToMultiverse(IDimension dimension)
        {
            return AddDimensionToMultiverseAsync(dimension).Result;
        }

        /// <summary>
        /// Create's the ThirdDimension within this Multiverse along with a child MagicVerse and UniversePrime.
        /// </summary>
        /// <returns></returns>
        public async Task<OASISResult<IThirdDimension>> AddThirdDimensionToMultiverseAsync()
        {
            OASISResult<IThirdDimension> result = new OASISResult<IThirdDimension>();

            if (GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.Id != Guid.Empty)
            {
                result.IsError = true;
                result.Message = "The Third Dimension has already been created. It cannot be created again.";
                return result;
            }

            if (GrandSuperStar.ParentMultiverse == null || (GrandSuperStar.ParentMultiverse != null && GrandSuperStar.ParentMultiverse.Id == Guid.Empty))
            {
                result.IsError = true;
                result.Message = "The Multiverse has not been created yet. Please create it first.";
                return result;
            }

            Mapper<IGrandSuperStar, ThirdDimension>.MapParentCelestialBodyProperties(GrandSuperStar, (ThirdDimension)GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension);
            GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParentGrandSuperStar = GrandSuperStar;
            GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParentGrandSuperStarId = GrandSuperStar.Id;

            // Now we need to save the ThirdDimension as a seperate Holon to get a Id.
            OASISResult<IHolon> thirdDimensionResult = await GlobalHolonData.SaveHolonAsync(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension, false);

            if (!thirdDimensionResult.IsError && thirdDimensionResult.Result != null)
            {
               // Mapper<IHolon, ThirdDimension>.MapBaseHolonProperties(thirdDimensionResult.Result, (ThirdDimension)GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension);
                result.Result = GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension;

                Mapper<IThirdDimension, Universe>.MapParentCelestialBodyProperties(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension, (Universe)GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.MagicVerse);
                GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.MagicVerse.ParentDimension = GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension;
                GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.MagicVerse.ParentDimensionId = GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.Id;
                //GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.MagicVerse.ParentGrandSuperStar = GrandSuperStar;
               // GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.MagicVerse.ParentGrandSuperStarId = GrandSuperStar.Id;

                // Now we need to save the MagicVerse as a seperate Holon to get a Id.
                OASISResult<IHolon> magicVerseResult = await GlobalHolonData.SaveHolonAsync(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.MagicVerse, false);

                if (!magicVerseResult.IsError && magicVerseResult.Result != null)
                {
                    //Mapper<IUniverse, Universe>.MapParentCelestialBodyProperties(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.MagicVerse, (Universe)GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.UniversePrime);
                    Mapper<IUniverse, Universe>.MapParentCelestialBodyProperties(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.MagicVerse, (Universe)GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.Universe);

                    // Now we need to save the UniversePrime as a seperate Holon to get a Id.
                    //OASISResult<IHolon> universePrimeResult = await SaveHolonAsync(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.UniversePrime, false);
                    OASISResult<IHolon> universePrimeResult = await GlobalHolonData.SaveHolonAsync(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.Universe, false);

                    if (!(!universePrimeResult.IsError && universePrimeResult.Result != null))
                        OASISResultHelper.CopyResult(universePrimeResult, result);
                }
                else
                    OASISResultHelper.CopyResult(magicVerseResult, result);
            }
            else
                OASISResultHelper.CopyResult(thirdDimensionResult, result);

            return result;
        }

        public OASISResult<IThirdDimension> AddThirdDimensionToMultiverse()
        {
            return AddThirdDimensionToMultiverseAsync().Result;
        }

        //public async Task<OASISResult<IThirdDimension>> AddMagicVerseToThirdDimensionAsync()
        //{
        //    OASISResult<IThirdDimension> result = new OASISResult<IThirdDimension>();

        //    if (GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.Id != Guid.Empty)
        //    {
        //        result.IsError = true;
        //        result.Message = "The Third Dimension has already been created. It cannot be created again.";
        //        return result;
        //    }

        //    if (GrandSuperStar.ParentMultiverse == null || (GrandSuperStar.ParentMultiverse != null && GrandSuperStar.ParentMultiverse.Id == Guid.Empty))
        //    {
        //        result.IsError = true;
        //        result.Message = "The Multiverse Has Not Been Created Yet. Please Create It First.";
        //        return result;
        //    }

        //    GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParentOmniverse = GrandSuperStar.ParentMultiverse.ParentOmniverse;
        //    GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParentOmniverseId = GrandSuperStar.ParentMultiverse.ParentOmniverseId;
        //    GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParentMultiverse = GrandSuperStar.ParentMultiverse;
        //    GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParentMultiverseId = GrandSuperStar.ParentMultiverse.Id;
        //    GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParentGrandSuperStar = GrandSuperStar;
        //    GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParentGrandSuperStarId = GrandSuperStar.Id;

        //    // Now we need to save the ThirdDimension as a seperate Holon to get a Id.
        //    OASISResult<IHolon> thirdDimensionResult = await SaveHolonAsync(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension);

        //    if (!thirdDimensionResult.IsError && thirdDimensionResult.Result != null)
        //    {
        //        Mapper<IHolon, ThirdDimension>.MapBaseHolonProperties(thirdDimensionResult.Result, (ThirdDimension)GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension);
        //        result.Result = GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension;
        //    }
        //    else
        //    {
        //        result.IsError = true;
        //        result.Message = thirdDimensionResult.Message;
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<IUniverse>> AddUniverseToDimensionAsync(IDimension dimension, IUniverse universe)
        //{
        //    return OASISResultHelper<IHolon, IUniverse>.CopyResult(
        //        await AddHolonToCollectionAsync(dimension, universe, (List<IHolon>)Mapper<IUniverse, Holon>.MapBaseHolonProperties(
        //            dimension.Universe)), new OASISResult<IUniverse>());
        //}

        //public OASISResult<IUniverse> AddUniverseToDimension(IDimension dimension, IUniverse universe)
        //{
        //    return AddUniverseToDimensionAsync(dimension, universe).Result;
        //}

        /*
        public async Task<OASISResult<IDimension>> AddDimensionToUniverseAsync(IUniverse universe, IDimension dimension)
        {
            return OASISResultHelper<IHolon, IDimension>.CopyResult(
                await AddHolonToCollectionAsync(universe, dimension, (List<IHolon>)Mapper<IDimension, Holon>.MapBaseHolonProperties(
                    universe.Dimensions)), new OASISResult<IDimension>());
        }

        public OASISResult<IDimension> AddDimensionToUniverse(IUniverse universe, IDimension dimension)
        {
            return AddDimensionToUniverseAsync(universe, dimension).Result;
        }

        public async Task<OASISResult<IGalaxyCluster>> AddGalaxyClusterToDimensionAsync(IDimension dimension, IGalaxyCluster galaxyCluster)
        {
            return OASISResultHelper<IHolon, IGalaxyCluster>.CopyResult(
               await AddHolonToCollectionAsync(dimension, galaxyCluster, (List<IHolon>)Mapper<IGalaxyCluster, Holon>.MapBaseHolonProperties(
                   dimension.GalaxyClusters)), new OASISResult<IGalaxyCluster>());
        }

        public OASISResult<IGalaxyCluster> AddGalaxyClusterToDimension(IDimension dimension, IGalaxyCluster galaxyCluster)
        {
            return AddGalaxyClusterToDimensionAsync(dimension, galaxyCluster).Result;
        }

        public async Task<OASISResult<ISolarSystem>> AddSolarSystemToDimensionAsync(IDimension dimension, ISolarSystem solarSystem)
        {
            return OASISResultHelper<IHolon, ISolarSystem>.CopyResult(
                await AddHolonToCollectionAsync(dimension, solarSystem, (List<IHolon>)Mapper<ISolarSystem, Holon>.MapBaseHolonProperties(
                    dimension.SoloarSystems)), new OASISResult<ISolarSystem>());
        }

        public OASISResult<ISolarSystem> AddSolarSystemToDimension(IDimension dimension, ISolarSystem solarSystem)
        {
            return AddSolarSystemToDimensionAsync(dimension, solarSystem).Result;
        }

        public async Task<OASISResult<IStar>> AddStarToDimensionAsync(IDimension dimension, IStar star)
        {
            return OASISResultHelper<IHolon, IStar>.CopyResult(
                await AddHolonToCollectionAsync(dimension, star, (List<IHolon>)Mapper<IStar, Holon>.MapBaseHolonProperties(
                    dimension.Stars)), new OASISResult<IStar>());
        }

        public OASISResult<IStar> AddStarToDimension(IDimension dimension, IStar star)
        {
            return AddStarToDimensionAsync(dimension, star).Result;
        }

        public async Task<OASISResult<IPlanet>> AddPlanetToDimensionAsync(IDimension dimension, IPlanet planet)
        {
            return OASISResultHelper<IHolon, IPlanet>.CopyResult(
                await AddHolonToCollectionAsync(dimension, planet, (List<IHolon>)Mapper<IPlanet, Holon>.MapBaseHolonProperties(
                    dimension.Planets)), new OASISResult<IPlanet>());
        }

        public OASISResult<IPlanet> AddPlanetToDimension(IDimension dimension, IPlanet planet)
        {
            return AddPlanetToDimensionAsync(dimension, planet).Result;
        }
        */

        public async Task<OASISResult<IGalaxyCluster>> AddGalaxyClusterToUniverseAsync(IUniverse universe, IGalaxyCluster galaxyCluster)
        {
            OASISResult<IHolon> holonResult = await GlobalHolonData.AddHolonToCollectionAsync(universe, galaxyCluster, (List<IHolon>)Mapper<IGalaxyCluster, Holon>.Convert(universe.GalaxyClusters));
            OASISResult<IGalaxyCluster> galaxyClusterResult = OASISResultHelper.CopyResult(holonResult, new OASISResult<IGalaxyCluster>());
            galaxyClusterResult.Result = (IGalaxyCluster)holonResult.Result;
            return galaxyClusterResult;

            //return OASISResultHelper<IHolon, IGalaxyCluster>.CopyResult(
            //   await AddHolonToCollectionAsync(universe, galaxyCluster, (List<IHolon>)Mapper<IGalaxyCluster, Holon>.MapBaseHolonProperties(
            //      universe.GalaxyClusters)), new OASISResult<IGalaxyCluster>());
        }

        public OASISResult<IGalaxyCluster> AddGalaxyClusterToUniverse(IUniverse universe, IGalaxyCluster galaxyCluster)
        {
            return AddGalaxyClusterToUniverseAsync(universe, galaxyCluster).Result;
        }

        public async Task<OASISResult<IGalaxy>> AddGalaxyToGalaxyClusterAsync(IGalaxyCluster galaxyCluster, IGalaxy galaxy)
        {
            OASISResult<IGalaxy> result = new OASISResult<IGalaxy>();

            //return OASISResultHelper<IHolon, IGalaxy>.CopyResult(
            //    await AddHolonToCollectionAsync(galaxyCluster, galaxy, (List<IHolon>)Mapper<IGalaxy, Holon>.MapBaseHolonProperties(
            //        galaxyCluster.Galaxies)), new OASISResult<IGalaxy>());

            if (galaxyCluster == null)
            {
                result.IsError = true;
                result.Message = "GalaxyCluster cannot be null.";
                return result;
            }

            if (galaxy == null)
            {
                result.IsError = true;
                result.Message = "Galaxy cannot be null.";
                return result;
            }

            if (galaxy.Id == Guid.Empty)
            {
                galaxy.Id = Guid.NewGuid();
                galaxy.IsNewHolon = true;
            }

            if (galaxy.SuperStar == null)
                galaxy.SuperStar = new SuperStar();

            if (galaxy.SuperStar.Id == Guid.Empty)
            {
                galaxy.SuperStar.Id = Guid.NewGuid();
                galaxy.SuperStar.IsNewHolon = true;
            }

            Mapper<IHolon, SuperStar>.MapParentCelestialBodyProperties(galaxyCluster, (SuperStar)galaxy.SuperStar);
                
            galaxy.SuperStar.ParentGalaxyCluster = galaxyCluster;
            galaxy.SuperStar.ParentGalaxyClusterId = galaxyCluster.Id;
            galaxy.SuperStar.ParentGalaxy = galaxy;
            galaxy.SuperStar.ParentGalaxyId = galaxy.Id;
            galaxy.ParentSuperStar = galaxy.SuperStar;
            galaxy.ParentSuperStarId = galaxy.SuperStar.Id;
            galaxy.ParentGalaxyCluster = galaxyCluster;
            galaxy.ParentGalaxyClusterId = galaxyCluster.Id;

            OASISResult<IHolon> holonResult = await GlobalHolonData.AddHolonToCollectionAsync(galaxyCluster, galaxy, (List<IHolon>)Mapper<IGalaxy, Holon>.Convert(galaxyCluster.Galaxies));
            result = OASISResultHelper.CopyResult(holonResult, new OASISResult<IGalaxy>());
            result.Result = (IGalaxy)holonResult.Result;

            if (!result.IsError && result.Result != null)
            {


                //No longer need to save the superstar seperately because it is automatically saved when the galaxy is saved because it is a child.
                //// Now we need to save the SuperStar as a seperate Holon.
                //OASISResult<IHolon> superStarResult = await GlobalHolonData.SaveHolonAsync(galaxy.SuperStar, false);

                //if (!superStarResult.IsError && superStarResult.Result != null)
                //{
                //   // Mapper<IHolon, SuperStar>.MapBaseHolonProperties(superStarResult.Result, (SuperStar)galaxy.SuperStar);
                //    result.Result = galaxy;
                //}
                //else
                //{
                //    result.IsError = true;
                //    result.Message = superStarResult.Message;
                //}
            }

            return result;
        }

        public OASISResult<IGalaxy> AddGalaxyToGalaxyCluster(IGalaxyCluster galaxyCluster, IGalaxy galaxy)
        {
            return AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy).Result;
        }

        public async Task<OASISResult<ISolarSystem>> AddSolarSystemToUniverseAsync(IUniverse universe, ISolarSystem solarSystem)
        {
            OASISResult<IHolon> holonResult = await GlobalHolonData.AddHolonToCollectionAsync(universe, solarSystem, (List<IHolon>)Mapper<ISolarSystem, Holon>.Convert(universe.SolarSystems));
            OASISResult<ISolarSystem> result = OASISResultHelper.CopyResult(holonResult, new OASISResult<ISolarSystem>());
            result.Result = (ISolarSystem)holonResult.Result;
            return result;

            //return OASISResultHelper<IHolon, ISolarSystem>.CopyResult(
            //    await AddHolonToCollectionAsync(universe, solarSystem, (List<IHolon>)Mapper<ISolarSystem, Holon>.MapBaseHolonProperties(
            //        universe.SolarSystems)), new OASISResult<ISolarSystem>());
        }

        public OASISResult<ISolarSystem> AddSolarSystemToUniverse(IUniverse universe, ISolarSystem solarSystem)
        {
            return AddSolarSystemToUniverseAsync(universe, solarSystem).Result;
        }

        public async Task<OASISResult<IStar>> AddStarToUniverseAsync(IUniverse universe, IStar star)
        {
            return OASISResultHelper.CopyResult(
                await GlobalHolonData.AddHolonToCollectionAsync(universe, star, (List<IHolon>)Mapper<IStar, Holon>.MapBaseHolonProperties(
                    universe.Stars)), new OASISResult<IStar>());

            //OASISResult<IHolon> holonResult = await AddHolonToCollectionAsync(universe, solarSystem, (List<IHolon>)Mapper<ISolarSystem, Holon>.Convert(universe.SolarSystems));
            //OASISResult<ISolarSystem> result = OASISResultHelper<IHolon, ISolarSystem>.CopyResult(holonResult, new OASISResult<ISolarSystem>());
            //result.Result = (ISolarSystem)holonResult.Result;
            //return result;
        }

        public OASISResult<IStar> AddStarToUniverse(IUniverse universe, IStar star)
        {
            return AddStarToUniverseAsync(universe, star).Result;
        }

        public async Task<OASISResult<IPlanet>> AddPlanetToUniverseAsync(IUniverse universe, IPlanet planet)
        {
            return OASISResultHelper.CopyResult(
                await GlobalHolonData.AddHolonToCollectionAsync(universe, planet, (List<IHolon>)Mapper<IPlanet, Holon>.MapBaseHolonProperties(
                    universe.Planets)), new OASISResult<IPlanet>());
        }

        public OASISResult<IPlanet> AddPlanetToUniverse(IUniverse universe, IPlanet planet)
        {
            return AddPlanetToUniverseAsync(universe, planet).Result;
        }

        /*
        public async Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
            
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GrandSuperStar.ParentMultiverse.Universes, HolonType.Universe, refresh);
            OASISResultHelper<IEnumerable<IHolon>, IEnumerable<IUniverse>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Universe>.MapBaseHolonProperties(holonResult.Result);

            return result;
        }

        public OASISResult<IEnumerable<IUniverse>> GetAllUniversesForMultiverse(bool refresh = true)
        {
            return GetAllUniversesForMultiverseAsync(refresh).Result;
        }*/

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
            List<IUniverse> universes = new List<IUniverse>();

            universes.Add(GrandSuperStar.ParentMultiverse.Dimensions.FirstDimension.Universe);
            universes.Add(GrandSuperStar.ParentMultiverse.Dimensions.SecondDimension.Universe);
            //universes.Add(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.UniversePrime);
            universes.Add(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.Universe);
            universes.Add(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.MagicVerse);
            universes.AddRange(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension.ParallelUniverses);
            universes.Add(GrandSuperStar.ParentMultiverse.Dimensions.FourthDimension.Universe);
            universes.Add(GrandSuperStar.ParentMultiverse.Dimensions.FifthDimension.Universe);
            universes.Add(GrandSuperStar.ParentMultiverse.Dimensions.SixthDimension.Universe);
            universes.Add(GrandSuperStar.ParentMultiverse.Dimensions.SeventhDimension.Universe);

            //TODO: Come back to this...
            //foreach (IDimension dimension in GrandSuperStar.ParentMultiverse.Dimensions.CustomDimensions)
            //    universes.Add(dimension.Universe);

            result.Result = universes;
            return result;
        }

        public OASISResult<IEnumerable<IUniverse>> GetAllUniversesForMultiverse(bool refresh = true)
        {
            return GetAllUniversesForMultiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForMultiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IDimension>> result = new OASISResult<IEnumerable<IDimension>>();
        //    OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForMultiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IUniverse>, IEnumerable<IDimension>>.CopyResult(universesResult, ref result);

        //    if (!universesResult.IsError)
        //    {
        //        List<IDimension> dimensions = new List<IDimension>();

        //        foreach (IUniverse universe in universesResult.Result)
        //            dimensions.AddRange(universe.Dimensions);

        //        result.Result = dimensions;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IDimension>> result = new OASISResult<IEnumerable<IDimension>>();
            List<IDimension> dimensions = new List<IDimension>();

            dimensions.Add(GrandSuperStar.ParentMultiverse.Dimensions.FirstDimension);
            dimensions.Add(GrandSuperStar.ParentMultiverse.Dimensions.SecondDimension);
            dimensions.Add(GrandSuperStar.ParentMultiverse.Dimensions.ThirdDimension);
            dimensions.Add(GrandSuperStar.ParentMultiverse.Dimensions.FourthDimension);
            dimensions.Add(GrandSuperStar.ParentMultiverse.Dimensions.FifthDimension);
            dimensions.Add(GrandSuperStar.ParentMultiverse.Dimensions.SixthDimension);
            dimensions.Add(GrandSuperStar.ParentMultiverse.Dimensions.SeventhDimension);
            dimensions.AddRange(GrandSuperStar.ParentMultiverse.Dimensions.CustomDimensions);

            result.Result = dimensions;
            return result;
        }

        public OASISResult<IEnumerable<IDimension>> GetAllDimensionsForMultiverse(bool refresh = true)
        {
            return GetAllDimensionsForMultiverseAsync(refresh).Result;
        }


        //public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForMultiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IGalaxyCluster>> result = new OASISResult<IEnumerable<IGalaxyCluster>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForMultiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IDimension>, IEnumerable<IGalaxyCluster>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<IGalaxyCluster> clusters = new List<IGalaxyCluster>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            clusters.AddRange(dimension.GalaxyClusters);

        //        result.Result = clusters;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxyCluster>> result = new OASISResult<IEnumerable<IGalaxyCluster>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(universesResult, result);

            if (!universesResult.IsError)
            {
                List<IGalaxyCluster> clusters = new List<IGalaxyCluster>();

                foreach (IUniverse universe in universesResult.Result)
                    clusters.AddRange(universe.GalaxyClusters);

                result.Result = clusters;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGalaxyCluster>> GetAllGalaxyClustersForMultiverse(bool refresh = true)
        {
            return GetAllGalaxyClustersForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxy>> result = new OASISResult<IEnumerable<IGalaxy>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxyClustersResult, result);

            if (!galaxyClustersResult.IsError)
            {
                List<IGalaxy> galaxies = new List<IGalaxy>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    galaxies.AddRange(cluster.Galaxies);

                result.Result = galaxies;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForMultiverse(bool refresh = true)
        {
            return GetAllGalaxiesForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISuperStar>> result = new OASISResult<IEnumerable<ISuperStar>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetAllGalaxiesForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxiesResult, result);

            if (!galaxiesResult.IsError)
            {
                List<ISuperStar> superstars = new List<ISuperStar>();

                foreach (IGalaxy galaxy in galaxiesResult.Result)
                    superstars.Add(galaxy.SuperStar);

                result.Result = superstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForMultiverse(bool refresh = true)
        {
            return GetAllSuperStarsForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxiesForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxyClustersResult, result);

            if (!galaxyClustersResult.IsError)
            {
                List<ISolarSystem> solarSystems = new List<ISolarSystem>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    solarSystems.AddRange(cluster.SolarSystems);

                result.Result = solarSystems;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxiesForMultiverse(bool refresh = true)
        {
            return GetAllSolarSystemsOutSideOfGalaxiesForMultiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForMultiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IDimension>, IEnumerable<ISolarSystem>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<ISolarSystem> solarSystems = new List<ISolarSystem>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            solarSystems.AddRange(dimension.SoloarSystems);

        //        result.Result = solarSystems;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(universesResult, result);

            if (!universesResult.IsError)
            {
                List<ISolarSystem> solarSystems = new List<ISolarSystem>();

                foreach (IUniverse universe in universesResult.Result)
                    solarSystems.AddRange(universe.SolarSystems);

                result.Result = solarSystems;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxyClustersForMultiverse(bool refresh = true)
        {
            return GetAllSolarSystemsOutSideOfGalaxyClustersForMultiverseAsync(refresh).Result;
        }


        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetAllGalaxiesForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxiesResult, result);
            List<ISolarSystem> solarSystems = new List<ISolarSystem>();

            if (!galaxiesResult.IsError)
            {
                foreach (IGalaxy galaxy in galaxiesResult.Result)
                    solarSystems.AddRange(galaxy.SolarSystems);

                result.Result = solarSystems;
            }

            OASISResult<IEnumerable<ISolarSystem>> solarSystemsOutsideResult = await GetAllSolarSystemsOutSideOfGalaxyClustersForMultiverseAsync(refresh);

            if (!solarSystemsOutsideResult.IsError)
                solarSystems.AddRange(solarSystemsOutsideResult.Result);

            solarSystemsOutsideResult = await GetAllSolarSystemsOutSideOfGalaxiesForMultiverseAsync(refresh);

            if (!solarSystemsOutsideResult.IsError)
                solarSystems.AddRange(solarSystemsOutsideResult.Result);

            result.Result = solarSystems;
            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForMultiverse(bool refresh = true)
        {
            return GetAllSolarSystemsForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxiesForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxyClustersResult, result);

            if (!galaxyClustersResult.IsError)
            {
                List<IStar> stars = new List<IStar>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    stars.AddRange(cluster.Stars);

                result.Result = stars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxiesForMultiverse(bool refresh = true)
        {
            return GetAllStarsOutSideOfGalaxiesForMultiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForMultiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IDimension>, IEnumerable<IStar>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<IStar> stars = new List<IStar>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            stars.AddRange(dimension.Stars);

        //        result.Result = stars;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(universesResult, result);

            if (!universesResult.IsError)
            {
                List<IStar> stars = new List<IStar>();

                foreach (IUniverse universe in universesResult.Result)
                    stars.AddRange(universe.Stars);

                result.Result = stars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxyClustersForMultiverse(bool refresh = true)
        {
            return GetAllStarsOutSideOfGalaxyClustersForMultiverseAsync(refresh).Result;
        }


        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(superStarsResult, result);
            List<IStar> stars = new List<IStar>();

            if (!superStarsResult.IsError)
            {
                foreach (ISuperStar superStar in superStarsResult.Result)
                {
                    OASISResult<IEnumerable<IStar>> starsResult = await ((ISuperStarCore)superStar.CelestialBodyCore).GetAllStarsForGalaxyAsync(refresh);

                    if (!starsResult.IsError)
                        stars.AddRange(starsResult.Result);
                }
            }

            OASISResult<IEnumerable<IStar>> starsOutsideResult = await GetAllStarsOutSideOfGalaxyClustersForMultiverseAsync(refresh);

            if (!starsOutsideResult.IsError)
                stars.AddRange(starsOutsideResult.Result);

            starsOutsideResult = await GetAllStarsOutSideOfGalaxiesForMultiverseAsync(refresh);

            if (!starsOutsideResult.IsError)
                stars.AddRange(starsOutsideResult.Result);

            result.Result = stars;
            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsForMultiverse(bool refresh = true)
        {
            return GetAllStarsForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxiesForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(galaxyClustersResult, result);

            if (!galaxyClustersResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    planets.AddRange(cluster.Planets);

                result.Result = planets;
            }

            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxiesForMultiverse(bool refresh = true)
        {
            return GetAllPlanetsOutSideOfGalaxiesForMultiverseAsync(refresh).Result;
        }
        /*
        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForMultiverseAsync(refresh);
            OASISResultHelper<IEnumerable<IDimension>, IEnumerable<IPlanet>>.CopyResult(dimensionsResult, ref result);

            if (!dimensionsResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IDimension dimension in dimensionsResult.Result)
                    planets.AddRange(dimension.Planets);

                result.Result = planets;
            }

            return result;
        }*/

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(universesResult, result);

            if (!universesResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IUniverse universe in universesResult.Result)
                    planets.AddRange(universe.Planets);

                result.Result = planets;
            }

            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxyClustersForMultiverse(bool refresh = true)
        {
            return GetAllPlanetsOutSideOfGalaxyClustersForMultiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(superStarsResult, result);
            List<IPlanet> planets = new List<IPlanet>();

            if (!superStarsResult.IsError)
            {
                foreach (ISuperStar superStar in superStarsResult.Result)
                {
                    OASISResult<IEnumerable<IPlanet>> planetsResult = await ((ISuperStarCore)superStar.CelestialBodyCore).GetAllPlanetsForGalaxyAsync(refresh);

                    if (!planetsResult.IsError)
                        planets.AddRange(planetsResult.Result);
                }
            }

            OASISResult<IEnumerable<IPlanet>> planetsOutsideResult = await GetAllPlanetsOutSideOfGalaxyClustersForMultiverseAsync(refresh);

            if (!planetsOutsideResult.IsError)
                planets.AddRange(planetsOutsideResult.Result);

            planetsOutsideResult = await GetAllPlanetsOutSideOfGalaxiesForMultiverseAsync(refresh);

            if (!planetsOutsideResult.IsError)
                planets.AddRange(planetsOutsideResult.Result);

            result.Result = planets;
            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForMultiverse(bool refresh = true)
        {
            return GetAllPlanetsForMultiverseAsync(refresh).Result;
        }


        /*
        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IStar>> starsResult = await GetAllStarsForMultiverseAsync(refresh);
            OASISResultHelper<IEnumerable<IStar>, IEnumerable<IPlanet>>.CopyResult(starsResult, ref result);

            if (!starsResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IStar star in starsResult.Result)
                {
                    OASISResult<IEnumerable<IPlanet>> planetsResult = await ((IStarCore)star.CelestialBodyCore).GetAllPlanetsForSolarSystemAsync(refresh);

                    if (!planetsResult.IsError)
                        planets.AddRange(planetsResult.Result);
                }

                result.Result = planets;
            }

            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForMultiverse(bool refresh = true)
        {
            return GetAllPlanetsForMultiverseAsync(refresh).Result;
        }*/

        public async Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForMultiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();
            OASISResult<IEnumerable<IPlanet>> planetsResult = await GetAllPlanetsForMultiverseAsync(refresh);
            OASISResultHelper.CopyResult(planetsResult, result);

            if (!planetsResult.IsError)
            {
                List<IMoon> moons = new List<IMoon>();

                foreach (IPlanet planet in planetsResult.Result)
                {
                    OASISResult<IEnumerable<IMoon>> moonsResult = await ((IPlanetCore)planet.CelestialBodyCore).GetMoonsAsync(refresh);

                    if (!moonsResult.IsError)
                        moons.AddRange(moonsResult.Result);
                }

                result.Result = moons;
            }

            return result;
        }

        public OASISResult<IEnumerable<IMoon>> GetAllMoonsForMultiverse(bool refresh = true)
        {
            return GetAllMoonsForMultiverseAsync(refresh).Result;
        }
    }
}