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
    public partial class GreatGrandSuperStarCore
    {
        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; }

        public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar)
        {
            GreatGrandSuperStar = greatGrandSuperStar;
        }

        //public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar, Dictionary<ProviderType, string> providerKey) : base(providerKey)
        //{
        //    GreatGrandSuperStar = greatGrandSuperStar;
        //}

        public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar, string providerKey, ProviderType providerType) : base(providerKey, providerType)
        {
            GreatGrandSuperStar = greatGrandSuperStar;
        }

        public GreatGrandSuperStarCore(IGreatGrandSuperStar greatGrandSuperStar, Guid id) : base(id)
        {
            GreatGrandSuperStar = greatGrandSuperStar;
        }

        public async Task<OASISResult<IOmiverse>> AddOmiverseAsync(IOmiverse omniverse)
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            OASISResult<IHolon> holonResult = await GlobalHolonData.SaveHolonAsync(omniverse, false);

            if (!holonResult.IsError && holonResult.Result != null)
                result.Result = (IOmiverse)holonResult.Result;
            else
                OASISResultHelper.CopyResult(holonResult, result);

            return result;
        }

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

        public async Task<OASISResult<Omniverse>> AddOmiverseAsync(IOmiverse omniverse)
        {
            return await SaveHolonAsync<Omniverse>(omniverse, false);
        }*/

        public OASISResult<IOmiverse> AddOmiverse(IOmiverse omniverse)
        {
            return AddOmiverseAsync(omniverse).Result;
        }

        public async Task<OASISResult<IDimension>> AddDimensionToOmniverseAsync(IDimension dimension)
        {
            return OASISResultHelper.CopyResult(
                await GlobalHolonData.AddHolonToCollectionAsync(GreatGrandSuperStar, dimension, (List<IHolon>)Mapper<IDimension, Holon>.MapBaseHolonProperties(
                    GreatGrandSuperStar.ParentOmniverse.Dimensions.CustomDimensions)), new OASISResult<IDimension>());
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
            if (multiverse.GrandSuperStar == null)
                multiverse.GrandSuperStar = new GrandSuperStar();

            if (multiverse.GrandSuperStar.Id == Guid.Empty)
            {
                multiverse.GrandSuperStar.Id = Guid.NewGuid();
                // multiverse.GrandSuperStar.IsNewHolon = true;  — now redundant: MatchedCount fallback in HolonRepository handles first insert.
            }

            multiverse.ParentGrandSuperStar = multiverse.GrandSuperStar;
            multiverse.ParentGrandSuperStarId = multiverse.GrandSuperStar.Id;

            OASISResult<IHolon> holonResult =  await GlobalHolonData.AddHolonToCollectionAsync(GreatGrandSuperStar, multiverse, (List<IHolon>)Mapper<IMultiverse, Holon>.Convert(GreatGrandSuperStar.ParentOmniverse.Multiverses));
            OASISResult<IMultiverse> multiverseResult = OASISResultHelper.CopyResult(holonResult, new OASISResult<IMultiverse>());
            multiverseResult.Result = (IMultiverse)holonResult.Result;

            if (!multiverseResult.IsError && multiverseResult.Result != null)
            {
                Mapper<IMultiverse, GrandSuperStar>.MapParentCelestialBodyProperties(multiverseResult.Result, (GrandSuperStar)multiverseResult.Result.GrandSuperStar);
                multiverseResult.Result.GrandSuperStar.ParentMultiverse = multiverseResult.Result;
                multiverseResult.Result.GrandSuperStar.ParentMultiverseId = multiverseResult.Result.Id;
                multiverseResult.Result.GrandSuperStar.ParentGrandSuperStar = null;
                multiverseResult.Result.GrandSuperStar.ParentGrandSuperStarId = Guid.Empty;

                // Now we need to save the GrandSuperStar as a seperate Holon to get a Id.
                OASISResult<IHolon> grandSuperStarResult = await GlobalHolonData.SaveHolonAsync(multiverseResult.Result.GrandSuperStar, false);

                if (!grandSuperStarResult.IsError && grandSuperStarResult.Result != null)
                {
                    //Mapper<IHolon, GrandSuperStar>.MapBaseHolonProperties(grandSuperStarResult.Result, (GrandSuperStar)multiverseResult.Result.GrandSuperStar, false);

                    // The GrandSuperStar at the centre of the new Multiverse is resposnsible for creating its own child dimensions and universes.
                    // Create's the ThirdDimension within the new Multiverse along with a child MagicVerse and UniversePrime.
                    OASISResult<IThirdDimension> addThirdDimensionToMultiverseResult = await((GrandSuperStarCore)multiverseResult.Result.GrandSuperStar.CelestialBodyCore).AddThirdDimensionToMultiverseAsync();

                    if (!addThirdDimensionToMultiverseResult.IsError && addThirdDimensionToMultiverseResult.Result != null)
                        multiverseResult.Result.Dimensions.ThirdDimension = addThirdDimensionToMultiverseResult.Result;
                    else
                        OASISResultHelper.CopyResult(addThirdDimensionToMultiverseResult, multiverseResult);
                }
                else
                    OASISResultHelper.CopyResult(grandSuperStarResult, multiverseResult);
            }

            //TODO: One day there may also be init code here for the other dimensions, etc.... ;-)

            return multiverseResult;
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

        public OASISResult<IMultiverse> AddMultiverse(IMultiverse multiverse)
        {
            return AddMultiverseAsync(multiverse).Result;
        }

        //TODO: Come back to this... ;-)
        /*
        public async Task<OASISResult<IMultiverse>> AddSuperverseToDimensionAsync(IOmniverseDimension dimension, ISuperVerse superverse)
        {
            dimension.SuperVerse = superverse;
            //return OASISResultHelper<IHolon, ISuperVerse>.CopyResult(
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
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GreatGrandSuperStar.ParentOmniverse.Multiverses, HolonType.Multiverse, refresh);
            OASISResultHelper.CopyResult(holonResult, result);
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

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetAllUniversesForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
            OASISResult<IEnumerable<IMultiverse>> multiversesResult = await GetAllMultiversesForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(multiversesResult, result);
            List<IUniverse> universes = new List<IUniverse>();

            if (!multiversesResult.IsError)
            {
                foreach (IMultiverse multiverse in multiversesResult.Result)
                {
                    universes.Add(multiverse.Dimensions.FirstDimension.Universe);
                    universes.Add(multiverse.Dimensions.SecondDimension.Universe);
                    //universes.Add(multiverse.Dimensions.ThirdDimension.UniversePrime);
                    universes.Add(multiverse.Dimensions.ThirdDimension.Universe);
                    universes.Add(multiverse.Dimensions.ThirdDimension.MagicVerse);
                    universes.AddRange(multiverse.Dimensions.ThirdDimension.ParallelUniverses);
                    universes.Add(multiverse.Dimensions.FourthDimension.Universe);
                    universes.Add(multiverse.Dimensions.FifthDimension.Universe);
                    universes.Add(multiverse.Dimensions.SixthDimension.Universe);
                    universes.Add(multiverse.Dimensions.SeventhDimension.Universe);
                }
            }

            universes.AddRange(GreatGrandSuperStar.ParentOmniverse.Dimensions.EighthDimension.SuperVerse.Universes);
            universes.AddRange(GreatGrandSuperStar.ParentOmniverse.Dimensions.NinthDimension.SuperVerse.Universes);
            universes.AddRange(GreatGrandSuperStar.ParentOmniverse.Dimensions.TenthDimension.SuperVerse.Universes);
            universes.AddRange(GreatGrandSuperStar.ParentOmniverse.Dimensions.EleventhDimension.SuperVerse.Universes);
            universes.AddRange(GreatGrandSuperStar.ParentOmniverse.Dimensions.TwelfthDimension.SuperVerse.Universes);

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

        public async Task<OASISResult<IEnumerable<IDimension>>> GetAllDimensionsForOmiverseAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IDimension>> result = new OASISResult<IEnumerable<IDimension>>();
            OASISResult<IEnumerable<IMultiverse>> multiveresesResult = await GetAllMultiversesForOmiverseAsync(refresh);
            OASISResultHelper.CopyResult(multiveresesResult, result);

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

                //Now add the Omniverse Dimensions (exist outside of the multiverses and spam across the entire Omniverse).
                dimensions.Add(GreatGrandSuperStar.ParentOmniverse.Dimensions.EighthDimension);
                dimensions.Add(GreatGrandSuperStar.ParentOmniverse.Dimensions.NinthDimension);
                dimensions.Add(GreatGrandSuperStar.ParentOmniverse.Dimensions.TenthDimension);
                dimensions.Add(GreatGrandSuperStar.ParentOmniverse.Dimensions.EleventhDimension);
                dimensions.Add(GreatGrandSuperStar.ParentOmniverse.Dimensions.TwelfthDimension);
                dimensions.AddRange(GreatGrandSuperStar.ParentOmniverse.Dimensions.CustomDimensions);

                result.Result = dimensions;
            }

            return result;
        }
    }
}
