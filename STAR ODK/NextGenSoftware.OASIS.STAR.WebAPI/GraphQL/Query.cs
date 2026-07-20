using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Types;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GraphQL
{
    /// <summary>Root Query type for the STAR ODK GraphQL endpoint.</summary>
    public class Query
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        // ── Celestial Bodies ──────────────────────────────────────────────────

        /// <summary>Returns all celestial bodies visible to the caller.</summary>
        public async Task<IEnumerable<CelestialBodyDto>> GetCelestialBodiesAsync()
        {
            var result = await _starAPI.CelestialBodies.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<CelestialBodyDto>();

            return result.Result.Select(b => new CelestialBodyDto
            {
                Id = b.Id.ToString(),
                Name = b.Name ?? string.Empty,
                Description = b.Description ?? string.Empty,
            });
        }

        /// <summary>Returns a single celestial body by ID.</summary>
        public async Task<CelestialBodyDto?> GetCelestialBodyAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.CelestialBodies.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var b = result.Result;
            return new CelestialBodyDto { Id = b.Id.ToString(), Name = b.Name ?? string.Empty, Description = b.Description ?? string.Empty };
        }

        // ── NFTs ──────────────────────────────────────────────────────────────

        /// <summary>Returns all NFTs.</summary>
        public async Task<IEnumerable<NftDto>> GetNftsAsync()
        {
            var result = await _starAPI.NFTs.LoadAllAsync(Guid.Empty, null);
            if (result.IsError || result.Result == null)
                return Enumerable.Empty<NftDto>();

            return result.Result.Select(n => new NftDto
            {
                Id = n.Id.ToString(),
                Name = n.Name ?? string.Empty,
                Description = n.Description ?? string.Empty,
            });
        }

        /// <summary>Returns a single NFT by ID.</summary>
        public async Task<NftDto?> GetNftAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var result = await _starAPI.NFTs.LoadAsync(Guid.Empty, guid, 0);
            if (result.IsError || result.Result == null)
                return null;

            var n = result.Result;
            return new NftDto { Id = n.Id.ToString(), Name = n.Name ?? string.Empty, Description = n.Description ?? string.Empty };
        }

        // ── STAR Status ───────────────────────────────────────────────────────

        /// <summary>Returns whether the STAR ODK engine is currently ignited (booted).</summary>
        public bool GetStarStatus() => _starAPI.IsOASISBooted;

        // ── oApps ─────────────────────────────────────────────────────────────

        /// <summary>Returns all registered oApps. TODO: wire to _starAPI.OApps when available.</summary>
        public IEnumerable<string> GetOApps()
        {
            // TODO: return real oApp list when _starAPI.OApps is surfaced
            return Enumerable.Empty<string>();
        }

        // ── Games ─────────────────────────────────────────────────────────────

        /// <summary>Returns all registered games. TODO: wire to _starAPI.Games when available.</summary>
        public IEnumerable<string> GetGames()
        {
            // TODO: return real game list when _starAPI.Games is surfaced
            return Enumerable.Empty<string>();
        }

        // ── Social ────────────────────────────────────────────────────────────

        /// <summary>Returns social posts/feed for an avatar. TODO: wire to social manager.</summary>
        public IEnumerable<string> GetSocialFeed(string avatarId)
        {
            // TODO: wire to _starAPI.Social or SocialManager
            return Enumerable.Empty<string>();
        }
    }
}
