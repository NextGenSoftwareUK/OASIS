using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Libs : STARNETUIBase<Library, DownloadedLibrary, InstalledLibrary, LibraryDNA>
    {
        public Libs(Guid avatarId) : base(new LibraryManager(avatarId),
            "Welcome to the Library Wizard", new List<string> 
            {
                "This wizard will allow you create a Library which can be used in a OAPP from (along with a OAPP Template & Runtime).",
                "The library can be created from anything you like from any stack, platform, OS etc.",
                "You can even link an existing library from any site or package store to this one. When you publish you will be given an option to do so.",
                "The wizard will create an empty folder with a LibraryDNA.json file in it. You then simply place any files/folders you need into this folder (if you are linking to an existing library then you can skip this step).",
                "Finally you run the sub-command 'library publish' to convert the folder containing the library (can contain any number of files and sub-folders) into a OASIS Library file (.olibrary) as well as optionally upload to STARNET.",
                "You can then share the .olibrary file with others across any platform or OS from which they can include in OAPP's. They can install the Library from the file using the sub-command 'library install'.",
                "You can also optionally choose to upload the .olibrary file to the STARNET store so others can search, download and install the library. They can then create OAPP's from the library."
            },
            STAR.STARDNA.DefaultLibsSourcePath, "DefaultLibsSourcePath",
            STAR.STARDNA.DefaultLibsPublishedPath, "DefaultLibsPublishedPath",
            STAR.STARDNA.DefaultLibsDownloadedPath, "DefaultLibsDownloadedPath",
            STAR.STARDNA.DefaultLibsInstalledPath, "DefaultLibsInstalledPath")
        { }
    }
}