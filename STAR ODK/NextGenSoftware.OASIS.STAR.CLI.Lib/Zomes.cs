using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Zomes : STARUIBase<STARZome, DownloadedZome, InstalledZome>
    {
        public Zomes(Guid avatarId) : base(new API.ONODE.Core.Managers.STARZomeManager(avatarId),
            "Welcome to the Zome Wizard", new List<string> 
            {
                "This wizard will allow you create a Zome which contain Holon's. Zome's are like modules and Holon's are the basic building blocks of The OASIS and one of their functions is to act as data objects, everything in The OASIS is dervived from a Holon, Holon's can be made of other Holon's also.",
                "The wizard will create an empty folder with a ZomeDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the zome into this folder.",
                "Finally you run the sub-command 'zome publish' to convert the folder containing the zome (can contain any number of files and sub-folders) into a OASIS Zome file (.ozome) as well as optionally upload to STARNET.",
                "You can then share the .ozome file with others across any platform or OS, who can then install the Zome from the file using the sub-command 'zome install'.",
                "You can also optionally choose to upload the .ozome file to the STARNET store so others can search, download and install the zome."
            },
            STAR.STARDNA.DefaultZomesSourcePath, "DefaultZomesSourcePath",
            STAR.STARDNA.DefaultZomesPublishedPath, "DefaultZomesPublishedPath",
            STAR.STARDNA.DefaultZomesDownloadedPath, "DefaultZomesDownloadedPath",
            STAR.STARDNA.DefaultZomesInstalledPath, "DefaultZomesInstalledPath")
        { }
    }
}