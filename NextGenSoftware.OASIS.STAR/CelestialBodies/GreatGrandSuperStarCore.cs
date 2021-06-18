using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.STAR.CelestialContainers;

namespace NextGenSoftware.OASIS.STAR
{
    public class GreatGrandSuperStarCore : CelestialBodyCore, IGreatGrandSuperStarCore 
    {
        public IGreatGrandSuperStar GreatGrandSuperStar { get; set; }

        public GreatGrandSuperStarCore(Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {

        }

        public GreatGrandSuperStarCore(Guid id) : base(id)
        {

        }

        public async Task<OASISResult<IUniverse>> AddUniverseAsync(IUniverse universe)
        {
            return OASISResultHolonToHolonHelper<IHolon, IUniverse>.CopyResult(
                await AddHolonToCollectionAsync(GreatGrandSuperStar, universe, (List<IHolon>)Mapper<IUniverse, Holon>.MapBaseHolonProperties(
                    GreatGrandSuperStar.ParentOmiverse.Universes)), new OASISResult<IUniverse>());
        }

        public OASISResult<IUniverse> AddUniverse(IUniverse universe)
        {
            return AddUniverseAsync(universe).Result;
        }

        public async Task<OASISResult<IEnumerable<IUniverse>>> GetUniversesAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IUniverse>> result = new OASISResult<IEnumerable<IUniverse>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GreatGrandSuperStar.ParentOmiverse.Universes, HolonType.Universe, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IUniverse>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Universe>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IUniverse>> GetUniverses(bool refresh = true)
        {
            return GetUniversesAsync(refresh).Result;
        }

        // Helper method to get the GrandSuperStars at the centre of each Universe.
        public async Task<OASISResult<IEnumerable<IGrandSuperStar>>> GetGrandSuperStarsAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGrandSuperStar>> result = new OASISResult<IEnumerable<IGrandSuperStar>>();
            OASISResult<IEnumerable<IUniverse>> universesResult = await GetUniversesAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IUniverse>, IEnumerable<IGrandSuperStar>>.CopyResult(universesResult, ref result);

            if (!universesResult.IsError)
            {
                List<IGrandSuperStar> grandSuperstars = new List<IGrandSuperStar>();

                foreach (IUniverse universe in universesResult.Result)
                    grandSuperstars.Add(universe.GrandSuperStar);

                result.Result = grandSuperstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<IGrandSuperStar>> GetGrandSuperStars(bool refresh = true)
        {
            throw new NotImplementedException();
        }

        // Helper method to get the SuperStars at the centre of each Galaxy.
        // TODO: I don't think we should allow SuperStars to be added to a Universe outside of a Galaxy?
        // So SuperStars MUST always be contained inside a Galaxy.
        public async Task<OASISResult<IEnumerable<ISuperStar>>> GetSuperStarsAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<ISuperStar>> result = new OASISResult<IEnumerable<ISuperStar>>();
            OASISResult<IEnumerable<IGalaxy>> galaxiesResult = await GetGalaxiesAsync(refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IGalaxy>, IEnumerable<ISuperStar>>.CopyResult(galaxiesResult, ref result);

            if (!galaxiesResult.IsError)
            {
                List<ISuperStar> superstars = new List<ISuperStar>();

                foreach (IGalaxy galaxy in galaxiesResult.Result)
                    superstars.Add(galaxy.SuperStar);

                result.Result = superstars;
            }

            return result;
        }

        public OASISResult<IEnumerable<ISuperStar>> GetSuperStars(bool refresh = true)
        {
            return GetSuperStarsAsync(refresh).Result;
        }

        //TODO: Currently we are allowing Stars to be added outside of a Galaxy, not sure we should allow this or not?
        // In real life this is allowed so I guess ok here? :)
        public async Task<OASISResult<IEnumerable<IStar>>> GetStarsAsync(bool refresh = true)
        {
            
        }

        public OASISResult<IEnumerable<IStar>> GetStars(bool refresh = true)
        {
            return GetStarsAsync(refresh).Result;
        }
    }
}