using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Holons : STARUIBase<STARHolon, DownloadedHolon, InstalledHolon>
    {
        public Holons(Guid avatarId) : base(new API.ONODE.Core.Managers.STARHolonManager(avatarId),
            "Welcome to the Holon Wizard", new List<string> 
            {
                "This wizard will allow you create a Holon. Holon's are the basic building blocks of The OASIS and one of their functions is to act as data objects, everything in The OASIS is dervived from a Holon, Holon's can be made of other Holon's also.",
                "The wizard will create an empty folder with a HolonDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the mission into this folder.",
                "Finally you run the sub-command 'holon publish' to convert the folder containing the holon (can contain any number of files and sub-folders) into a OASIS Holon file (.oholon) as well as optionally upload to STARNET.",
                "You can then share the .oholon file with others across any platform or OS, who can then install the Holon from the file using the sub-command 'holon install'.",
                "You can also optionally choose to upload the .oholon file to the STARNET store so others can search, download and install the holon."
            },
            STAR.STARDNA.DefaultHolonsSourcePath, "DefaultHolonsSourcePath",
            STAR.STARDNA.DefaultHolonsPublishedPath, "DefaultHolonsPublishedPath",
            STAR.STARDNA.DefaultHolonsDownloadedPath, "DefaultHolonsDownloadedPath",
            STAR.STARDNA.DefaultHolonsInstalledPath, "DefaultHolonsInstalledPath")
        { }
    }
}