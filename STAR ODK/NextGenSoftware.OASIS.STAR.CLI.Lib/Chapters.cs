using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class Chapters : STARNETUIBase<Chapter, DownloadedChapter, InstalledChapter, ChapterDNA>
    public class Chapters : STARNETUIBase<Chapter, DownloadedChapter, InstalledChapter, STARNETDNA>
    {
        public Chapters(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.ChapterManager(avatarId, STARDNA),
            "Welcome to the Chapter Wizard", new List<string> 
            {
                "This wizard will allow you create a Chapter which contain Quest's. Chapter's belong to Mission's and allow larger quest's to be broken into Chapter's.",
                "Quest's can also have sub-quests.",
                "Quest's contain GeoNFT's & GeoHotSpot's which can reward you various InventoryItem's for the avatar who completes the quest, triggers the GeoHotSpot or collects the GeoNFT.",
                "The wizard will create an empty folder with a ChapterDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the mission into this folder.",
                "Finally you run the sub-command 'chapter publish' to convert the folder containing the chapter (can contain any number of files and sub-folders) into a OASIS Chapter file (.ochapter) as well as optionally upload to STARNET.",
                "You can then share the .ochapter file with others across any platform or OS, who can then install the Chapter from the file using the sub-command 'chapter install'.",
                "You can also optionally choose to upload the .ochapter file to the STARNET store so others can search, download and install the chapter."
            },
            STAR.STARDNA.DefaultChaptersSourcePath, "DefaultChaptersSourcePath",
            STAR.STARDNA.DefaultChaptersPublishedPath, "DefaultChaptersPublishedPath",
            STAR.STARDNA.DefaultChaptersDownloadedPath, "DefaultChaptersDownloadedPath",
            STAR.STARDNA.DefaultChaptersInstalledPath, "DefaultChaptersInstalledPath")
        { }
    }
}