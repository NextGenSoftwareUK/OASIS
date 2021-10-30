using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.CelestialSpace;

namespace NextGenSoftware.OASIS.STAR
{
    // At the centre of the Omiverse (there can only be ONE) ;-) (creates Multiverses (with a GrandSuperStar/Prime Creator at the centre of each).  Spirit/God/The Divine, etc
    public class GreatGrandSuperStarCore : CelestialBodyCore, IGreatGrandSuperStarCore
    {
        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; }

        public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar)
        {
            GreatGrandSuperStar = greatGrandSuperStar;
        }

        public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {
            GreatGrandSuperStar = greatGrandSuperStar;
        }

        public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar, Guid id) : base(id)
        {
            GreatGrandSuperStar = greatGrandSuperStar;
        }

        public async Task<OASISResult<IOmiverse>> AddOmiverseAsync(IOmiverse omiverse)
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            OASISResult<IHolon> holonResult = await SaveHolonAsync(omiverse, false);

            if (!holonResult.IsError && holonResult.Result != null)
                result.Result = Mapper<IHolon, Omiverse>.MapBaseHolonProperties(holonResult.Result);
            else
                OASISResultHolonToHolonHelper<IHolon, IOmiverse>.CopyResult(holonResult, result);

            return result;
        }

        public OASISResult<IOmiverse> AddOmiverse(IOmiverse omiverse)
        {
            return AddOmiverseAsync(omiverse).Result;
        }

        public async Task<OASISResult<IDimension>> AddDimensionToOmniverseAsync(IDimension dimension)
        {
            return OASISResultHolonToHolonHelper<IHolon, IDimension>.CopyResult(
                await AddHolonToCollectionAsync(GreatGrandSuperStar, dimension, (List<IHolon>)Mapper<IDimension, Holon>.MapBaseHolonProperties(
                    GreatGrandSuperStar.ParentOmiverse.Dimensions.CustomDimensions)), new OASISResult<IDimension>());
        }

        public OASISResult<IDimension> AddDimensionToOmniverse(IDimension dimension)
        {
            return AddDimensionToOmniverseAsync(dimension).Result;
        }

        /// <summary>
        /// Create's a Multiverse within the Omniverse alomg with the ThirdDimension within this Multiverse along with a child MagicVerse and UniversePrime within the ThirdDimension.
        /// </summary>
        /// <returns></returns>
        public async Task<OASISResult<IMultiverse>> AddMultiverseAsync(IMultiverse multiverse)
        {
            OASISResult<IMultiverse> multiverseResult = OASISResultHolonToHolonHelper<IHolon, IMultiverse>.CopyResult(
               await AddHolonToCollectionAsync(GreatGrandSuperStar, multiverse, (List<IHolon>)Mapper<IMultiverse, Holon>.MapBaseHolonProperties(
                   GreatGrandSuperStar.ParentOmiverse.Multiverses)), new OASISResult<IMultiverse>());

            if (!multiverseResult.IsError && multiverseResult.Result != null)
            {
                Mapper<IHolon, Multiverse>.MapBaseHolonProperties(multiverseResult.Result, (Multiverse)multiverse);

                multiverseResult.Result.GrandSuperStar.ParentOmiverse = multiverse.ParentOmiverse;
                multiverseResult.Result.GrandSuperStar.ParentOmiverseId = multiverse.ParentOmiverseId;
                multiverseResult.Result.GrandSuperStar.ParentMultiverse = multiverse;
                multiverseResult.Result.GrandSuperStar.ParentMultiverseId = multiverse.Id;

                // Now we need to save the GrandSuperStar as a seperate Holon to get a Id.
                OASISResult<IHolon> grandSuperStarResult = await SaveHolonAsync(multiverseResult.Result.GrandSuperStar, false);

                if (!grandSuperStarResult.IsError && grandSuperStarResult.Result != null)
                {
                    Mapper<IHolon, GrandSuperStar>.MapBaseHolonProperties(grandSuperStarResult.Result, (GrandSuperStar)multiverseResult.Result.GrandSuperStar);

                    // The GrandSuperStar at the centre of the new Multiverse is resposnsible for creating its own child dimensions and universes.
                    // Create's the ThirdDimension within the new Multiverse along with a child MagicVerse and UniversePrime.
                    OASISResult<IThirdDimension> addThirdDimensionToMultiverseResult = await((GrandSuperStarCore)multiverseResult.Result.GrandSuperStar.CelestialBodyCore).AddThirdDimensionToMultiverseAsync();

                    if (!addThirdDimensionToMultiverseResult.IsError && addThirdDimensionToMultiverseResult.Result != null)
                        multiverseResult.Result.Dimensions.ThirdDimension = addThirdDimensionToMultiverseResult.Result;
                    else
                        OASISResultHolonToHolonHelper<IThirdDimension, IMultiverse>.CopyResult(addThirdDimensionToMultiverseResult, multiverseResult);
                }
                else
                    OASISResultHolonToHolonHelper<IHolon, IMultiverse>.CopyResult(grandSuperStarResult, multiverseResult);
            }

            //TODO: One day there may also be init code here for the other dimensions, etc.... ;-)

            return multiverseResult;
        }

        /*
        public async Task<OASISResult<IMultiverse>> AddMultiverseAsync(IMultiverse multiverse)
        {
            //return OASISResultHolonToHolonHelper<IHolon, IMultiverse>.CopyResult(
            //    await AddHolonToCollectionAsync(GreatGrandSuperStar, multiverse, (List<IHolon>)Mapper<IMultiverse, Holon>.MapBaseHolonProperties(
            //        GreatGrandSuperStar.ParentOmiverse.Multiverses)), new OASISResult<IMultiverse>());

            OASISResult<IMultiverse> multiverseResult = OASISResultHolonToHolonHelper<IHolon, IMultiverse>.CopyResult(
               await AddHolonToCollectionAsync(GreatGrandSuperStar, multiverse, (List<IHolon>)Mapper<IMultiverse, Holon>.MapBaseHolonProperties(
                   GreatGrandSuperStar.ParentOmiverse.Multiverses)), new OASISResult<IMultiverse>());

            if (!multiverseResult.IsError && multiverseResult.Result != null)
            {
                multiverseResult.Result.GrandSuperStar.ParentOmiverse = multiverse.ParentOmiverse;
                multiverseResult.Result.GrandSuperStar.ParentOmiverseId = multiverse.ParentOmiverseId;
                multiverseResult.Result.GrandSuperStar.ParentMultiverse = multiverse;
                multiverseResult.Result.GrandSuperStar.ParentMultiverseId = multiverse.Id;

                // Now we need to save the GrandSuperStar as a seperate Holon to get a Id.
                OASISResult<IHolon> grandSuperStarResult = await SaveHolonAsync(multiverseResult.Result.GrandSuperStar);

                if (!grandSuperStarResult.IsError && grandSuperStarResult.Result != null)
                {
                    Mapper<IHolon, GrandSuperStar>.MapBaseHolonProperties(grandSuperStarResult.Result, (GrandSuperStar)multiverseResult.Result.GrandSuperStar);

                    //TODO: I THINK THE GRAND SUPERSTAR SHOULD BE CREATING IT'S OWN DIMENSIONS AND UNIVERSES INSIDE ITS MULTIVERSE?
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentOmiverse = multiverse.ParentOmiverse;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentOmiverseId = multiverse.ParentOmiverseId;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentMultiverse = multiverse;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentMultiverseId = multiverse.Id;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentGrandSuperStar = (GrandSuperStar)grandSuperStarResult.Result;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentGrandSuperStarId = grandSuperStarResult.Result.Id;

                    // Now we need to save the ThirdDimension as a seperate Holon to get a Id.
                    OASISResult<IHolon> thirdDimensionResult = await SaveHolonAsync(multiverseResult.Result.Dimensions.ThirdDimension);

                    if (!thirdDimensionResult.IsError && thirdDimensionResult.Result != null)
                    {
                        Mapper<IHolon, ThirdDimension>.MapBaseHolonProperties(thirdDimensionResult.Result, (ThirdDimension)multiverseResult.Result.Dimensions.ThirdDimension);

                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentOmiverse = multiverse.ParentOmiverse;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentOmiverseId = multiverse.ParentOmiverseId;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentMultiverse = multiverse;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentMultiverseId = multiverse.Id;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentGrandSuperStar = (GrandSuperStar)grandSuperStarResult.Result;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentGrandSuperStarId = grandSuperStarResult.Result.Id;

                        // Now we need to save the MagicVerse as a seperate Holon to get a Id.
                        OASISResult<IHolon> magicVerseResult = await SaveHolonAsync(multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse);

                        if (!magicVerseResult.IsError && magicVerseResult.Result != null)
                        {
                            Mapper<IHolon, Universe>.MapBaseHolonProperties(thirdDimensionResult.Result, (Universe)multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse);

                            multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime.ParentOmiverse = multiverse.ParentOmiverse;
                            multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime.ParentOmiverseId = multiverse.ParentOmiverseId;
                            multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime.ParentMultiverse = multiverse;
                            multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime.ParentMultiverseId = multiverse.Id;
                            multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime.ParentGrandSuperStar = (GrandSuperStar)grandSuperStarResult.Result;
                            multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime.ParentGrandSuperStarId = grandSuperStarResult.Result.Id;

                            // Now we need to save the UniversePrime as a seperate Holon to get a Id.
                            OASISResult<IHolon> universePrimeResult = await SaveHolonAsync(multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime);

                            if (!universePrimeResult.IsError && universePrimeResult.Result != null)
                            {
                                Mapper<IHolon, Universe>.MapBaseHolonProperties(thirdDimensionResult.Result, (Universe)multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime);

                                //TODO: Do we need to re-save the new multiverse so its child holon ids are also saved within the multiverse holon object in storage?
                                OASISResult<IHolon> multiverseHolonResult = await SaveHolonAsync(multiverseResult.Result);

                                if (!multiverseHolonResult.IsError && multiverseHolonResult.Result != null)
                                    Mapper<IHolon, Multiverse>.MapBaseHolonProperties(multiverseHolonResult.Result, (Multiverse)multiverseResult.Result);
                                else
                                {
                                    multiverseResult.IsError = true;
                                    multiverseResult.Message = multiverseHolonResult.Message;
                                }
                            }
                            else
                            {
                                multiverseResult.IsError = true;
                                multiverseResult.Message = universePrimeResult.Message;
                            }
                        }
                        else
                        {
                            multiverseResult.IsError = true;
                            multiverseResult.Message = magicVerseResult.Message;
                        }
                    }
                    else
                    {
                        multiverseResult.IsError = true;
                        multiverseResult.Message = thirdDimensionResult.Message;
                    }
                }
                else
                {
                    multiverseResult.IsError = true;
                    multiverseResult.Message = grandSuperStarResult.Message;
                }
            }

            //TODO: One day there may also be init code here for the other dimensions, etc.... ;-)

            return multiverseResult;
        }*/

        public OASISResult<IMultiverse> AddMultiverse(IMultiverse multiverse)
        {
            return AddMultiverseAsync(multiverse).Result;
        }

        //TODO: Come back to this... ;-)
        /*
        public async Task<OASISResult<IMultiverse>> AddSuperverseToDimensionAsync(IOmniverseDimension dimension, ISuperVerse superverse)
        {
            dimension.SuperVerse = superverse;
            //return OASISResultHolonToHolonHelper<IHolon, ISuperVerse>.CopyResult(
            //    await AddHolonToCollectionAsync(GreatGrandSuperStar, superverse, (List<IHolon>)Mapper<ISuperVerse, Holon>.MapBaseHolonProperties(
            //        dimension.SuperVerse)), new OASISResult<ISuperVerse>());
        }

        public OASISResult<IMultiverse> AddSuperverse(ISuperVerse superverse)
        {
            return AddSuperverseAsync(superverse).Result;
        }*/

        public async Task<OASISResult<IEnumerable<IMultiverse>>> GetAllMultiversesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMultiverse>> result = new OASISResult<IEnumerable<IMultiverse>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GreatGrandSuperStar.ParentOmiverse.Multiverses, HolonType.Multiverse, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IMultiverse>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Multiverse>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IMultiverse>> GetAllMultiversesForOmiverse(bool refresh = true)
        {
            return GetAllMultiversesForOmiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
        //    OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiversesForOmiverseAsync(refresh);
        //    OASISResultCollectionToCollectionHelper<IEnumerable<IMultiverse>, IEnumerable<IUniverse>>.CopyResult(multiversesResult, ref result);

        //    if (!multiversesResult.IsError)
        //    {
        //        List<IUniverse> universe = new List<IUniverse>();

        //        foreach (IMultiverse multiverse in multiversesResult.Result)
        //            universe.AddRange(multiverse.Universes);

        //        result.Result = universe;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
            OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IMultiverse>, IEnumerable<IUniverse>>.CopyResult(multiversesResult, ref result);
            List<IUniverse> universes = new List<IUniverse>();

            if (!multiversesResult.IsError)
            {
                foreach (IMultiverse multiverse in multiversesResult.Result)
                {
                    universes.Add(multiverse.Dimensions.FirstDimension.Universe);
                    universes.Add(multiverse.Dimensions.SecondDimension.Universe);
                    universes.Add(multiverse.Dimensions.ThirdDimension.UniversePrime);
                    universes.Add(multiverse.Dimensions.ThirdDimension.MagicVerse);
                    universes.AddRange(multiverse.Dimensions.ThirdDimension.ParallelUniverses);
                    universes.Add(multiverse.Dimensions.FourthDimension.Universe);
                    universes.Add(multiverse.Dimensions.FifthDimension.Universe);
                    universes.Add(multiverse.Dimensions.SixthDimension.Universe);
                    universes.Add(multiverse.Dimensions.SeventhDimension.Universe);
                }
            }

            universes.AddRange(GreatGrandSuperStar.ParentOmiverse.Dimensions.EighthDimension.SuperVerse.Universes);
            universes.AddRange(GreatGrandSuperStar.ParentOmiverse.Dimensions.NinthDimension.SuperVerse.Universes);
            universes.AddRange(GreatGrandSuperStar.ParentOmiverse.Dimensions.TenthDimension.SuperVerse.Universes);
            universes.AddRange(GreatGrandSuperStar.ParentOmiverse.Dimensions.EleventhDimension.SuperVerse.Universes);
            universes.AddRange(GreatGrandSuperStar.ParentOmiverse.Dimensions.TwelfthDimension.SuperVerse.Universes);

            result.Result = universes;
            return result;
        }

        public OASISResult<IEnumerable<IUniverse>> GetAllUniversesForOmiverse(bool refresh = true)
        {
            return GetAllUniversesForOmiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IDimension>> result = new OASISResult<IEnumerable<IDimension>>();
        //    OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
        //    OASISResultCollectionToCollectionHelper<IEnumerable<IUniverse>, IEnumerable<IDimension>>.CopyResult(universesResult, ref result);

        //    if (!universesResult.IsError)
        //    {
        //        List<IDimension> dimensions = new List<IDimension>();

        //        foreach (IUniverse universe in universesResult.Result)
        //            dimensions.AddRange(universe.Dimensions);

        //        result.Result = dimensions;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IDimension>> result = new OASISResult<IEnumerable<IDimension>>();
            OASISResult<IEnumerable<IMultiverse>> multiveresesResult = await GetAllMultiversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IMultiverse>, IEnumerable<IDimension>>.CopyResult(multiveresesResult, ref result);

            if (!multiveresesResult.IsError)
            {
                List<IDimension> dimensions = new List<IDimension>();

                //First add all of the dimensions contain inside each of the Multiverses.
                foreach (IMultiverse multiverse in multiveresesResult.Result)
                {
                    dimensions.Add(multiverse.Dimensions.FirstDimension);
                    dimensions.Add(multiverse.Dimensions.SecondDimension);
                    dimensions.Add(multiverse.Dimensions.ThirdDimension);
                    dimensions.Add(multiverse.Dimensions.FourthDimension);
                    dimensions.Add(multiverse.Dimensions.FifthDimension);
                    dimensions.Add(multiverse.Dimensions.SixthDimension);
                    dimensions.Add(multiverse.Dimensions.SeventhDimension);
                    dimensions.AddRange(multiverse.Dimensions.CustomDimensions);
                }

                //Now add the Omiverse Dimensions (exist outside of the multiverses and spam across the entire Omiverse).
                dimensions.Add(GreatGrandSuperStar.ParentOmiverse.Dimensions.EighthDimension);
                dimensions.Add(GreatGrandSuperStar.ParentOmiverse.Dimensions.NinthDimension);
                dimensions.Add(GreatGrandSuperStar.ParentOmiverse.Dimensions.TenthDimension);
                dimensions.Add(GreatGrandSuperStar.ParentOmiverse.Dimensions.EleventhDimension);
                dimensions.Add(GreatGrandSuperStar.ParentOmiverse.Dimensions.TwelfthDimension);
                dimensions.AddRange(GreatGrandSuperStar.ParentOmiverse.Dimensions.CustomDimensions);

                result.Result = dimensions;
            }

            return result;
        }

        public OASISResult<IEnumerable<IDimension>> GetAllDimensionsForOmiverse(bool refresh = true)
        {
            return GetAllDimensionsForOmiverseAsync(refresh).Result;
        }



        //public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IGalaxyCluster>> result = new OASISResult<IEnumerable<IGalaxyCluster>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
        //    OASISResultCollectionToCollectionHelper<IEnumerable<IDimension>, IEnumerable<IGalaxyCluster>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<IGalaxyCluster> clusters = new List<IGalaxyCluster>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            clusters.AddRange(dimension.GalaxyClusters);

        //        result.Result = clusters;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxyCluster>> result = new OASISResult<IEnumerable<IGalaxyCluster>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IUniverse>, IEnumerable<IGalaxyCluster>>.CopyResult(universesResult, ref result);

            if (!universesResult.IsError)
            {
                List<IGalaxyCluster> clusters = new List<IGalaxyCluster>();

                foreach (IUniverse universe in universesResult.Result)
                    clusters.AddRange(universe.GalaxyClusters);

                result.Result = clusters;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGalaxyCluster>> GetAllGalaxyClustersForOmiverse(bool refresh = true)
        {
            return GetAllGalaxyClustersForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetAllGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxy>> result = new OASISResult<IEnumerable<IGalaxy>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxyCluster>, IEnumerable<IGalaxy>>.CopyResult(galaxyClustersResult, ref result);

            if (!galaxyClustersResult.IsError)
            {
                List<IGalaxy> galaxies = new List<IGalaxy>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    galaxies.AddRange(cluster.Galaxies);

                result.Result = galaxies;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGalaxy>> GetAllGalaxiesForOmiverse(bool refresh = true)
        {
            return GetAllGalaxiesForOmiverseAsync(refresh).Result;
        }

        /*
        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetAllGalaxiesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxy>, IEnumerable<ISolarSystem>>.CopyResult(galaxiesResult, ref result);

            if (!galaxiesResult.IsError)
            {
                List<ISolarSystem> solarSystems = new List<ISolarSystem>();

                foreach (IGalaxy galaxy in galaxiesResult.Result)
                    solarSystems.AddRange(galaxy.SolarSystems);

                result.Result = solarSystems;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForOmiverse(bool refresh = true)
        {
            return GetAllSolarSystemsForOmiverseAsync(refresh).Result;
        }*/

        // Helper method to get the GrandSuperStars at the centre of each Multiverse.
        public async Task<OASISResult<IEnumerable<IGrandSuperStar>>> GetAllGrandSuperStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGrandSuperStar>> result = new OASISResult<IEnumerable<IGrandSuperStar>>();
            OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IMultiverse>, IEnumerable<IGrandSuperStar>>.CopyResult(multiversesResult, ref result);

            if (!multiversesResult.IsError)
            {
                List<IGrandSuperStar> grandSuperstars = new List<IGrandSuperStar>();

                foreach (IMultiverse multiverse in multiversesResult.Result)
                    grandSuperstars.Add(multiverse.GrandSuperStar);

                result.Result = grandSuperstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGrandSuperStar>> GetAllGrandSuperStarsForOmiverse(bool refresh = true)
        {
            return GetAllGrandSuperStarsForOmiverseAsync(refresh).Result;
        }

        // Helper method to get the SuperStars at the centre of each Galaxy.
        public async Task<OASISResult<IEnumerable<ISuperStar>>> GetAllSuperStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISuperStar>> result = new OASISResult<IEnumerable<ISuperStar>>();
            OASISResult<IEnumerable<IGrandSuperStar>> grandSuperStarsResult = await GetAllGrandSuperStarsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGrandSuperStar>, IEnumerable<ISuperStar>>.CopyResult(grandSuperStarsResult, ref result);

            if (!grandSuperStarsResult.IsError)
            {
                List<ISuperStar> superstars = new List<ISuperStar>();

                foreach (IGrandSuperStar grandSuperStar in grandSuperStarsResult.Result)
                {
                    OASISResult<IEnumerable<ISuperStar>> superStarsResult = await ((IGrandSuperStarCore)grandSuperStar.CelestialBodyCore).GetAllSuperStarsForMultiverseAsync(refresh);

                    if (!superStarsResult.IsError)
                        superstars.AddRange(superStarsResult.Result);
                }

                result.Result = superstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISuperStar>> GetAllSuperStarsForOmiverse(bool refresh = true)
        {
            return GetAllSuperStarsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxyCluster>, IEnumerable<ISolarSystem>>.CopyResult(galaxyClustersResult, ref result);

            if (!galaxyClustersResult.IsError)
            {
                List<ISolarSystem> solarSystems = new List<ISolarSystem>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    solarSystems.AddRange(cluster.SoloarSystems);

                result.Result = solarSystems;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxiesForOmiverse(bool refresh = true)
        {
            return GetAllSolarSystemsOutSideOfGalaxiesForOmiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
        //    OASISResultCollectionToCollectionHelper<IEnumerable<IDimension>, IEnumerable<ISolarSystem>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<ISolarSystem> solarSystems = new List<ISolarSystem>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            solarSystems.AddRange(dimension.SoloarSystems);

        //        result.Result = solarSystems;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IUniverse>, IEnumerable<ISolarSystem>>.CopyResult(universesResult, ref result);

            if (!universesResult.IsError)
            {
                List<ISolarSystem> solarSystems = new List<ISolarSystem>();

                foreach (IUniverse universe in universesResult.Result)
                    solarSystems.AddRange(universe.SolarSystems);

                result.Result = solarSystems;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverse(bool refresh = true)
        {
            return GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetAllGalaxiesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxy>, IEnumerable<ISolarSystem>>.CopyResult(galaxiesResult, ref result);
            List<ISolarSystem> solarSystems = new List<ISolarSystem>();

            if (!galaxiesResult.IsError)
            {
                foreach (IGalaxy galaxy in galaxiesResult.Result)
                    solarSystems.AddRange(galaxy.SolarSystems);

                result.Result = solarSystems;
            }

            OASISResult<IEnumerable<ISolarSystem>> solarSystemsOutsideResult = await GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(refresh);

            if (!solarSystemsOutsideResult.IsError)
                solarSystems.AddRange(solarSystemsOutsideResult.Result);

            solarSystemsOutsideResult = await GetAllSolarSystemsOutSideOfGalaxiesForOmiverseAsync(refresh);

            if (!solarSystemsOutsideResult.IsError)
                solarSystems.AddRange(solarSystemsOutsideResult.Result);

            result.Result = solarSystems;
            return result;
        }

        public OASISResult<IEnumerable<ISolarSystem>> GetAllSolarSystemsForOmiverse(bool refresh = true)
        {
            return GetAllSolarSystemsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxyCluster>, IEnumerable<IStar>>.CopyResult(galaxyClustersResult, ref result);

            if (!galaxyClustersResult.IsError)
            {
                List<IStar> stars = new List<IStar>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    stars.AddRange(cluster.Stars);

                result.Result = stars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxiesForOmiverse(bool refresh = true)
        {
            return GetAllStarsOutSideOfGalaxiesForOmiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
        //    OASISResultCollectionToCollectionHelper<IEnumerable<IDimension>, IEnumerable<IStar>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<IStar> stars = new List<IStar>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            stars.AddRange(dimension.Stars);

        //        result.Result = stars;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IUniverse>, IEnumerable<IStar>>.CopyResult(universesResult, ref result);

            if (!universesResult.IsError)
            {
                List<IStar> stars = new List<IStar>();

                foreach (IUniverse universe in universesResult.Result)
                    stars.AddRange(universe.Stars);

                result.Result = stars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsOutSideOfGalaxyClustersForOmiverse(bool refresh = true)
        {
            return GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<ISuperStar>, IEnumerable<IStar>>.CopyResult(superStarsResult, ref result);
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

            OASISResult<IEnumerable<IStar>> starsOutsideResult = await GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(refresh);

            if (!starsOutsideResult.IsError)
                stars.AddRange(starsOutsideResult.Result);

            starsOutsideResult = await GetAllStarsOutSideOfGalaxiesForOmiverseAsync(refresh);

            if (!starsOutsideResult.IsError)
                stars.AddRange(starsOutsideResult.Result);

            result.Result = stars;
            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsForOmiverse(bool refresh = true)
        {
            return GetAllStarsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxiesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IGalaxyCluster>> galaxyClustersResult = await GetAllGalaxyClustersForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxyCluster>, IEnumerable<IPlanet>>.CopyResult(galaxyClustersResult, ref result);

            if (!galaxyClustersResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IGalaxyCluster cluster in galaxyClustersResult.Result)
                    planets.AddRange(cluster.Planets);

                result.Result = planets;
            }

            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxiesForOmiverse(bool refresh = true)
        {
            return GetAllPlanetsOutSideOfGalaxiesForOmiverseAsync(refresh).Result;
        }

        //public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
        //    OASISResultCollectionToCollectionHelper<IEnumerable<IDimension>, IEnumerable<IPlanet>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<IPlanet> planets = new List<IPlanet>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            planets.AddRange(dimension.Planets);

        //        result.Result = planets;
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IUniverse>, IEnumerable<IPlanet>>.CopyResult(universesResult, ref result);

            if (!universesResult.IsError)
            {
                List<IPlanet> planets = new List<IPlanet>();

                foreach (IUniverse universe in universesResult.Result)
                    planets.AddRange(universe.Planets);

                result.Result = planets;
            }

            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverse(bool refresh = true)
        {
            return GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<ISuperStar>, IEnumerable<IPlanet>>.CopyResult(superStarsResult, ref result);
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

            OASISResult<IEnumerable<IPlanet>> planetsOutsideResult = await GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(refresh);

            if (!planetsOutsideResult.IsError)
                planets.AddRange(planetsOutsideResult.Result);

            planetsOutsideResult = await GetAllPlanetsOutSideOfGalaxiesForOmiverseAsync(refresh);

            if (!planetsOutsideResult.IsError)
                planets.AddRange(planetsOutsideResult.Result);

            result.Result = planets;
            return result;
        }

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForOmiverse(bool refresh = true)
        {
            return GetAllPlanetsForOmiverseAsync(refresh).Result;
        }


        /*
        public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<ISuperStar>> superStarsResult = await GetAllSuperStarsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<ISuperStar>, IEnumerable<IStar>>.CopyResult(superStarsResult, ref result);

            if (!superStarsResult.IsError)
            {
                List<IStar> stars = new List<IStar>();

                foreach (ISuperStar superStar in superStarsResult.Result)
                {
                    OASISResult<IEnumerable<IStar>> starsResult = await ((ISuperStarCore)superStar.CelestialBodyCore).GetAllStarsForGalaxyAsync(refresh);

                    if (!starsResult.IsError)
                        stars.AddRange(starsResult.Result);
                }

                result.Result = stars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetAllStarsForOmiverse(bool refresh = true)
        {
            return GetAllStarsForOmiverseAsync(refresh).Result;
        }

        public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
            OASISResult<IEnumerable<IStar>> starsResult = await GetAllStarsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IStar>, IEnumerable<IPlanet>>.CopyResult(starsResult, ref result);

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

        public OASISResult<IEnumerable<IPlanet>> GetAllPlanetsForOmiverse(bool refresh = true)
        {
            return GetAllPlanetsForOmiverseAsync(refresh).Result;
        }*/


        public async Task<OASISResult<IEnumerable<IMoon>>> GetAllMoonsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IMoon>> result = new OASISResult<IEnumerable<IMoon>>();
            OASISResult<IEnumerable<IPlanet>> planetsResult = await GetAllPlanetsForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IPlanet>, IEnumerable<IMoon>>.CopyResult(planetsResult, ref result);

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

        public OASISResult<IEnumerable<IZome>> GetAllZomesForOmiverse(bool refresh = true)
        {
            return GetAllZomesForOmiverseAsync(refresh).Result;
        }

        //TODO: Come back to this! :)
        public async Task<OASISResult<IEnumerable<IZome>>> GetAllZomesForOmiverseAsync(bool refresh = true)
        {
            List<IZome> zomes = new List<IZome>();
            OASISResult<IEnumerable<IZome>> result = new OASISResult<IEnumerable<IZome>>();
            OASISResult<IEnumerable<IStar>> starsResult = await GetAllStarsForOmiverseAsync(refresh);
            OASISResult<IEnumerable<IPlanet>> planetsResult = await GetAllPlanetsForOmiverseAsync(refresh);
            OASISResult<IEnumerable<IMoon>> moonsResult = await GetAllMoonsForOmiverseAsync(refresh);
            //OASISResultCollectionToCollectionHelper<IEnumerable<IMoon>, IEnumerable<IZome>>.CopyResult(moonsResult, ref result);

            if (!moonsResult.IsError)
            {
                foreach (IMoon moon in moonsResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await ((IMoonCore)moon.CelestialBodyCore).LoadZomesAsync();

                    if (!zomesResult.IsError)
                        zomes.AddRange(zomesResult.Result);

                    if (moon.ParentPlanet.CelestialBodyCore.Zomes != null)
                        zomes.AddRange(moon.ParentPlanet.CelestialBodyCore.Zomes);
                    else
                    {
                        OASISResult<IEnumerable<IZome>> planetZomesResult = await moon.ParentPlanet.LoadZomesAsync();

                        if (!planetZomesResult.IsError && planetZomesResult.Result != null)
                            zomes.AddRange(planetZomesResult.Result);
                    }

                    /*
                    if (moon.ParentStar.CelestialBodyCore.Zomes != null)
                        zomes.AddRange(moon.ParentStar.CelestialBodyCore.Zomes);
                    else
                    {
                        OASISResult<IEnumerable<IZome>> starZomesResult = await moon.ParentStar.LoadZomesAsync();

                        if (!starZomesResult.IsError && starZomesResult.Result != null)
                            zomes.AddRange(starZomesResult.Result);
                    }*/
                }

                result.Result = zomes;
            }

            //TODO: Think this way is better than what is commented out above?
            if (!planetsResult.IsError)
            {
                foreach (IPlanet planet in planetsResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await ((IPlanetCore)planet.CelestialBodyCore).LoadZomesAsync();

                    if (!zomesResult.IsError)
                        zomes.AddRange(zomesResult.Result);
                }
            }

            if (!starsResult.IsError)
            {
                foreach (IStar star in starsResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await ((IStarCore)star.CelestialBodyCore).LoadZomesAsync();

                    if (!zomesResult.IsError)
                        zomes.AddRange(zomesResult.Result);
                }
            }

            //TODO: Be good to get this working so it will be 4 lines of code instead of 9 for each collection! :)
            //OASISResult<IEnumerable<ICelestialBody>> celestialBodyResult = new OASISResult<IEnumerable<ICelestialBody>>();
            //OASISResultCollectionToCollectionHelper<IEnumerable<IMoon>, IEnumerable<ICelestialBody>>.CopyResult(moonsResult, ref celestialBodyResult);
            //celestialBodyResult.Result = Mapper<IMoon, CelestialBody>.MapBaseHolonProperties(moonsResult.Result);
            //zomes.AddRange(await LoadAlLZomesForCelestialBody(celestialBodyResult));

            result.Result = zomes;
            return result;
        }

        private async Task<List<IZome>> LoadAlLZomesForCelestialBody(OASISResult<IEnumerable<ICelestialBody>> celestialbodiesResult)
        {
            List<IZome> zomes = new List<IZome>();

            if (!celestialbodiesResult.IsError)
            {
                foreach (ICelestialBody celestialBody in celestialbodiesResult.Result)
                {
                    OASISResult<IEnumerable<IZome>> zomesResult = await celestialBody.CelestialBodyCore.LoadZomesAsync();

                    if (!zomesResult.IsError && zomesResult.Result != null)
                        zomes.AddRange(zomesResult.Result);
                }
            }

            return zomes;
        }

        public OASISResult<IEnumerable<IMoon>> GetAllMoonsForOmiverse(bool refresh = true)
        {
            return GetAllMoonsForOmiverseAsync(refresh).Result;
        }

        /*
        public async Task<OASISResult<IEnumerable<ICelestialHolon>>> GetCelestialCollection(OASISResult<IEnumerable<ICelestialHolon>> parentCollection, bool refresh = true)
        {
            OASISResult<IEnumerable<ICelestialHolon>> result = new OASISResult<IEnumerable<ICelestialHolon>>();
            //OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiverseForOmiverseAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<ICelestialHolon>, IEnumerable<ICelestialHolon>>.CopyResult(parentCollection, ref result);

            if (!parentCollection.IsError)
            {
                List<ICelestialHolon> children = new List<ICelestialHolon>();

                foreach (ICelestialHolon parent in parentCollection.Result)
                    children.AddRange(parent.Universes); //TODO: Need to pass in a dyanmic property name somehow? If we can work out how to make this work we can save a lot of code with this generic method! ;-)

                result.Result = children;
            }

            return result;
        }*/
    }
}