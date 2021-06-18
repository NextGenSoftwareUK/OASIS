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
    public class GrandSuperStarCore : CelestialBodyCore, IGrandSuperStarCore 
    {
        public IGrandSuperStar GrandSuperStar { get; set; }

        public GrandSuperStarCore(Dictionary<ProviderType, string> providerKey) : base(providerKey)
        {

        }

        public GrandSuperStarCore(Guid id) : base(id)
        {

        }

        public async Task<OASISResult<IGalaxy>> AddGalaxyAsync(IGalaxy galaxy)
        {
            return OASISResultHolonToHolonHelper<IHolon, IGalaxy>.CopyResult(
                await AddHolonToCollectionAsync(GrandSuperStar, galaxy, (List<IHolon>)Mapper<IGalaxy, Holon>.MapBaseHolonProperties(
                    GrandSuperStar.ParentUniverse.Galaxies)), new OASISResult<IGalaxy>());
        }

        public OASISResult<IGalaxy> AddGalaxy(IGalaxy solarSystem)
        {
            return AddGalaxyAsync(solarSystem).Result;
        }

        public async Task<OASISResult<IStar>> AddStarAsync(IStar star)
        {
            return OASISResultHolonToHolonHelper<IHolon, IStar>.CopyResult(
                await AddHolonToCollectionAsync(GrandSuperStar, star, (List<IHolon>)Mapper<IStar, Holon>.MapBaseHolonProperties(
                    GrandSuperStar.ParentUniverse.Stars)), new OASISResult<IStar>());
        }

        public OASISResult<IStar> AddStar(IStar star)
        {
            return AddStarAsync(star).Result;
        }

        public async Task<OASISResult<IEnumerable<IGalaxy>>> GetGalaxiesAsync(bool refresh = true)
        {
            OASISResult<IEnumerable<IGalaxy>> result = new OASISResult<IEnumerable<IGalaxy>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GrandSuperStar.ParentUniverse.Galaxies, HolonType.Galaxy, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IGalaxy>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Galaxy>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IGalaxy>> GetGalaxies(bool refresh = true)
        {
            return GetGalaxiesAsync(refresh).Result;
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
            OASISResult<IEnumerable<IStar>> result = new OASISResult<IEnumerable<IStar>>();
            OASISResult<IEnumerable<IHolon>> holonResult = await GetHolonsAsync(GrandSuperStar.ParentUniverse.Stars, HolonType.Star, refresh);
            OASISResultCollectionToCollectionHelper<IEnumerable<IHolon>, IEnumerable<IStar>>.CopyResult(holonResult, ref result);
            result.Result = Mapper<IHolon, Star>.MapBaseHolonProperties(holonResult.Result);
            return result;
        }

        public OASISResult<IEnumerable<IStar>> GetStars(bool refresh = true)
        {
            return GetStarsAsync(refresh).Result;
        }
    }
}