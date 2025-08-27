using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static class STARCLI
    {
        public static bool IsSTARIgnited
        {
            get
            {
                return STAR.IsStarIgnited;
            }
        }

        public static OASISResult<IOmiverse> IgniteSTAR()
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();

            if (!STAR.IsStarIgnited)
                result = STAR.IgniteStar();

            return result;
        }

        public static async Task<OASISResult<IOmiverse>> IgniteSTARAsync()
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();

            if (!STAR.IsStarIgnited)
                result = await STAR.IgniteStarAsync();

            return result;
        }

        public static OASISResult<bool> ExtinguishStar()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (STAR.IsStarIgnited)
                result = STAR.ExtinguishStar();

            return result;
        }

        public static async Task<OASISResult<bool>> ExtinguishStarAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            if (STAR.IsStarIgnited)
                result = await STAR.ExtinguishStarAsync();

            return result;
        }

        //public static Avatars Avatars { get; } = new Avatars(STAR.BeamedInAvatar?.Id ?? Guid.Empty);
        //public static Zomes Zomes { get; } = new Zomes(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

        public static Avatars Avatars
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();

                return new Avatars(STAR.BeamedInAvatar?.Id ?? Guid.Empty);
            }
        }

        public static Zomes Zomes
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();

                return new Zomes(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static ZomesMetaDataDNA ZomesMetaDataDNA { get; } = new ZomesMetaDataDNA(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        //public static Holons Holons { get; } = new Holons(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

        public static ZomesMetaDataDNA ZomesMetaDataDNA
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();

                return new ZomesMetaDataDNA(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        public static Holons Holons
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();

                return new Holons(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static HolonsMetaDataDNA HolonsMetaDataDNA { get; } = new HolonsMetaDataDNA(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static HolonsMetaDataDNA HolonsMetaDataDNA
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();

                return new HolonsMetaDataDNA(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }


        //public static CelestialBodies CelestialBodies { get; } = new CelestialBodies(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static CelestialBodies CelestialBodies
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();

                return new CelestialBodies(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static CelestialBodiesMetaDataDNA CelestialBodiesMetaDataDNA { get; } = new CelestialBodiesMetaDataDNA(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static CelestialBodiesMetaDataDNA CelestialBodiesMetaDataDNA
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new CelestialBodiesMetaDataDNA(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static CelestialSpaces CelestialSpaces { get; } = new CelestialSpaces(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static CelestialSpaces CelestialSpaces
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new CelestialSpaces(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static OAPPs OAPPs { get; } = new OAPPs(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static OAPPs OAPPs
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new OAPPs(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static OAPPTemplates OAPPTemplates { get; } = new OAPPTemplates(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static OAPPTemplates OAPPTemplates
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new OAPPTemplates(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static Runtimes Runtimes { get; } = new Runtimes(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static Runtimes Runtimes
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new Runtimes(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static Libs Libs { get; } = new Libs(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static Libs Libs
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new Libs(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static Chapters Chapters { get; } = new Chapters(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static Chapters Chapters
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new Chapters(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static Missions Missions { get; } = new Missions(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static Missions Missions
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new Missions(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static Quests Quests { get; } = new Quests(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

        public static Quests Quests
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new Quests(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static NFTs NFTs { get; } = new NFTs(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

        public static NFTs NFTs
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new NFTs(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static GeoNFTs GeoNFTs { get; } = new GeoNFTs(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

        public static GeoNFTs GeoNFTs
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new GeoNFTs(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static GeoHotSpots GeoHotSpots { get; } = new GeoHotSpots(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
        public static GeoHotSpots GeoHotSpots
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new GeoHotSpots(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static InventoryItems InventoryItems { get; } = new InventoryItems(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

        public static InventoryItems InventoryItems
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new InventoryItems(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static Plugins Plugins { get; } = new Plugins(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);

        public static Plugins Plugins
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new Plugins(STAR.BeamedInAvatar?.Id ?? Guid.Empty, STAR.STARDNA);
            }
        }

        //public static STARTests STARTests { get; } = new STARTests();

        public static STARTests STARTests
        {
            get
            {
                if (!IsSTARIgnited)
                    IgniteSTAR();
                return new STARTests();
            }
        }
    }
}
