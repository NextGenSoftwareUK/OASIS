using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Types;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GraphQL
{
    /// <summary>Root Mutation type for STAR ODK write operations.</summary>
    public class Mutation
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());

        // ── STAR Engine ───────────────────────────────────────────────────────

        /// <summary>Ignites (boots) the STAR ODK engine.</summary>
        public async Task<string> IgniteStarAsync(string username, string password)
        {
            var result = await _starAPI.BootOASISAsync(username, password);
            return result.Message ?? (result.IsError ? "Failed to ignite STAR." : "STAR ignited successfully.");
        }

        /// <summary>Extinguishes (shuts down) the STAR ODK engine.</summary>
        public async Task<string> ExtinguishStarAsync()
        {
            var result = await STARAPI.ShutdownOASISAsync();
            return result.Message ?? (result.IsError ? "Failed to extinguish STAR." : "STAR extinguished successfully.");
        }

        // ── Celestial Bodies ──────────────────────────────────────────────────

        /// <summary>Creates a new celestial body.</summary>
        public async Task<CelestialBodyDto?> CreateCelestialBodyAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.CelestialBodies.CreateAsync(avId, name, description, default, string.Empty, null);
            if (result.IsError || result.Result == null)
                return null;

            var b = result.Result;
            return new CelestialBodyDto { Id = b.Id.ToString(), Name = b.Name ?? string.Empty, Description = b.Description ?? string.Empty };
        }

        /// <summary>Updates an existing celestial body.</summary>
        public async Task<CelestialBodyDto?> UpdateCelestialBodyAsync(string id, string name, string description, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var body = new STARCelestialBody { Id = guid, Name = name, Description = description };
            var result = await _starAPI.CelestialBodies.UpdateAsync(avId, body);
            if (result.IsError || result.Result == null)
                return null;

            var b = result.Result;
            return new CelestialBodyDto { Id = b.Id.ToString(), Name = b.Name ?? string.Empty, Description = b.Description ?? string.Empty };
        }

        /// <summary>Deletes a celestial body by ID.</summary>
        public async Task<bool> DeleteCelestialBodyAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.CelestialBodies.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── NFTs ──────────────────────────────────────────────────────────────

        /// <summary>Mints a new NFT.</summary>
        public async Task<NftDto?> MintNftAsync(string name, string description, string avatarId)
        {
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var nft = new STARNFT { Name = name, Description = description };
            var result = await _starAPI.NFTs.UpdateAsync(avId, nft);
            if (result.IsError || result.Result == null)
                return null;

            var n = result.Result;
            return new NftDto { Id = n.Id.ToString(), Name = n.Name ?? string.Empty, Description = n.Description ?? string.Empty, MintedByAvatarId = avatarId };
        }

        /// <summary>Updates an existing NFT's metadata.</summary>
        public async Task<NftDto?> UpdateNftAsync(string id, string name, string description, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var nft = new STARNFT { Id = guid, Name = name, Description = description };
            var result = await _starAPI.NFTs.UpdateAsync(avId, nft);
            if (result.IsError || result.Result == null)
                return null;

            var n = result.Result;
            return new NftDto { Id = n.Id.ToString(), Name = n.Name ?? string.Empty, Description = n.Description ?? string.Empty, MintedByAvatarId = avatarId };
        }

        /// <summary>Deletes (burns) an NFT by ID.</summary>
        public async Task<bool> DeleteNftAsync(string id, string avatarId)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;
            if (!Guid.TryParse(avatarId, out var avId))
                avId = Guid.Empty;

            var result = await _starAPI.NFTs.DeleteAsync(avId, guid, 0);
            return !result.IsError;
        }

        // ── oApps ─────────────────────────────────────────────────────────────

        /// <summary>Creates a new oApp. TODO: wire to _starAPI.OApps when available.</summary>
        public string CreateOApp(string name, string description, string avatarId)
        {
            // TODO: wire to _starAPI.OApps.CreateAsync when the OApps manager is surfaced on STARAPI
            return $"TODO: oApp '{name}' creation not yet wired.";
        }

        // ── Quests ────────────────────────────────────────────────────────────

        /// <summary>Seeds a new quest. TODO: wire to quest manager.</summary>
        public string SeedQuest(string name, string description, string avatarId)
        {
            // TODO: wire to _starAPI.Quests.CreateAsync when available
            return $"TODO: Quest '{name}' seeding not yet wired.";
        }
    }
}
