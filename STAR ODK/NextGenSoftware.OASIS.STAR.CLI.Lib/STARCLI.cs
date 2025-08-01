
namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static class STARCLI
    {
        public static Avatars Avatars { get; } = new Avatars(STAR.BeamedInAvatar.Id);
        public static Zomes Zomes { get; } = new Zomes(STAR.BeamedInAvatar.Id);
        public static ZomesMetaDataDNA ZomesMetaDataDNA { get; } = new ZomesMetaDataDNA(STAR.BeamedInAvatar.Id);
        public static Holons Holons { get; } = new Holons(STAR.BeamedInAvatar.Id);
        public static HolonsMetaDataDNA HolonsMetaDataDNA { get; } = new HolonsMetaDataDNA(STAR.BeamedInAvatar.Id);
        public static CelestialBodies CelestialBodies { get; } = new CelestialBodies(STAR.BeamedInAvatar.Id);
        public static CelestialBodiesMetaDataDNA CelestialBodiesMetaDataDNA { get; } = new CelestialBodiesMetaDataDNA(STAR.BeamedInAvatar.Id);
        public static CelestialSpaces CelestialSpaces { get; } = new CelestialSpaces(STAR.BeamedInAvatar.Id);
        public static OAPPs OAPPs { get; } = new OAPPs(STAR.BeamedInAvatar.Id);
        public static OAPPTemplates OAPPTemplates { get; } = new OAPPTemplates(STAR.BeamedInAvatar.Id);
        public static Runtimes Runtimes { get; } = new Runtimes(STAR.BeamedInAvatar.Id);
        public static Libs Libs { get; } = new Libs(STAR.BeamedInAvatar.Id);
        public static Chapters Chapters { get; } = new Chapters(STAR.BeamedInAvatar.Id);
        public static Missions Missions { get; } = new Missions(STAR.BeamedInAvatar.Id);
        public static Quests Quests { get; } = new Quests(STAR.BeamedInAvatar.Id);
        public static NFTs NFTs { get; } = new NFTs(STAR.BeamedInAvatar.Id);
        public static GeoNFTs GeoNFTs { get; } = new GeoNFTs(STAR.BeamedInAvatar.Id);
        public static GeoHotSpots GeoHotSpots { get; } = new GeoHotSpots(STAR.BeamedInAvatar.Id);
        public static InventoryItems InventoryItems { get; } = new InventoryItems(STAR.BeamedInAvatar.Id);
        public static Plugins Plugins { get; } = new Plugins(STAR.BeamedInAvatar.Id);
        public static STARTests STARTests { get; } = new STARTests();
    }
}
