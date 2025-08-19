using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class GeoHotSpots : STARNETUIBase<GeoHotSpot, DownloadedGeoHotSpot, InstalledGeoHotSpot, GeoHotSpotDNA>
    public class GeoHotSpots : STARNETUIBase<GeoHotSpot, DownloadedGeoHotSpot, InstalledGeoHotSpot, STARNETDNA>
    {
        public GeoHotSpots(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.GeoHotSpotManager(avatarId, STARDNA),
            "Welcome to the Geo-HotSpot Wizard", new List<string> 
            {
                "This wizard will allow you create a Mission which contains Quest's. Larger Quest's can be broken into Chapter's.",
                "Mission's can contain both Quest's and Chapter's. Quest's can also have sub-quests.",
                "Quest's contain GeoNFT's & GeoHotSpot's which can reward you various InventoryItem's for the avatar who completes the quest, triggers the GeoHotSpot or collects the GeoNFT.",
                "Mission's can optionally be linked to OAPP's.",
                "The wizard will create an empty folder with a MissionDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the mission into this folder.",
                "Finally you run the sub-command 'mission publish' to convert the folder containing the mission (can contain any number of files and sub-folders) into a OASIS Mission file (.omission) as well as optionally upload to STARNET.",
                "You can then share the .omission file with others across any platform or OS, who can then install the Mission from the file using the sub-command 'mission install'.",
                "You can also optionally choose to upload the .omission file to the STARNET store so others can search, download and install the mission."
            },
            STAR.STARDNA.DefaultMissionsSourcePath, "DefaultMissionsSourcePath",
            STAR.STARDNA.DefaultMissionsPublishedPath, "DefaultMissionsPublishedPath",
            STAR.STARDNA.DefaultMissionsDownloadedPath, "DefaultMissionsDownloadedPath",
            STAR.STARDNA.DefaultMissionsInstalledPath, "DefaultMissionsInstalledPath")
        { }
    }
}