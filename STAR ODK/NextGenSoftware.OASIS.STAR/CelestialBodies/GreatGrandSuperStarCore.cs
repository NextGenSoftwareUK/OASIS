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
    // At the centre of the Omniverse (there can only be ONE) ;-) (creates Multiverses (with a GrandSuperStar/Prime Creator at the centre of each).  Spirit/God/The Divine, etc
    public partial class GreatGrandSuperStarCore : CelestialBodyCore<Star>, IGreatGrandSuperStarCore
    {


        //public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        //{
        //    GreatGrandSuperStar = greatGrandSuperStar;
        //}




        /*
        public async Task<OASISResult<IOmiverse>> AddOmiverseAsync(IOmiverse omniverse)
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            OASISResult<Omniverse> holonResult = await SaveHolonAsync<Omniverse>(omniverse, false);

            if (!result.IsError && holonResult.Result != null)
                result.Result = Mapper<IHolon, Omniverse>.MapBaseHolonProperties(holonResult.Result);
            else
                OASISResultHelper<Omniverse, IOmiverse>.CopyResult(holonResult, result);

            return result;
        }






        /*
        public async Task<OASISResult<IMultiverse>> AddMultiverseAsync(IMultiverse multiverse)
        {
            //return OASISResultHelper<IHolon, IMultiverse>.CopyResult(
            //    await AddHolonToCollectionAsync(GreatGrandSuperStar, multiverse, (List<IHolon>)Mapper<IMultiverse, Holon>.MapBaseHolonProperties(
            //        GreatGrandSuperStar.ParentOmniverse.Multiverses)), new OASISResult<IMultiverse>());

            OASISResult<IMultiverse> multiverseResult = OASISResultHelper<IHolon, IMultiverse>.CopyResult(
               await AddHolonToCollectionAsync(GreatGrandSuperStar, multiverse, (List<IHolon>)Mapper<IMultiverse, Holon>.MapBaseHolonProperties(
                   GreatGrandSuperStar.ParentOmniverse.Multiverses)), new OASISResult<IMultiverse>());

            if (!multiverseResult.IsError && multiverseResult.Result != null)
            {
                multiverseResult.Result.GrandSuperStar.ParentOmniverse = multiverse.ParentOmniverse;
                multiverseResult.Result.GrandSuperStar.ParentOmniverseId = multiverse.ParentOmniverseId;
                multiverseResult.Result.GrandSuperStar.ParentMultiverse = multiverse;
                multiverseResult.Result.GrandSuperStar.ParentMultiverseId = multiverse.Id;

                // Now we need to save the GrandSuperStar as a seperate Holon to get a Id.
                OASISResult<IHolon> grandSuperStarResult = await SaveHolonAsync(multiverseResult.Result.GrandSuperStar);

                if (!grandSuperStarResult.IsError && grandSuperStarResult.Result != null)
                {
                    Mapper<IHolon, GrandSuperStar>.MapBaseHolonProperties(grandSuperStarResult.Result, (GrandSuperStar)multiverseResult.Result.GrandSuperStar);

                    //TODO: I THINK THE GRAND SUPERSTAR SHOULD BE CREATING IT'S OWN DIMENSIONS AND UNIVERSES INSIDE ITS MULTIVERSE?
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentOmniverse = multiverse.ParentOmniverse;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentOmniverseId = multiverse.ParentOmniverseId;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentMultiverse = multiverse;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentMultiverseId = multiverse.Id;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentGrandSuperStar = (GrandSuperStar)grandSuperStarResult.Result;
                    multiverseResult.Result.Dimensions.ThirdDimension.ParentGrandSuperStarId = grandSuperStarResult.Result.Id;

                    // Now we need to save the ThirdDimension as a seperate Holon to get a Id.
                    OASISResult<IHolon> thirdDimensionResult = await SaveHolonAsync(multiverseResult.Result.Dimensions.ThirdDimension);

                    if (!thirdDimensionResult.IsError && thirdDimensionResult.Result != null)
                    {
                        Mapper<IHolon, ThirdDimension>.MapBaseHolonProperties(thirdDimensionResult.Result, (ThirdDimension)multiverseResult.Result.Dimensions.ThirdDimension);

                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentOmniverse = multiverse.ParentOmniverse;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentOmniverseId = multiverse.ParentOmniverseId;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentMultiverse = multiverse;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentMultiverseId = multiverse.Id;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentGrandSuperStar = (GrandSuperStar)grandSuperStarResult.Result;
                        multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse.ParentGrandSuperStarId = grandSuperStarResult.Result.Id;

                        // Now we need to save the MagicVerse as a seperate Holon to get a Id.
                        OASISResult<IHolon> magicVerseResult = await SaveHolonAsync(multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse);

                        if (!magicVerseResult.IsError && magicVerseResult.Result != null)
                        {
                            Mapper<IHolon, Universe>.MapBaseHolonProperties(thirdDimensionResult.Result, (Universe)multiverseResult.Result.Dimensions.ThirdDimension.MagicVerse);

                            multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime.ParentOmniverse = multiverse.ParentOmniverse;
                            multiverseResult.Result.Dimensions.ThirdDimension.UniversePrime.ParentOmniverseId = multiverse.ParentOmniverseId;
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






        //public async Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
        //    OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiversesForOmiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IMultiverse>, IEnumerable<IUniverse>>.CopyResult(multiversesResult, ref result);

        //    if (!multiversesResult.IsError)
        //    {
        //        List<IUniverse> universe = new List<IUniverse>();

        //        foreach (IMultiverse multiverse in multiversesResult.Result)
        //            universe.AddRange(multiverse.Universes);

        //        result.Result = universe;
        //    }

        //    return result;
        //}



        //public async Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IDimension>> result = new OASISResult<IEnumerable<IDimension>>();
        //    OASISResult<IEnumerable<IUniverse>> universesResult = await GetAllUniversesForOmiverseAsync(refresh);
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





        //public async Task<OASISResult<IEnumerable<IGalaxyCluster>>> GetAllGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IGalaxyCluster>> result = new OASISResult<IEnumerable<IGalaxyCluster>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
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













        //public async Task<OASISResult<IEnumerable<ISolarSystem>>> GetAllSolarSystemsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<ISolarSystem>> result = new OASISResult<IEnumerable<ISolarSystem>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
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







        //public async Task<OASISResult<IEnumerable<IStar>>> GetAllStarsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
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







        //public async Task<OASISResult<IEnumerable<IPlanet>>> GetAllPlanetsOutSideOfGalaxyClustersForOmiverseAsync(bool refresh = true)
        //{
        //    OASISResult<IEnumerable<IPlanet>> result = new OASISResult<IEnumerable<IPlanet>>();
        //    OASISResult<IEnumerable<IDimension>> dimensionsResult = await GetAllDimensionsForOmiverseAsync(refresh);
        //    OASISResultHelper<IEnumerable<IDimension>, IEnumerable<IPlanet>>.CopyResult(dimensionsResult, ref result);

        //    if (!dimensionsResult.IsError)
        //    {
        //        List<IPlanet> planets = new List<IPlanet>();

        //        foreach (IDimension dimension in dimensionsResult.Result)
        //            planets.AddRange(dimension.Planets);

        //        result.Result = planets;
        //    }

        //    return result;
        //}
















    }
}
