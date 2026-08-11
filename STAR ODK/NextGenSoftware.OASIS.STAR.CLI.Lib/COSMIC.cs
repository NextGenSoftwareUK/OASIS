using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class COSMIC
    {
        private COSMICManager _cosmicManager;
        private Guid _avatarId;

        // All Celestial Body Types (excluding spaces)
        private static readonly List<HolonType> CelestialBodyTypes = new List<HolonType>
        {
            HolonType.Star,
            HolonType.Planet,
            HolonType.Moon,
            HolonType.Asteroid,
            HolonType.Comet,
            HolonType.Meteroid,
            HolonType.Nebula,
            HolonType.SuperVerse,
            HolonType.WormHole,
            HolonType.BlackHole,
            HolonType.Portal,
            HolonType.StarGate,
            HolonType.SpaceTimeDistortion,
            HolonType.SpaceTimeAbnormally,
            HolonType.TemporalRift,
            HolonType.StarDust,
            HolonType.CosmicWave,
            HolonType.CosmicRay,
            HolonType.GravitationalWave
        };

        // All Celestial Space Types
        private static readonly List<HolonType> CelestialSpaceTypes = new List<HolonType>
        {
            HolonType.Omniverse,
            HolonType.Multiverse,
            HolonType.Universe,
            HolonType.GalaxyCluster,
            HolonType.Galaxy,
            HolonType.SolarSystem
        };

        public COSMIC(Guid avatarId, OASISDNA oasisDNA = null)
        {
            _avatarId = avatarId;
            _cosmicManager = new COSMICManager(avatarId, oasisDNA);
        }
    }
}
