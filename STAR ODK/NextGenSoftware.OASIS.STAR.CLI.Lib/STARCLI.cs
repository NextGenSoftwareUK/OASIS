
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static class STARCLI
    {
        public static Avatars Avatars { get; } = new Avatars(STAR.BeamedInAvatar.Id);
        public static Data Data { get; } = new Data(STAR.BeamedInAvatar.Id);
        public static OAPPs OAPPs { get; } = new OAPPs(STAR.BeamedInAvatar.Id);
        public static OAPPTemplates OAPPTemplates { get; } = new OAPPTemplates(STAR.BeamedInAvatar.Id);
        public static Runtimes Runtimes { get; } = new Runtimes(STAR.BeamedInAvatar.Id);
        public static Chapters Chapters { get; } = new Chapters(STAR.BeamedInAvatar.Id);
        public static Missions Missions { get; } = new Missions(STAR.BeamedInAvatar.Id);
        public static Quests Quests { get; } = new Quests(STAR.BeamedInAvatar.Id);
        public static NFTs NFTs { get; } = new NFTs(STAR.BeamedInAvatar.Id);
        public static GeoNFTs GeoNFTs { get; } = new GeoNFTs(STAR.BeamedInAvatar.Id);
        public static GeoHotsSpots GeoHotsSpots { get; } = new GeoHotsSpots(STAR.BeamedInAvatar.Id);
        public static InventoryItems InventoryItems { get; } = new InventoryItems(STAR.BeamedInAvatar.Id);
    }
}
