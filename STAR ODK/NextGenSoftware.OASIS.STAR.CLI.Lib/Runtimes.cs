using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Runtimes : STARNETUIBase<Runtime, DownloadedQuest, InstalledRuntime>
    {
        public Runtimes(Guid avatarId) : base(new API.ONODE.Core.Managers.RuntimeManager(avatarId),
            "Welcome to the Runtime Wizard", new List<string> 
            {
                "This wizard will allow you create an Runtime which can be used to create a OAPP from (along with a OAPP Template).",
                "The runtime can be created from anything you like from any stack, platform, OS etc.",
                "The STAR & OASIS runtimes can only be created by an admin/wizard.",
                "The wizard will create an empty folder with a RuntimeDNA.json file in it. You then simply place any files/folders you need into this folder.",
                "Finally you run the sub-command 'runtime publish' to convert the folder containing the runtime (can contain any number of files and sub-folders) into a OASIS Runtime file (.oruntime) as well as optionally upload to STARNET.",
                "You can then share the .oruntime file with others across any platform or OS from which they can create OAPP's from (along with a OAPP Template, you can even use the same OAPP Template for different runtimes). They can install the Runtime from the file using the sub-command 'runtime install'.",
                "You can also optionally choose to upload the .oruntime file to the STARNET store so others can search, download and install the runtime. They can then create OAPP's from the runtime."
            },
            STAR.STARDNA.DefaultRuntimesSourcePath, "DefaultRuntimesSourcePath",
            STAR.STARDNA.DefaultRuntimesPublishedPath, "DefaultRuntimesPublishedPath",
            STAR.STARDNA.DefaultRuntimesDownloadedPath, "DefaultRuntimesDownloadedPath",
            STAR.STARDNA.DefaultRuntimesInstalledPath, "DefaultRuntimesInstalledPath")
        { }
    }
}